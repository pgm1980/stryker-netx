using System.Collections.Generic;
using System.IO;
using Moq;
using Stryker.Abstractions.Analysis;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 25 (v2.12.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/TestHelper.cs, **architecture-adapted**
/// from upstream's Buildalyzer-specific <c>Mock&lt;IAnalyzerResult&gt;</c> to our
/// stryker-netx-native <c>Mock&lt;IProjectAnalysis&gt;</c> abstraction (Sprint 1
/// Phase 9: Workspaces.MSBuild port replaced Buildalyzer with
/// <c>Stryker.Utilities.MSBuild.RoslynProjectAnalysis</c>).
///
/// Test consumers can synthesize <see cref="IProjectAnalysis"/> mocks shaped
/// like a real project-analysis return value without having to invoke MSBuild.
/// The signature mirrors upstream's <c>SetupProjectAnalyzerResult</c> for a
/// near-1:1 migration of upstream tests; the &quot;properties&quot; dictionary
/// keys (<c>TargetDir</c>, <c>TargetFileName</c>, <c>Language</c>,
/// <c>IsTestProject</c>) are routed onto the equivalent strongly-typed
/// <see cref="IProjectAnalysis"/> properties.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Builds a Moq-backed <see cref="IProjectAnalysis"/>. Matches the upstream
    /// <c>SetupProjectAnalyzerResult</c> argument shape so per-module test ports
    /// can stay close to the upstream call site; the property-bag
    /// is unpacked onto the <see cref="IProjectAnalysis"/> shape.
    /// </summary>
    public static Mock<IProjectAnalysis> SetupProjectAnalyzerResult(
        IDictionary<string, string>? properties = null,
        string? projectFilePath = null,
        string[]? sourceFiles = null,
        IEnumerable<string>? projectReferences = null,
        string? targetFramework = null,
        string[]? references = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? aliases = null,
        bool isTestProject = false)
    {
        var mock = new Mock<IProjectAnalysis>();
        properties ??= new Dictionary<string, string>(System.StringComparer.Ordinal);
        SetupPaths(mock, properties, projectFilePath, targetFramework);
        SetupCollections(mock, sourceFiles, projectReferences, references, aliases);
        SetupFlags(mock, properties, targetFramework, isTestProject);
        return mock;
    }

    private static void SetupPaths(
        Mock<IProjectAnalysis> mock,
        IDictionary<string, string> properties,
        string? projectFilePath,
        string? targetFramework)
    {
        var resolvedProjectFilePath = projectFilePath ?? string.Empty;
        if (resolvedProjectFilePath.Length > 0)
        {
            mock.Setup(x => x.ProjectFilePath).Returns(resolvedProjectFilePath);
            if (!properties.ContainsKey("TargetDir"))
            {
                properties["TargetDir"] = Path.Combine(Path.GetFullPath(resolvedProjectFilePath), "bin", "Debug", targetFramework ?? "net");
                properties["TargetFileName"] = Path.GetFileNameWithoutExtension(resolvedProjectFilePath) + ".dll";
            }
        }

        if (properties.TryGetValue("TargetDir", out var targetDir))
        {
            mock.Setup(x => x.TargetDir).Returns(targetDir);
        }
        if (properties.TryGetValue("TargetFileName", out var targetFileName))
        {
            mock.Setup(x => x.TargetFileName).Returns(targetFileName);
        }
        if (properties.TryGetValue("TargetDir", out var td) && properties.TryGetValue("TargetFileName", out var tfn))
        {
            mock.Setup(x => x.OutputFilePath).Returns(Path.Combine(td, tfn));
        }
    }

    private static void SetupCollections(
        Mock<IProjectAnalysis> mock,
        string[]? sourceFiles,
        IEnumerable<string>? projectReferences,
        string[]? references,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? aliases)
    {
        mock.Setup(x => x.SourceFiles).Returns(sourceFiles ?? []);
        mock.Setup(x => x.References).Returns(references ?? []);
        mock.Setup(x => x.ProjectReferences).Returns(projectReferences is null ? [] : [.. projectReferences]);
        mock.Setup(x => x.ReferenceAliases).Returns(aliases ?? new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal));
        mock.Setup(x => x.EmbeddedResourcePaths).Returns([]);
        mock.Setup(x => x.AnalyzerAssemblyPaths).Returns([]);
    }

    private static void SetupFlags(
        Mock<IProjectAnalysis> mock,
        IDictionary<string, string> properties,
        string? targetFramework,
        bool isTestProject)
    {
        mock.Setup(x => x.Language).Returns(properties.TryGetValue("Language", out var lang) ? lang : "C#");
        if (targetFramework is not null)
        {
            mock.Setup(x => x.TargetFramework).Returns(targetFramework);
        }
        mock.Setup(x => x.IsTestProject).Returns(isTestProject);
        mock.Setup(x => x.Succeeded).Returns(true);
        mock.Setup(x => x.BuildsAnAssembly).Returns(properties.ContainsKey("TargetFileName"));
        mock.Setup(x => x.GetPropertyOrDefault(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns<string, string?>((key, def) => properties.TryGetValue(key, out var v) ? v : def);
    }
}
