using System.IO;
using FluentAssertions;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.Core.Tests;

public class StrykerOptionsTests
{
    [Fact]
    public void IsSolutionContext_WhenSolutionPathSetButWorkingDirectoryDiffers_ReturnsTrue()
    {
        // SOL-001 (external 360 test): an explicitly supplied solution path must enable solution mode
        // regardless of the working directory. Previously IsSolutionContext also required the working
        // directory to equal the solution directory, so passing the solution flag from any other folder
        // (even with an absolute path) silently fell back to single-project mode and produced a
        // misleading error. SolutionPath is only ever set from the solution flag, so a non-null value
        // already means the user opted in.
        var options = new StrykerOptions
        {
            SolutionPath = Path.Combine("repo", "src", "Calc.slnx"),
            WorkingDirectory = Path.Combine("repo", "tests", "AllTests"),
        };
        options.IsSolutionContext.Should().BeTrue(
            "an explicit solution path must enable solution mode from any working directory");
    }

    [Fact]
    public void IsSolutionContext_WhenSolutionPathSetAndWorkingDirectoryMatches_ReturnsTrue()
    {
        // Control: the originally supported case (working directory is the solution directory) still works.
        var options = new StrykerOptions
        {
            SolutionPath = Path.Combine("repo", "src", "Calc.slnx"),
            WorkingDirectory = Path.Combine("repo", "src"),
        };
        options.IsSolutionContext.Should().BeTrue();
    }

    [Fact]
    public void IsSolutionContext_WhenNoSolutionPath_ReturnsFalse()
    {
        // Control: without a solution path there is no solution context.
        var options = new StrykerOptions { WorkingDirectory = Path.Combine("repo", "src") };
        options.IsSolutionContext.Should().BeFalse();
    }
}
