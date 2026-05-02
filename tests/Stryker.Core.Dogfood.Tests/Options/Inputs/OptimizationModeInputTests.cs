using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 70 (v2.56.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Renamed upstream OptimizationModeInputTests covers production CoverageAnalysisInput.</summary>
public class OptimizationModeInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new CoverageAnalysisInput();
        target.HelpText.Should().Be(
            "Use coverage info to speed up execution. Possible values are: off, perTest, all, perTestInIsolation.\n\t- off: Coverage data is not captured. Every mutant is tested against all test. Slowest, use in case of doubt.\n\t- perTest: Capture mutations covered by each test. Mutations are tested against covering tests (or flagged NoCoverage if no test cover them). Fastest option.\n\t- all: Capture the list of mutations covered by some test. Test only these mutations, other are flagged as NoCoverage. Fast option.\n\t- perTestInIsolation: 'perTest' but coverage of each test is captured in isolation. Increase coverage accuracy at the expense of a slow init phase.\n | default: 'perTest'"
                .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, new[] { OptimizationModes.CoverageBasedTest })]
    [InlineData("perTestinisolation", new[] { OptimizationModes.CoverageBasedTest, OptimizationModes.CaptureCoveragePerTest })]
    [InlineData("perTest", new[] { OptimizationModes.CoverageBasedTest })]
    [InlineData("all", new[] { OptimizationModes.SkipUncoveredMutants })]
    [InlineData("off", new[] { OptimizationModes.None })]
    public void ShouldSetFlags(string? value, OptimizationModes[] expectedFlags)
    {
        var target = new CoverageAnalysisInput { SuppliedInput = value! };

        var result = target.Validate();

        foreach (var flag in expectedFlags)
        {
            result.HasFlag(flag).Should().BeTrue();
        }
    }

    [Fact]
    public void ShouldThrowOnInvalidOptimizationMode()
    {
        var target = new CoverageAnalysisInput { SuppliedInput = "gibberish" };

        var act = () => target.Validate();

        var exception = act.Should().Throw<InputException>().Which;
        exception.Message.Should().MatchRegex(@"Incorrect coverageAnalysis option \(gibberish\)\. The options are \[.+\]\.");
        exception.ToString().Should().Contain("[off, perTest, all, perTestInIsolation].");
    }
}
