using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stryker.Abstractions.Exceptions;
using System.Xml.Linq;
using System.Linq;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.ProjectComponents;
using System.IO;
using System.IO.Abstractions;
using Stryker.Utilities;

namespace Stryker.Core.Initialisation;

public abstract class ProjectComponentsBuilder
{
    protected IFileSystem FileSystem { get; }

    public abstract IReadOnlyProjectComponent Build();

    protected ProjectComponentsBuilder(IFileSystem fileSystem) => FileSystem = fileSystem;

    protected IEnumerable<string> ExtractProjectFolders(IProjectAnalysis projectAnalysis)
    {
        var projectFilePath = projectAnalysis.ProjectFilePath;
        var projectFile = FileSystem.File.OpenText(projectFilePath);
        var xDocument = XDocument.Load(projectFile);
        var folders = new List<string>();
        var projectDirectory = FileSystem.Path.GetDirectoryName(projectFilePath) ?? string.Empty;
        folders.Add(projectDirectory);

        foreach (var sharedProject in FindSharedProjects(xDocument))
        {
            var sharedProjectName = ReplaceMsbuildProperties(sharedProject, projectAnalysis);

            if (!FileSystem.File.Exists(FileSystem.Path.Combine(projectDirectory, sharedProjectName)))
            {
                throw new FileNotFoundException($"Missing shared project {sharedProjectName}");
            }

            var directoryName = FileSystem.Path.GetDirectoryName(sharedProjectName) ?? string.Empty;
            folders.Add(FileSystem.Path.Combine(projectDirectory, directoryName));
        }

        return folders;
    }

    private static IEnumerable<string> FindSharedProjects(XDocument document)
    {
        var importStatements = document.Elements().Descendants()
            .Where(projectElement => string.Equals(projectElement.Name.LocalName, "Import", StringComparison.OrdinalIgnoreCase));

        var sharedProjects = importStatements
            .SelectMany(importStatement => importStatement.Attributes(
                XName.Get("Project")))
            .Select(importFileLocation => FilePathUtils.NormalizePathSeparators(importFileLocation.Value) ?? string.Empty)
            .Where(importFileLocation => importFileLocation.EndsWith(".projitems", StringComparison.Ordinal));
        return sharedProjects;
    }

    private static readonly Regex MsBuildPropertyRegex = new(
        @"\$\((?<name>[a-zA-Z_][a-zA-Z0-9_\-.]*)\)",
        RegexOptions.ExplicitCapture | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(200));

    private static string ReplaceMsbuildProperties(string projectReference, IProjectAnalysis projectAnalysis)
    {
        return MsBuildPropertyRegex.Replace(projectReference,
            m =>
            {
                var property = m.Groups["name"].Value;
                var propertyValue = projectAnalysis.GetPropertyOrDefault(property);
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    return propertyValue;
                }

                var message = $"Missing MSBuild property ({property}) in project reference ({projectReference}). Please check your project file ({projectAnalysis.ProjectFilePath}) and try again.";
                throw new InputException(message);
            });
    }

    public abstract void InjectHelpers(IReadOnlyProjectComponent inputFiles);
    public abstract Action PostBuildAction();
}
