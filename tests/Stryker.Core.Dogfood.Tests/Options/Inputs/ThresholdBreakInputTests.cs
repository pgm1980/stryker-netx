using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 63 (v2.49.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ThresholdBreakInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ThresholdBreakInput();
        target.HelpText.Should().Be("Anything below this mutation score will return a non-zero exit code. Must be less than or equal to threshold low. | default: '0' | allowed: 0 - 100");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ShouldValidateThresholdBreak(int thresholdBreak)
    {
        var act = () => new ThresholdBreakInput { SuppliedInput = thresholdBreak }.Validate(low: 50);
        act.Should().Throw<InputException>().WithMessage("Threshold break must be between 0 and 100.");
    }

    [Fact]
    public void ThresholdBreakShouldBeLowerThanOrEqualToThresholdLow()
    {
        var act = () => new ThresholdBreakInput { SuppliedInput = 51 }.Validate(low: 50);
        act.Should().Throw<InputException>()
            .WithMessage("Threshold break must be less than or equal to threshold low. Current break: 51, low: 50.");
    }

    [Fact]
    public void CanBeEqualToThresholdLow()
    {
        var input = 60;
        var options = new ThresholdBreakInput { SuppliedInput = input }.Validate(low: 60);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldAllow100PercentBreak()
    {
        var result = new ThresholdBreakInput { SuppliedInput = 100 }.Validate(low: 100);
        result.Should().Be(100, "because some people will not allow any mutations in their projects.");
    }

    [Fact]
    public void ShouldAllow0PercentBreak()
    {
        var result = new ThresholdBreakInput { SuppliedInput = 0 }.Validate(low: 100);
        result.Should().Be(0, "because some users will want to break only on literally 0.00 percent score.");
    }

    [Fact]
    public void ShouldBeDefaultValueWhenNull()
    {
        var input = new ThresholdBreakInput { SuppliedInput = null };
        var options = input.Validate(low: 80);
        options.Should().Be(input.Default!.Value);
    }
}
