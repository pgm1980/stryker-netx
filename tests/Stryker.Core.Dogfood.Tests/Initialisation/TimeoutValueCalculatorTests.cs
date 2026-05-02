using FluentAssertions;
using Stryker.Core.Initialisation;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>
/// Sprint 57 (v2.43.0) port — minimal scope.
/// Most Initialisation tests deferred per Sprint 45 Investigation: HIGH DRIFT
/// (Sprint 1 Phase 9 removed Buildalyzer; tests need IProjectAnalysis-mock-builder, ~2d effort).
/// </summary>
public class TimeoutValueCalculatorTests
{
    [Theory]
    [InlineData(1000, 0, 1500)]
    [InlineData(1000, 2000, 3500)]
    [InlineData(4000, 2000, 8000)]
    public void Calculator_ShouldCalculateTimeoutValueNoExtra(int baseTime, int extra, int expected)
    {
        var target = new TimeoutValueCalculator(extra);

        target.CalculateTimeoutValue(baseTime).Should().Be(expected);
    }
}
