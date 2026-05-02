using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Stryker.Abstractions.Analysis;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 61 (v2.47.0). Fluent builder for <see cref="IProjectAnalysis"/> Moq mocks.
/// Complements the Sprint-25 <see cref="TestHelper.SetupProjectAnalyzerResult"/>
/// param-bag helper by giving full coverage of all 17 <see cref="IProjectAnalysis"/>
/// members (param-bag covers ~90% but lacks: <c>AssemblyName</c> direct setup,
/// <c>OutputRefFilePath</c>, <c>EmbeddedResourcePaths</c>,
/// <c>AnalyzerAssemblyPaths</c>, <c>GetItemPaths</c>).
/// </summary>
/// <remarks>
/// <para>Designed for upstream-port readability:</para>
/// <code>
/// var analysis = new ProjectAnalysisMockBuilder()
///     .WithProjectFilePath("c:\\project.csproj")
///     .WithTargetFramework("net10.0")
///     .WithAssemblyName("MyProject")
///     .AsTestProject()
///     .WithSourceFiles("c:\\src\\Foo.cs", "c:\\src\\Bar.cs")
///     .WithProperty("RootNamespace", "MyProject")
///     .WithItemPaths("EmbeddedResource", "c:\\res\\strings.resx")
///     .Build();
/// </code>
/// <para>Defaults are set so an unconfigured <c>new ProjectAnalysisMockBuilder().Build()</c>
/// returns a usable <see cref="IProjectAnalysis"/> (Succeeded=true, Language=C#,
/// empty collections).</para>
/// </remarks>
public sealed class ProjectAnalysisMockBuilder
{
    private string _projectFilePath = string.Empty;
    private string _targetFramework = "net10.0";
    private string? _assemblyName;
    private string? _targetFileName;
    private string? _targetDir;
    private string? _outputFilePath;
    private string _outputRefFilePath = string.Empty;
    private string _language = "C#";
    private bool _isTestProject;
    private bool _succeeded = true;
    private bool? _buildsAnAssembly;

    private List<string> _sourceFiles = [];
    private List<string> _references = [];
    private List<string> _projectReferences = [];
    private List<string> _embeddedResourcePaths = [];
    private List<string> _analyzerAssemblyPaths = [];

    private readonly Dictionary<string, IReadOnlyList<string>> _referenceAliases =
        new(StringComparer.Ordinal);

    private readonly Dictionary<string, string> _properties =
        new(StringComparer.Ordinal);

    private readonly Dictionary<string, IReadOnlyList<string>> _itemPaths =
        new(StringComparer.Ordinal);

