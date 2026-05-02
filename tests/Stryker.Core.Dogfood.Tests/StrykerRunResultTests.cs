using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.Core.Dogfood.Tests;

/// <summary>
/// Sprint 46 (v2.33.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/StrykerRunResultTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class StrykerRunResultTests
{
    [Theory]
    [InlineData(1, 80)]
    [InlineData(0.5, 50)]
    [InlineData(0.1, 0)]
    public void ScoreIsLowerThanThresholdBreak_ShouldReturnFalseWhen(double mutationScore, int thresholdBreak)
    {
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 100, Low = 100, Break = thresholdBreak },
        };
        var runResult = new StrykerRunResult(options, mutationScore);

        var scoreIsLowerThanThresholdBreak = runResult.ScoreIsLowerThanThresholdBreak();

        scoreIsLowerThanThresholdBreak.Should().BeFalse("because the mutation score is higher than or equal to the threshold break");
    }

    [Theory]
    [InlineData(0.79, 80)]
    [InlineData(0.4, 50)]
    [InlineData(0, 1)]
    public void ScoreIsLowerThanThresholdBreak_ShouldReturnTrueWhen(double mutationScore, int thresholdBreak)
    {
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 100, Low = 100, Break = thresholdBreak },
        };
        var runResult = new StrykerRunResult(options, mutationScore);

        var scoreIsLowerThanThresholdBreak = runResult.ScoreIsLowerThanThresholdBreak();

        scoreIsLowerThanThresholdBreak.Should().BeTrue("because the mutation score is lower than the threshold break");
    }
}
