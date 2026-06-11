using System.IO;
using FluentAssertions;
using Stryker.Core.Initialisation;
using Xunit;

namespace Stryker.Core.Tests.Initialisation;

/// <summary>
/// Sprint 172 (ADR-052, issue #270): unit-level pin for
/// <see cref="InputFileResolver.MatchesFilter(string, string)"/> — the single
/// name-matching primitive behind all three ADR-039 filter-defense layers.
/// The Sprint-159 E2E coverage (AisessLikeSlnxFoldersTests) only ever exercised
/// filters WITH a <c>.csproj</c> extension; extension-less dotted module names
/// (<c>"Stryker.Configuration"</c>) were mutilated by
/// <c>Path.GetFileNameWithoutExtension</c> on the FILTER side ("Stryker") and
/// never matched — which silently discarded the <c>project</c> config key for
/// every dotted project name (the entire nightly-dogfood roster).
/// <para>
/// All inline paths use forward slashes: <c>Path.GetFileName</c> splits on
/// <c>'/'</c> on every OS, while backslash literals only split on Windows —
/// the production callers pre-normalize filters via <c>NormalizePath</c>
/// (backslash → forward slash), so forward-slash inputs are the faithful form.
/// </para>
/// </summary>
public class ProjectFilterMatchingTests
{
    [Theory]
    // The issue-#270 class: dotted project name, filter WITHOUT extension.
    [InlineData("C:/repo/src/Stryker.Configuration/Stryker.Configuration.csproj", "Stryker.Configuration")]
    [InlineData("C:/repo/src/Stryker.TestRunner.VsTest/Stryker.TestRunner.VsTest.csproj", "Stryker.TestRunner.VsTest")]
    [InlineData("/home/runner/work/src/Stryker.Configuration/Stryker.Configuration.csproj", "Stryker.Configuration")]
    // Filter WITH extension (the Sprint-159 E2E-covered class) keeps working.
    [InlineData("C:/repo/TargetProject/TargetProject.csproj", "TargetProject.csproj")]
    [InlineData("C:/repo/src/Stryker.Configuration/Stryker.Configuration.csproj", "Stryker.Configuration.csproj")]
    // Filter as FULL PATH (targetProjectMode passes NormalizePath(FindProjectFile(wd)) —
    // the MultipleTestProjects integration scenario; regression of the first fix attempt).
    [InlineData("C:/repo/TargetProject/TargetProject.csproj", "C:/repo/TargetProject/TargetProject.csproj")]
    [InlineData("/home/x/TargetProject/TargetProject.csproj", "/other/checkout/TargetProject/TargetProject.csproj")]
    // Filter as relative path.
    [InlineData("C:/repo/src/Stryker.Configuration/Stryker.Configuration.csproj", "../Stryker.Configuration/Stryker.Configuration.csproj")]
    // Undotted name without extension.
    [InlineData("C:/repo/TargetProject/TargetProject.csproj", "TargetProject")]
    // Case-insensitive on both sides.
    [InlineData("C:/repo/src/stryker.configuration/stryker.configuration.csproj", "STRYKER.CONFIGURATION")]
    public void MatchesFilter_Matches_ExactProjectNames(string projectFilePath, string filter) =>
        InputFileResolver.MatchesFilter(projectFilePath, filter).Should().BeTrue(
            $"filter '{filter}' names exactly the project file '{projectFilePath}'");

    [Theory]
    // Accidental cross-match of the pre-fix implementation: 'Foo.Bar' was reduced
    // to 'Foo' and matched Foo.csproj — i.e. the WRONG project could be selected.
    [InlineData("C:/repo/Foo/Foo.csproj", "Foo.Bar")]
    // Cross-extension must not match either ('TargetProject.fsproj' vs csproj).
    [InlineData("C:/repo/TargetProject/TargetProject.csproj", "TargetProject.fsproj")]
    // Full-path filter naming a DIFFERENT project must not match.
    [InlineData("C:/repo/TargetProject/TargetProject.csproj", "C:/repo/ExtraProject/ExtraProject.csproj")]
    // No partial/prefix semantics (ADR-039 explicitly replaced substring matching).
    [InlineData("C:/repo/src/Stryker.Configuration.Extra/Stryker.Configuration.Extra.csproj", "Stryker.Configuration")]
    [InlineData("C:/repo/src/Stryker.Configuration/Stryker.Configuration.csproj", "Configuration")]
    // Degenerate inputs.
    [InlineData("", "Stryker.Configuration")]
    [InlineData("C:/repo/src/Stryker.Configuration/Stryker.Configuration.csproj", "")]
    public void MatchesFilter_Rejects_NonMatchingNames(string projectFilePath, string filter) =>
        InputFileResolver.MatchesFilter(projectFilePath, filter).Should().BeFalse(
            $"filter '{filter}' does not name the project file '{projectFilePath}'");

    [Fact]
    public void MatchesFilter_Matches_NativePlatformPaths()
    {
        // Native-separator probe: composes the path with the OS's own separator so
        // the suite proves the primitive on whatever platform it runs (the inline
        // matrix above is forward-slash-only by design).
        var path = Path.Combine(Path.GetTempPath(), "Stryker.Configuration", "Stryker.Configuration.csproj");
        InputFileResolver.MatchesFilter(path, "Stryker.Configuration").Should().BeTrue();
        InputFileResolver.MatchesFilter(path, "Stryker.Configuration.csproj").Should().BeTrue();
        InputFileResolver.MatchesFilter(path, "Stryker").Should().BeFalse();
    }
}
