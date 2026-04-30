using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Core.MutantFilters;
using Stryker.Core.ProjectComponents;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.Initialisation;

public partial class CsharpProjectComponentsBuilder : ProjectComponentsBuilder
{
    private readonly SourceProjectInfo _projectInfo;
    private readonly IStrykerOptions _options;
    private readonly string[] _foldersToExclude;
    private readonly ILogger _logger;

    public CsharpProjectComponentsBuilder(SourceProjectInfo projectInfo, IStrykerOptions options, string[] foldersToExclude, ILogger logger, IFileSystem fileSystem) : base(fileSystem)
    {
        _projectInfo = projectInfo;
        _options = options;
        _foldersToExclude = foldersToExclude;
        _logger = logger;
    }

    public override IReadOnlyProjectComponent Build()
    {
        CsharpFolderComposite inputFiles;
        if (_projectInfo.Analysis.SourceFiles is { Count: > 0 })
        {
            inputFiles = FindProjectFilesUsingAnalysis(_projectInfo.Analysis, _options);
        }
        else
        {
            LogProjectAnalysisFallback(_logger);
            inputFiles = FindProjectFilesScanningProjectFolders(_projectInfo.Analysis);
        }
        return inputFiles;
    }

    // This is a backup strategy
    private CsharpFolderComposite FindProjectFilesScanningProjectFolders(IProjectAnalysis analysis)
    {
        var inputFiles = new CsharpFolderComposite();
        var sourceProjectDir = Path.GetDirectoryName(analysis.ProjectFilePath) ?? string.Empty;
        var cSharpParseOptions = analysis.GetParseOptions(_options);
        foreach (var dir in ExtractProjectFolders(analysis))
        {
            var folder = FileSystem.Path.Combine(Path.GetDirectoryName(sourceProjectDir) ?? string.Empty, dir);
            LogScanningFolder(_logger, folder);
            inputFiles.Add(FindInputFiles(folder, sourceProjectDir, analysis, cSharpParseOptions));
        }

        return inputFiles;
    }

    public override void InjectHelpers(IReadOnlyProjectComponent inputFiles)
        => InjectMutantHelpers((CsharpFolderComposite)inputFiles, _projectInfo.Analysis.GetParseOptions(_options));

    private CsharpFolderComposite FindProjectFilesUsingAnalysis(IProjectAnalysis analysis, IStrykerOptions options)
    {
        var generatedAssemblyInfo = analysis.AssemblyAttributeFileName();
        var projectUnderTestFolderComposite = new CsharpFolderComposite()
        {
            FullPath = Path.GetDirectoryName(analysis.ProjectFilePath) ?? string.Empty,
            RelativePath = Path.GetDirectoryName(Path.GetDirectoryName(analysis.ProjectFilePath)) ?? string.Empty,
        };
        var cache = new Dictionary<string, CsharpFolderComposite>(StringComparer.Ordinal) { [string.Empty] = projectUnderTestFolderComposite };

        // Save cache in a singleton, so we can use it in other parts of the project
        FolderCompositeCacheRegistry.Get<CsharpFolderComposite>().Cache = cache;

        foreach (var sourceFile in analysis.SourceFiles)
        {
            var projectDir = Path.GetDirectoryName(analysis.ProjectFilePath) ?? string.Empty;
            var relativePath = Path.GetRelativePath(projectDir, sourceFile);
            var folderComposite = GetOrBuildFolderComposite(cache, Path.GetDirectoryName(relativePath) ?? string.Empty, projectDir, projectUnderTestFolderComposite);

            var file = new CsharpFileLeaf()
            {
                SourceCode = FileSystem.File.ReadAllText(sourceFile),
                FullPath = sourceFile,
                RelativePath = relativePath
            };

            // Get the syntax tree for the source file
            var syntaxTree = CSharpSyntaxTree.ParseText(file.SourceCode, analysis.GetParseOptions(options), file.FullPath, encoding: Encoding.UTF32);

            // don't mutate auto generated code
            if (syntaxTree.IsGenerated())
            {
                // we found the generated assemblyinfo file
                if (string.Equals(FileSystem.Path.GetFileName(sourceFile), generatedAssemblyInfo, StringComparison.OrdinalIgnoreCase))
                {
                    // add the mutated text
                    syntaxTree = InjectMutationLabel(syntaxTree);
                }
                LogSkippingGenerated(_logger, file.FullPath);
                folderComposite.AddCompilationSyntaxTree(syntaxTree); // Add the syntaxTree to the list of compilationSyntaxTrees
                continue; // Don't add the file to the folderComposite as we're not reporting on the file
            }

            file.SyntaxTree = syntaxTree;
            folderComposite.Add(file);
        }
        return projectUnderTestFolderComposite;
    }

