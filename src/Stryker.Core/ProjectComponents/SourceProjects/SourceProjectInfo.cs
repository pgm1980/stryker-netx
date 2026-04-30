using System;
using System.Collections.Generic;
using Stryker.Abstractions;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Core.InjectedHelpers;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.ProjectComponents.SourceProjects;

public class SourceProjectInfo : IProjectAndTests
{
    private readonly List<string> _warnings = [];

    public Action? OnProjectBuilt { get; set; }

    public SolutionInfo? SolutionInfo { get; set; }

    public IProjectAnalysis Analysis { get; init; } = null!; // initialized via object initializer in InputFileResolver.BuildSourceProjectInfo

    /// <summary>
    /// The Folder/File structure found in the project under test.
    /// </summary>
    public IReadOnlyProjectComponent ProjectContents { get; set; } = null!; // initialized via setter in InputFileResolver.BuildSourceProjectInfo

    public bool IsFullFramework => Analysis.TargetsFullFramework();

    public string HelperNamespace => CodeInjector.HelperNamespace;

    public CodeInjection CodeInjector { get; } = new();

    public ITestProjectsInfo TestProjectsInfo { get; set; } = null!; // initialized via setter in InputFileResolver.BuildSourceProjectInfo

    public IReadOnlyCollection<string> Warnings => _warnings;

    public IReadOnlyList<string> GetTestAssemblies() =>
        TestProjectsInfo.GetTestAssemblies();

    public string LogError(string error)
    {
        _warnings.Add(error);
        return error;
    }
}
