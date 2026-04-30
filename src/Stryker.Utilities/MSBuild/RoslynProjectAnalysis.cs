using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions.Analysis;
using EvaluationProject = Microsoft.Build.Evaluation.Project;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace Stryker.Utilities.MSBuild;

/// <summary>
/// <see cref="IProjectAnalysis"/> implementation that adapts a Roslyn
/// <see cref="RoslynProject"/> together with an optional
/// <see cref="EvaluationProject"/> for raw MSBuild property access.
/// </summary>
/// <remarks>
/// Roslyn's workspace API exposes pre-evaluated project information (output paths,
/// references, source documents) but not raw MSBuild properties. For features such as
/// the <c>IsTestingPlatformApplication</c> flag, this adapter falls back to a parallel
/// <see cref="EvaluationProject"/> instance that's loaded on demand.
/// </remarks>
public sealed class RoslynProjectAnalysis : IProjectAnalysis
{
    private static readonly string[] KnownTestPackages =
    [
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.core",
        "xunit.runner.visualstudio",
        "MSTest.TestFramework",
        "MSTest.TestAdapter",
        "NUnit",
        "NUnit3TestAdapter",
        "TUnit",
    ];

    private readonly RoslynProject _roslynProject;
    private readonly EvaluationProject? _evaluationProject;
    private readonly IReadOnlyList<string> _references;
    private readonly IReadOnlyList<string> _projectReferences;
    private readonly IReadOnlyList<string> _sourceFiles;
    private readonly IReadOnlyList<string> _analyzerAssemblyPaths;
    private readonly IReadOnlyList<string> _embeddedResourcePaths;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _referenceAliases;

    /// <summary>
    /// Creates a new analysis from a Roslyn workspace project. The optional
    /// <paramref name="evaluationProject"/> enables raw MSBuild property lookup;
    /// when omitted, <see cref="GetPropertyOrDefault"/> always returns the default
    /// value supplied by the caller.
    /// </summary>
    public RoslynProjectAnalysis(RoslynProject roslynProject, EvaluationProject? evaluationProject = null)
    {
        _roslynProject = roslynProject;
        _evaluationProject = evaluationProject;
        _references = roslynProject.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Where(r => r.FilePath is not null)
            .Select(r => r.FilePath!)
            .ToArray();
        _sourceFiles = roslynProject.Documents
            .Where(d => d.FilePath is not null)
            .Select(d => d.FilePath!)
            .ToArray();
        _projectReferences = roslynProject.ProjectReferences
            .Select(pr => roslynProject.Solution.GetProject(pr.ProjectId)?.FilePath)
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();
        _analyzerAssemblyPaths = roslynProject.AnalyzerReferences
            .Where(a => a.FullPath is not null)
            .Select(a => a.FullPath!)
            .ToArray();
        _embeddedResourcePaths = ResolveEmbeddedResources(evaluationProject, roslynProject.FilePath);
        _referenceAliases = roslynProject.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Where(r => r.FilePath is not null && !r.Properties.Aliases.IsDefaultOrEmpty)
            .ToDictionary(
                r => r.FilePath!,
                r => (IReadOnlyList<string>)r.Properties.Aliases.ToArray(),
                System.StringComparer.Ordinal);
    }

    private static string[] ResolveEmbeddedResources(EvaluationProject? evaluationProject, string? csprojPath)
    {
        if (evaluationProject is null || csprojPath is null)
        {
            return [];
        }

        var projectDir = Path.GetDirectoryName(csprojPath) ?? string.Empty;
        return evaluationProject.GetItems("EmbeddedResource")
            .Select(item => item.EvaluatedInclude)
            .Where(include => !string.IsNullOrEmpty(include))
            .Select(include => Path.IsPathRooted(include) ? include : Path.GetFullPath(Path.Combine(projectDir, include)))
            .ToArray();
    }

    /// <inheritdoc />
    public string ProjectFilePath => _roslynProject.FilePath ?? string.Empty;

    /// <inheritdoc />
    public string TargetFramework =>
        GetPropertyOrDefault("TargetFramework")
        ?? GetPropertyOrDefault("TargetFrameworks")
        ?? string.Empty;

