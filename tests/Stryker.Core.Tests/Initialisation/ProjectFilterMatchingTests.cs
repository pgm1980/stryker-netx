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
/// </summary>
public class ProjectFilterMatchingTests
{
    [Theory]
    // The issue-#270 class: dotted project name, filter WITHOUT extension.
    [InlineData(@"C:\repo\src\Stryker.Configuration\Stryker.Configuration.csproj", "Stryker.Configuration")]
    [InlineData(@"C:\repo\src\Stryker.TestRunner.VsTest\Stryker.TestRunner.VsTest.csproj", "Stryker.TestRunner.VsTest")]
    // Linux-style separators (CI runners).
    [InlineData("/home/runner/work/src/Stryker.Configuration/Stryker.Configuration.csproj", "Stryker.Configuration")]
    // Filter WITH extension (the Sprint-159 E2E-covered class) keeps working.
    [InlineData(@"C:\repo\TargetProject\TargetProject.csproj", "TargetProject.csproj")]
    [InlineData(@"C:\repo\src\Stryker.Configuration\Stryker.Configuration.csproj", "Stryker.Configuration.csproj")]
    // Undotted name without extension.
    [InlineData(@"C:\repo\TargetProject\TargetProject.csproj", "TargetProject")]
    // Case-insensitive on both sides.
    [InlineData(@"C:\repo\src\stryker.configuration\stryker.configuration.csproj", "STRYKER.CONFIGURATION")]
    public void MatchesFilter_Matches_ExactProjectNames(string projectFilePath, string filter) =>
        InputFileResolver.MatchesFilter(projectFilePath, filter).Should().BeTrue(
            $"filter '{filter}' names exactly the project file '{projectFilePath}'");

    [Theory]
    // Accidental cross-match of the pre-fix implementation: 'Foo.Bar' was reduced
    // to 'Foo' and matched Foo.csproj — i.e. the WRONG project could be selected.
    [InlineData(@"C:\repo\Foo\Foo.csproj", "Foo.Bar")]
    // Cross-extension must not match either ('TargetProject.fsproj' vs csproj).
    [InlineData(@"C:\repo\TargetProject\TargetProject.csproj", "TargetProject.fsproj")]
    // No partial/prefix semantics (ADR-039 explicitly replaced substring matching).
    [InlineData(@"C:\repo\src\Stryker.Configuration.Extra\Stryker.Configuration.Extra.csproj", "Stryker.Configuration")]
    [InlineData(@"C:\repo\src\Stryker.Configuration\Stryker.Configuration.csproj", "Configuration")]
    // Degenerate inputs.
    [InlineData("", "Stryker.Configuration")]
    [InlineData(@"C:\repo\src\Stryker.Configuration\Stryker.Configuration.csproj", "")]
    public void MatchesFilter_Rejects_NonMatchingNames(string projectFilePath, string filter) =>
        InputFileResolver.MatchesFilter(projectFilePath, filter).Should().BeFalse(
            $"filter '{filter}' does not name the project file '{projectFilePath}'");
}
