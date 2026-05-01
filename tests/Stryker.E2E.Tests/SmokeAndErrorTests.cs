using FluentAssertions;
using Stryker.E2E.Tests.Infrastructure;
using Xunit;

namespace Stryker.E2E.Tests;

/// <summary>
/// Sprint 21 — fast E2E tests that don't require a full mutation run. Each
/// case spawns Stryker.CLI with a flag that exits in &lt;3 s (--version,
/// --help, missing solution, unknown reporter). Smokes the CLI surface
/// without paying the ~25 s cost of a full Stryker run.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class SmokeAndErrorTests
{
    [Fact]
    public void HelpFlag_ExitsZeroAndPrintsUsage()
    {
        var result = ProcessSpawnHelper.RunCli(["--help"], RepoRoot.SamplesDir, timeoutSeconds: 30);
        result.ExitCode.Should().Be(0, "--help is the standard CLI hint and must succeed");
        var helpText = result.StdOut + result.StdErr;
        helpText.Should().Contain("Usage: Stryker", "--help must print the McMaster usage banner");
    }

    [Fact]
    public void HelpFlag_ListsCoreOptions()
    {
        // Spot-check that the help text documents the production CLI surface.
        // Regression here means a CLI flag silently disappeared.
        var result = ProcessSpawnHelper.RunCli(["--help"], RepoRoot.SamplesDir, timeoutSeconds: 30);
        result.ExitCode.Should().Be(0);
        var helpText = result.StdOut + result.StdErr;
        helpText.Should().Contain("--reporter");
        helpText.Should().Contain("--mutation-level");
        helpText.Should().Contain("--solution");
    }

    [Fact]
    public void NonExistentSolutionPath_ExitsNonZero()
    {
        // Stryker auto-discovers a solution from the working directory; passing an
        // explicit non-existent path must short-circuit instead of fallback-discovering.
        var result = ProcessSpawnHelper.RunCli(
            ["--solution", "this-path-does-not-exist.sln"],
            RepoRoot.SamplesDir, timeoutSeconds: 60);
        result.ExitCode.Should().NotBe(0,
            "an explicit --solution that resolves to no file must fail the run rather than silently fall back");
    }

    [Fact]
    public void UnknownReporter_ExitsNonZeroWithDiagnostic()
    {
        var result = ProcessSpawnHelper.RunCli(
            ["--solution", RepoRoot.SampleSlnx, "--reporter", "this-reporter-does-not-exist"],
            RepoRoot.SamplesDir, timeoutSeconds: 60);
        result.ExitCode.Should().NotBe(0, "an unknown reporter must short-circuit the run");
        var output = result.StdOut + result.StdErr;
        output.Should().Contain("this-reporter-does-not-exist",
            "the diagnostic must echo the offending reporter name back to the user");
    }
}