    /// <summary>Sets <see cref="IProjectAnalysis.ProjectFilePath"/>.</summary>
    public ProjectAnalysisMockBuilder WithProjectFilePath(string projectFilePath)
    {
        _projectFilePath = projectFilePath;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.TargetFramework"/> (default: <c>net10.0</c>).</summary>
    public ProjectAnalysisMockBuilder WithTargetFramework(string targetFramework)
    {
        _targetFramework = targetFramework;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.AssemblyName"/>. If unset, derived from <c>ProjectFilePath</c>.</summary>
    public ProjectAnalysisMockBuilder WithAssemblyName(string assemblyName)
    {
        _assemblyName = assemblyName;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.TargetFileName"/>. If unset, derived from <c>AssemblyName</c>.</summary>
    public ProjectAnalysisMockBuilder WithTargetFileName(string targetFileName)
    {
        _targetFileName = targetFileName;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.TargetDir"/>. If unset, derived as <c>bin/Debug/{TargetFramework}</c>.</summary>
    public ProjectAnalysisMockBuilder WithTargetDir(string targetDir)
    {
        _targetDir = targetDir;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.OutputFilePath"/>. If unset, computed as <c>TargetDir + TargetFileName</c>.</summary>
    public ProjectAnalysisMockBuilder WithOutputFilePath(string outputFilePath)
    {
        _outputFilePath = outputFilePath;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.OutputRefFilePath"/> (default: empty string).</summary>
    public ProjectAnalysisMockBuilder WithOutputRefFilePath(string outputRefFilePath)
    {
        _outputRefFilePath = outputRefFilePath;
        return this;
    }

    /// <summary>Sets <see cref="IProjectAnalysis.Language"/> (default: <c>C#</c>).</summary>
    public ProjectAnalysisMockBuilder WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    /// <summary>Marks the project as a test project (<see cref="IProjectAnalysis.IsTestProject"/>=true).</summary>
    public ProjectAnalysisMockBuilder AsTestProject()
    {
        _isTestProject = true;
        return this;
    }

    /// <summary>Marks the analysis as failed (<see cref="IProjectAnalysis.Succeeded"/>=false).</summary>
    public ProjectAnalysisMockBuilder AsFailed()
    {
        _succeeded = false;
        return this;
    }

    /// <summary>Forces <see cref="IProjectAnalysis.BuildsAnAssembly"/>. If unset, defaults to <c>true</c> when a TargetFileName is configured.</summary>
    public ProjectAnalysisMockBuilder WithBuildsAnAssembly(bool buildsAnAssembly)
    {
        _buildsAnAssembly = buildsAnAssembly;
        return this;
    }

    /// <summary>Replaces <see cref="IProjectAnalysis.SourceFiles"/>.</summary>
    public ProjectAnalysisMockBuilder WithSourceFiles(params string[] sourceFiles)
    {
        _sourceFiles = [.. sourceFiles];
        return this;
    }

    /// <summary>Replaces <see cref="IProjectAnalysis.References"/>.</summary>
    public ProjectAnalysisMockBuilder WithReferences(params string[] references)
    {
        _references = [.. references];
        return this;
    }

    /// <summary>Replaces <see cref="IProjectAnalysis.ProjectReferences"/>.</summary>
    public ProjectAnalysisMockBuilder WithProjectReferences(params string[] projectReferences)
    {
        _projectReferences = [.. projectReferences];
        return this;
    }

    /// <summary>Replaces <see cref="IProjectAnalysis.EmbeddedResourcePaths"/>.</summary>
    public ProjectAnalysisMockBuilder WithEmbeddedResources(params string[] embeddedResourcePaths)
    {
        _embeddedResourcePaths = [.. embeddedResourcePaths];
        return this;
    }

    /// <summary>Replaces <see cref="IProjectAnalysis.AnalyzerAssemblyPaths"/>.</summary>
    public ProjectAnalysisMockBuilder WithAnalyzerAssemblies(params string[] analyzerAssemblyPaths)
    {
        _analyzerAssemblyPaths = [.. analyzerAssemblyPaths];
        return this;
    }

    /// <summary>Adds an entry to <see cref="IProjectAnalysis.ReferenceAliases"/> (composable).</summary>
    public ProjectAnalysisMockBuilder WithReferenceAlias(string referencePath, params string[] aliases)
    {
        _referenceAliases[referencePath] = [.. aliases];
        return this;
    }

    /// <summary>Adds an entry to the MSBuild property bag returned by <see cref="IProjectAnalysis.GetPropertyOrDefault"/> (composable).</summary>
    public ProjectAnalysisMockBuilder WithProperty(string key, string value)
    {
        _properties[key] = value;
        return this;
    }

    /// <summary>Adds an entry to the MSBuild item-path bag returned by <see cref="IProjectAnalysis.GetItemPaths"/> (composable).</summary>
    public ProjectAnalysisMockBuilder WithItemPaths(string itemType, params string[] paths)
    {
        _itemPaths[itemType] = [.. paths];
        return this;
    }

    /// <summary>Builds the underlying <see cref="Mock{T}"/> so tests can <c>.Verify(...)</c> later.</summary>
    public Mock<IProjectAnalysis> BuildMock()
    {
        var mock = new Mock<IProjectAnalysis>();
        var resolvedAssemblyName = ResolveAssemblyName();
        var resolvedTargetFileName = ResolveTargetFileName(resolvedAssemblyName);
        var resolvedTargetDir = ResolveTargetDir();
        var resolvedOutputFilePath = ResolveOutputFilePath(resolvedTargetDir, resolvedTargetFileName);
        var resolvedBuildsAnAssembly = _buildsAnAssembly ?? !string.IsNullOrEmpty(resolvedTargetFileName);

        mock.Setup(x => x.ProjectFilePath).Returns(_projectFilePath);
        mock.Setup(x => x.TargetFramework).Returns(_targetFramework);
        mock.Setup(x => x.AssemblyName).Returns(resolvedAssemblyName);
        mock.Setup(x => x.TargetFileName).Returns(resolvedTargetFileName);
        mock.Setup(x => x.TargetDir).Returns(resolvedTargetDir);
        mock.Setup(x => x.OutputFilePath).Returns(resolvedOutputFilePath);
        mock.Setup(x => x.OutputRefFilePath).Returns(_outputRefFilePath);
        mock.Setup(x => x.Language).Returns(_language);
        mock.Setup(x => x.IsTestProject).Returns(_isTestProject);
        mock.Setup(x => x.Succeeded).Returns(_succeeded);
        mock.Setup(x => x.BuildsAnAssembly).Returns(resolvedBuildsAnAssembly);

        mock.Setup(x => x.SourceFiles).Returns(_sourceFiles);
        mock.Setup(x => x.References).Returns(_references);
        mock.Setup(x => x.ProjectReferences).Returns(_projectReferences);
        mock.Setup(x => x.EmbeddedResourcePaths).Returns(_embeddedResourcePaths);
        mock.Setup(x => x.AnalyzerAssemblyPaths).Returns(_analyzerAssemblyPaths);
        mock.Setup(x => x.ReferenceAliases).Returns(_referenceAliases);

        mock.Setup(x => x.GetPropertyOrDefault(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns<string, string?>((key, def) => _properties.TryGetValue(key, out var v) ? v : def);
        mock.Setup(x => x.GetItemPaths(It.IsAny<string>()))
            .Returns<string>(itemType => _itemPaths.TryGetValue(itemType, out var v) ? v : []);

        return mock;
    }

    /// <summary>Builds the <see cref="IProjectAnalysis"/> instance directly. Convenience over <see cref="BuildMock"/>.</summary>
    public IProjectAnalysis Build() => BuildMock().Object;

    private string ResolveAssemblyName()
    {
        if (_assemblyName is not null)
        {
            return _assemblyName;
        }
        if (!string.IsNullOrEmpty(_projectFilePath))
        {
            return Path.GetFileNameWithoutExtension(_projectFilePath) ?? string.Empty;
        }
        return string.Empty;
    }

    private string ResolveTargetFileName(string assemblyName)
    {
        if (_targetFileName is not null)
        {
            return _targetFileName;
        }
        if (!string.IsNullOrEmpty(assemblyName))
        {
            return assemblyName + ".dll";
        }
        return string.Empty;
    }

    private string ResolveTargetDir()
    {
        if (_targetDir is not null)
        {
            return _targetDir;
        }
        if (!string.IsNullOrEmpty(_projectFilePath))
        {
            var projectDir = Path.GetDirectoryName(Path.GetFullPath(_projectFilePath)) ?? string.Empty;
            return Path.Combine(projectDir, "bin", "Debug", _targetFramework);
        }
        return string.Empty;
    }

    private string ResolveOutputFilePath(string targetDir, string targetFileName)
    {
        if (_outputFilePath is not null)
        {
            return _outputFilePath;
        }
        if (!string.IsNullOrEmpty(targetDir) && !string.IsNullOrEmpty(targetFileName))
        {
            return Path.Combine(targetDir, targetFileName);
        }
        return string.Empty;
    }
}