    public override Action PostBuildAction() => () => ScanPackageContentFiles(_projectInfo.Analysis, (CsharpFolderComposite)_projectInfo.ProjectContents);

    public void ScanPackageContentFiles(IProjectAnalysis analysis, CsharpFolderComposite projectUnderTestFolderComposite)
    {
        // look for extra source files coming from Nuget packages
        var folder = analysis.GetPropertyOrDefault("ContentPreprocessorOutputDirectory");
        var sourceProjectDir = Path.GetDirectoryName(analysis.ProjectFilePath) ?? string.Empty;
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }
        folder = Path.Combine(sourceProjectDir, folder);
        if (FileSystem.Directory.Exists(folder))
        {
            projectUnderTestFolderComposite.Add(FindInputFiles(folder, sourceProjectDir, analysis.GetParseOptions(_options), false));
        }
    }

    private static SyntaxTree InjectMutationLabel(SyntaxTree syntaxTree)
    {
        var root = syntaxTree.GetRoot();

        var myAttribute = ((CompilationUnitSyntax)root).AttributeLists
            .SelectMany(al => al.Attributes).FirstOrDefault(n => n.Name.Kind() == SyntaxKind.QualifiedName
                                                                 && ((QualifiedNameSyntax)n.Name).Right.Kind() == SyntaxKind.IdentifierName
                                                                 && string.Equals((string?)((IdentifierNameSyntax)((QualifiedNameSyntax)n.Name).Right).Identifier.Value, "AssemblyTitleAttribute", StringComparison.Ordinal));
        var labelNode = myAttribute?.ArgumentList?.Arguments.First().Expression;
        var newLabel = string.Empty;
        if (labelNode != null && labelNode.Kind() == SyntaxKind.StringLiteralExpression)
        {
            var literal = (LiteralExpressionSyntax)labelNode;
            newLabel = $"Mutated {literal.Token.Value}";
        }

        if (myAttribute == null || labelNode == null)
        {
            return syntaxTree;
        }
        var newAttribute = myAttribute.ReplaceNode(labelNode,
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newLabel)));
        return root.ReplaceNode(myAttribute, newAttribute).SyntaxTree;

    }

    /// <summary>
    /// Recursively scans the given directory for files to mutate
    /// Deprecated method, should not be maintained
    /// </summary>
    private CsharpFolderComposite FindInputFiles(string path, string sourceProjectDir,
        IProjectAnalysis analysis, CSharpParseOptions cSharpParseOptions)
    {
        var rootFolderComposite = new CsharpFolderComposite
        {
            FullPath = Path.GetFullPath(path),
            RelativePath = Path.GetRelativePath(sourceProjectDir, Path.GetFullPath(path))
        };


        rootFolderComposite.Add(
            FindInputFiles(path, Path.GetDirectoryName(analysis.ProjectFilePath) ?? string.Empty, cSharpParseOptions)
        );
        return rootFolderComposite;
    }

    /// <summary>
    /// Recursively scans the given directory for files to mutate
    /// Deprecated method, should not be maintained
    /// </summary>
    private CsharpFolderComposite FindInputFiles(string path, string sourceProjectDir, CSharpParseOptions cSharpParseOptions, bool mutate = true)
    {

        var folderComposite = new CsharpFolderComposite
        {
            FullPath = Path.GetFullPath(path),
            RelativePath = Path.GetRelativePath(sourceProjectDir, Path.GetFullPath(path))
        };

        foreach (var folder in FileSystem.Directory.EnumerateDirectories(folderComposite.FullPath).Where(x => !_foldersToExclude.Contains(Path.GetFileName(x))))
        {
            folderComposite.Add(FindInputFiles(folder, sourceProjectDir, cSharpParseOptions, mutate));
        }

        foreach (var file in FileSystem.Directory.GetFiles(folderComposite.FullPath, "*.cs", SearchOption.TopDirectoryOnly).Where(f => !f.EndsWith(".xaml.cs", StringComparison.Ordinal)))
        {
            // Roslyn cannot compile xaml.cs files generated by xamarin.
            // Since the files are generated they should not be mutated anyway, so skip these files.

            var fileLeaf = new CsharpFileLeaf()
            {
                SourceCode = FileSystem.File.ReadAllText(file),
                FullPath = file,
                RelativePath = Path.GetRelativePath(sourceProjectDir, file)
            };

            // Get the syntax tree for the source file
            var syntaxTree = CSharpSyntaxTree.ParseText(fileLeaf.SourceCode, cSharpParseOptions, fileLeaf.FullPath, Encoding.UTF32);

            // don't mutate auto generated code
            if (syntaxTree.IsGenerated() || !mutate)
            {
                LogSkippingGenerated(_logger, fileLeaf.FullPath);
                folderComposite.AddCompilationSyntaxTree(syntaxTree); // Add the syntaxTree to the list of compilationSyntaxTrees
                continue; // Don't add the file to the folderComposite as we're not reporting on the file
            }

            fileLeaf.SyntaxTree = syntaxTree;
            folderComposite.Add(fileLeaf);
        }

        return folderComposite;
    }

    private void InjectMutantHelpers(CsharpFolderComposite rootFolderComposite, CSharpParseOptions cSharpParseOptions)
    {
        foreach (var (name, code) in _projectInfo.CodeInjector.MutantHelpers)
        {
            rootFolderComposite.AddCompilationSyntaxTree(CSharpSyntaxTree.ParseText(code, path: name, encoding: Encoding.UTF32, options: cSharpParseOptions));
        }
    }

    // get the FolderComposite object representing the project's folder 'targetFolder'. Build the needed FolderComposite(s) for a complete path
    private CsharpFolderComposite GetOrBuildFolderComposite(Dictionary<string, CsharpFolderComposite> cache, string targetFolder, string sourceProjectDir, CsharpFolderComposite inputFiles)
    {
        if (cache.TryGetValue(targetFolder, out var composite))
        {
            return composite;
        }

        var folder = targetFolder;
        CsharpFolderComposite? subDir = null;
        // build the cache recursively (in reverse order)
        while (!string.IsNullOrEmpty(folder))
        {
            if (cache.TryGetValue(folder, out var subCache))
            {
                // no need to travel further
                if (subDir is not null)
                {
                    subCache.Add(subDir);
                }
                break;
            }

            // we have not scanned this folder yet
            var fullPath = FileSystem.Path.Combine(sourceProjectDir, folder);
            var newComposite = new CsharpFolderComposite
            {
                FullPath = fullPath,
                RelativePath = Path.GetRelativePath(sourceProjectDir, fullPath),
            };
            if (subDir == null)
            {
                // this is the folder we are building
                composite = newComposite;
            }
            else
            {
                // going up
                newComposite.Add(subDir);
            }

            cache.Add(folder, newComposite);
            subDir = newComposite;
            folder = FileSystem.Path.GetDirectoryName(folder) ?? string.Empty;
            if (string.IsNullOrEmpty(folder))
            {
                // we are at root
                inputFiles.Add(subDir);
            }
        }

        return composite!;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project analysis could not find sourcefiles. This should not happen. We fallback to filesystem scan. Please report an issue at github.")]
    private static partial void LogProjectAnalysisFallback(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanning {Folder}")]
    private static partial void LogScanningFolder(ILogger logger, string folder);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping auto-generated code file: {FileName}")]
    private static partial void LogSkippingGenerated(ILogger logger, string fileName);
}