    /// <inheritdoc />
    public string AssemblyName => _roslynProject.AssemblyName;

    /// <inheritdoc />
    public string TargetFileName =>
        _roslynProject.OutputFilePath is { } output
            ? Path.GetFileName(output)
            : string.Empty;

    /// <inheritdoc />
    public string TargetDir =>
        _roslynProject.OutputFilePath is { } output
            ? Path.GetDirectoryName(output) ?? string.Empty
            : string.Empty;

    /// <inheritdoc />
    public string OutputFilePath => _roslynProject.OutputFilePath ?? string.Empty;

    /// <inheritdoc />
    public string OutputRefFilePath =>
        _roslynProject.OutputRefFilePath ?? OutputFilePath;

    /// <inheritdoc />
    public string Language => _roslynProject.Language;

    /// <inheritdoc />
    public bool IsTestProject => DetectIsTestProject();

    /// <inheritdoc />
    public bool Succeeded => !string.IsNullOrEmpty(_roslynProject.OutputFilePath);

    /// <inheritdoc />
    public bool BuildsAnAssembly => !string.IsNullOrEmpty(TargetFileName);

    /// <inheritdoc />
    public IReadOnlyList<string> SourceFiles => _sourceFiles;

    /// <inheritdoc />
    public IReadOnlyList<string> References => _references;

    /// <inheritdoc />
    public IReadOnlyList<string> ProjectReferences => _projectReferences;

    /// <inheritdoc />
    public IReadOnlyList<string> EmbeddedResourcePaths => _embeddedResourcePaths;

    /// <inheritdoc />
    public IReadOnlyList<string> AnalyzerAssemblyPaths => _analyzerAssemblyPaths;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ReferenceAliases => _referenceAliases;

    /// <inheritdoc />
    public IReadOnlyList<string> GetItemPaths(string itemType)
    {
        if (_evaluationProject is null || string.IsNullOrEmpty(itemType))
        {
            return [];
        }

        var projectDir = Path.GetDirectoryName(ProjectFilePath) ?? string.Empty;
        return _evaluationProject.GetItems(itemType)
            .Select(item => item.EvaluatedInclude)
            .Where(include => !string.IsNullOrEmpty(include))
            .Select(include => Path.IsPathRooted(include) ? include : Path.GetFullPath(Path.Combine(projectDir, include)))
            .ToArray();
    }

    /// <inheritdoc />
    public string? GetPropertyOrDefault(string key, string? defaultValue = null)
    {
        if (_evaluationProject is null)
        {
            return defaultValue;
        }

        var value = _evaluationProject.GetPropertyValue(key);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    private bool DetectIsTestProject()
    {
        // Primary signal — IsTestingPlatformApplication is set by the new MS Test Platform SDK.
        if (bool.TryParse(GetPropertyOrDefault("IsTestingPlatformApplication"), out var isTestingPlatformApp)
            && isTestingPlatformApp)
        {
            return true;
        }

        // Legacy IsTestProject MSBuild flag (older test SDKs).
        if (bool.TryParse(GetPropertyOrDefault("IsTestProject"), out var isTestProject)
            && isTestProject)
        {
            return true;
        }

        // Fallback heuristic: presence of a known test framework as a metadata reference.
        // Mirrors the Buildalyzer-era detection in IAnalyzerResultExtensions.IsTestProject.
        var hasKnownTestPackage = _references
            .Select(Path.GetFileNameWithoutExtension)
            .Any(fileName => KnownTestPackages.Any(p => string.Equals(fileName, p, System.StringComparison.OrdinalIgnoreCase)));
        if (hasKnownTestPackage)
        {
            return true;
        }

        // Roslyn project name suffix heuristic (UnitTest, IntegrationTest, etc.).
        var name = _roslynProject.Name ?? string.Empty;
        return name.EndsWith(".Tests", System.StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".Test", System.StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".UnitTest", System.StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".UnitTests", System.StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".IntegrationTest", System.StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".IntegrationTests", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Friendly diagnostic representation, primarily used by error logs.
    /// </summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{AssemblyName} ({TargetFramework}) at {ProjectFilePath}");
}
