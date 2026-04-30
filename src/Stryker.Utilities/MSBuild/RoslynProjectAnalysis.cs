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
        _references = [.. roslynProject.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Where(r => r.FilePath is not null)
            .Select(r => r.FilePath!)];
        _sourceFiles = [.. roslynProject.Documents
            .Where(d => d.FilePath is not null)
            .Select(d => d.FilePath!)];
        _projectReferences = [.. roslynProject.ProjectReferences
            .Select(pr => roslynProject.Solution.GetProject(pr.ProjectId)?.FilePath)
            .Where(p => p is not null)
            .Select(p => p!)];
        _analyzerAssemblyPaths = [.. roslynProject.AnalyzerReferences
            .Where(a => a.FullPath is not null)
            .Select(a => a.FullPath!)];
        _embeddedResourcePaths = ResolveEmbeddedResources(evaluationProject, roslynProject.FilePath);
        _referenceAliases = BuildReferenceAliases(roslynProject, evaluationProject);
    }

    /// <summary>
    /// Sprint 3.2: aliases can be declared either on a metadata reference
    /// (<c>&lt;Reference Include="..." Aliases="X"/&gt;</c> — surfaced via
    /// <see cref="MetadataReferenceProperties.Aliases"/>) or on a project-to-project
    /// reference (<c>&lt;ProjectReference Include="..." Aliases="X"/&gt;</c> — surfaced
    /// via <see cref="Microsoft.CodeAnalysis.ProjectReference.Aliases"/>). The first
    /// case is captured by iterating MetadataReferences; the second has to be
    /// resolved by mapping each project-reference's referenced project to its
    /// <see cref="Project.OutputFilePath"/> so the alias attaches to the same path
    /// that <c>IProjectAnalysisExtensions.LoadReferences</c> later emits.
    /// </summary>
    private static Dictionary<string, IReadOnlyList<string>> BuildReferenceAliases(RoslynProject roslynProject, EvaluationProject? evaluationProject)
    {
        // OrdinalIgnoreCase + Path.GetFullPath to handle Windows drive-letter casing
        // and forward/back-slash variation between MetadataReference paths and
        // Project.OutputFilePath strings.
        var dict = new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.OrdinalIgnoreCase);

        static string Normalize(string path) => Path.GetFullPath(path);

        // Case 1: aliases on metadata references (PortableExecutableReference.Properties.Aliases).
        foreach (var reference in roslynProject.MetadataReferences.OfType<PortableExecutableReference>())
        {
            if (reference.FilePath is null || reference.Properties.Aliases.IsDefaultOrEmpty)
            {
                continue;
            }
            dict[Normalize(reference.FilePath)] = [.. reference.Properties.Aliases];
        }

        // Case 2: aliases on project references (ProjectReference.Aliases). Map by output file path
        // so they co-index with the MetadataReference paths returned by LoadReferences().
        foreach (var projectRef in roslynProject.ProjectReferences)
        {
            if (projectRef.Aliases.IsDefaultOrEmpty)
            {
                continue;
            }
            var referencedProject = roslynProject.Solution.GetProject(projectRef.ProjectId);
            var outputPath = referencedProject?.OutputFilePath;
            if (string.IsNullOrEmpty(outputPath))
            {
                continue;
            }
            var key = Normalize(outputPath);
            // Project-ref aliases compose with metadata-ref aliases (both apply).
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = [.. existing.Concat(projectRef.Aliases).Distinct(System.StringComparer.Ordinal)];
            }
            else
            {
                dict[key] = [.. projectRef.Aliases];
            }
        }

        // Case 3: MSBuildWorkspace does NOT propagate <Aliases> metadata from
        // <ProjectReference> XML to roslynProject.ProjectReferences[i].Aliases.
        // When the parallel EvaluationProject is available, read raw MSBuild
        // metadata directly.
        if (evaluationProject is not null)
        {
            ApplyEvaluationProjectAliases(dict, roslynProject, evaluationProject);
        }

        return dict;
    }

    private static void ApplyEvaluationProjectAliases(
        Dictionary<string, IReadOnlyList<string>> dict,
        RoslynProject roslynProject,
        EvaluationProject evaluationProject)
    {
        var sourceProjectDir = Path.GetDirectoryName(roslynProject.FilePath ?? string.Empty) ?? string.Empty;
        foreach (var item in evaluationProject.GetItems("ProjectReference"))
        {
            var aliasesMeta = item.GetMetadataValue("Aliases");
            if (string.IsNullOrWhiteSpace(aliasesMeta))
            {
                continue;
            }
            var aliasList = aliasesMeta
                .Split([',', ';'], System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();
            if (aliasList.Count == 0)
            {
                continue;
            }
            var key = ResolveProjectReferenceOutputPath(item.EvaluatedInclude, sourceProjectDir, roslynProject);
            if (key is null)
            {
                continue;
            }
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = [.. existing.Concat(aliasList).Distinct(System.StringComparer.Ordinal)];
            }
            else
            {
                dict[key] = aliasList;
            }
        }
    }

    private static string? ResolveProjectReferenceOutputPath(string referenceCsprojRel, string sourceProjectDir, RoslynProject roslynProject)
    {
        if (string.IsNullOrWhiteSpace(referenceCsprojRel))
        {
            return null;
        }
        var referenceCsprojAbs = Path.GetFullPath(Path.IsPathRooted(referenceCsprojRel)
            ? referenceCsprojRel
            : Path.Combine(sourceProjectDir, referenceCsprojRel));
        var refProj = roslynProject.Solution.Projects.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.FilePath) &&
            string.Equals(Path.GetFullPath(p.FilePath), referenceCsprojAbs, System.StringComparison.OrdinalIgnoreCase));
        var refOutputPath = refProj?.OutputFilePath;
        return string.IsNullOrEmpty(refOutputPath) ? null : Path.GetFullPath(refOutputPath);
    }

    private static string[] ResolveEmbeddedResources(EvaluationProject? evaluationProject, string? csprojPath)
    {
        if (evaluationProject is null || csprojPath is null)
        {
            return [];
        }

        var projectDir = Path.GetDirectoryName(csprojPath) ?? string.Empty;
        return [.. evaluationProject.GetItems("EmbeddedResource")
            .Select(item => item.EvaluatedInclude)
            .Where(include => !string.IsNullOrEmpty(include))
            .Select(include => Path.IsPathRooted(include) ? include : Path.GetFullPath(Path.Combine(projectDir, include)))];
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
        return (IReadOnlyList<string>)[.. _evaluationProject.GetItems(itemType)
            .Select(item => item.EvaluatedInclude)
            .Where(include => !string.IsNullOrEmpty(include))
            .Select(include => Path.IsPathRooted(include) ? include : Path.GetFullPath(Path.Combine(projectDir, include)))];
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
