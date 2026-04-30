using System.Collections.Generic;

namespace Stryker.Abstractions.Analysis;

/// <summary>
/// Project-level analysis result produced by <c>stryker-netx</c>'s project loader.
/// Replaces the Buildalyzer-specific <c>Buildalyzer.IAnalyzerResult</c> contract that
/// previously leaked into Layer-0; consumers should depend on this interface instead.
/// </summary>
/// <remarks>
/// The implementation is provided by <c>Stryker.Utilities.MSBuild.RoslynProjectAnalysis</c>
/// (Phase 9), which adapts Roslyn's <c>Microsoft.CodeAnalysis.Project</c> +
/// <c>Microsoft.Build.Evaluation.Project</c> for raw MSBuild property access.
/// </remarks>
public interface IProjectAnalysis
{
    /// <summary>Absolute path to the .csproj file.</summary>
    string ProjectFilePath { get; }

    /// <summary>Target framework moniker (e.g. <c>net10.0</c>).</summary>
    string TargetFramework { get; }

    /// <summary>The MSBuild <c>AssemblyName</c> property value (e.g. <c>Sample.Library</c>).</summary>
    string AssemblyName { get; }

    /// <summary>The MSBuild <c>TargetFileName</c> property value (e.g. <c>Sample.Library.dll</c>).</summary>
    string TargetFileName { get; }

    /// <summary>The directory containing the compiled output (e.g. <c>bin/Debug/net10.0/</c>).</summary>
    string TargetDir { get; }

    /// <summary>Absolute path of the compiled assembly (<c>TargetDir + TargetFileName</c>).</summary>
    string OutputFilePath { get; }

    /// <summary>Absolute path of the reference-assembly variant (<c>obj/.../ref/...dll</c>).</summary>
    string OutputRefFilePath { get; }

    /// <summary>The Roslyn language identifier (<c>"C#"</c> or <c>"Visual Basic"</c>).</summary>
    string Language { get; }

    /// <summary>True when the project is detected as a unit-test project.</summary>
    bool IsTestProject { get; }

    /// <summary>True when project analysis succeeded; false signals a degraded result.</summary>
    bool Succeeded { get; }

    /// <summary>True when the project produces an assembly (i.e. has a <c>TargetFileName</c>).</summary>
    bool BuildsAnAssembly { get; }

    /// <summary>Absolute paths of every <c>Compile</c> source file.</summary>
    IReadOnlyList<string> SourceFiles { get; }

    /// <summary>Absolute paths of every metadata reference (resolved DLLs).</summary>
    IReadOnlyList<string> References { get; }

    /// <summary>Absolute paths of every <c>ProjectReference</c> target.</summary>
    IReadOnlyList<string> ProjectReferences { get; }

    /// <summary>
    /// Absolute paths of every <c>EmbeddedResource</c> item declared by the project.
    /// Used by Stryker to inject mutation-control helpers as managed resources.
    /// </summary>
    IReadOnlyList<string> EmbeddedResourcePaths { get; }

    /// <summary>
    /// Absolute paths of every analyzer / source-generator assembly the project pulls in.
    /// Used by Stryker to load source generators when assembling the mutation compilation.
    /// </summary>
    IReadOnlyList<string> AnalyzerAssemblyPaths { get; }

    /// <summary>
    /// Per-reference <c>Aliases</c> values. Keys are the absolute reference paths from
    /// <see cref="References"/>; missing keys mean the reference has no aliases.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<string>> ReferenceAliases { get; }

    /// <summary>
    /// Looks up a raw MSBuild property by name. Returns <paramref name="defaultValue"/>
    /// when the property is undefined or empty.
    /// </summary>
    string? GetPropertyOrDefault(string key, string? defaultValue = null);

    /// <summary>
    /// Enumerates raw MSBuild item metadata for the given item type (e.g. <c>EmbeddedResource</c>).
    /// Each entry is a list of absolute paths; this is the equivalent of
    /// <c>EvaluationProject.GetItems(itemType).Select(i =&gt; i.EvaluatedInclude)</c>.
    /// Returns an empty array when the item type has no entries.
    /// </summary>
    IReadOnlyList<string> GetItemPaths(string itemType);
}
