using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 68 (v2.54.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ThresholdLowInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ThresholdLowInput();
        target.HelpText.Should().Be("Minimum acceptable mutation score. Must be less than or equal to threshold high and more than or equal to threshold break. | default: '60' | allowed: 0 - 100");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void MustBeBetween0and100(int thresholdLow)
    {
        var act = () => new ThresholdLowInput { SuppliedInput = thresholdLow }.Validate(@break: 0, high: 100);
        act.Should().Throw<InputException>().WithMessage("Threshold low must be between 0 and 100.");
    }

    [Fact]
    public void MustBeLessthanOrEqualToThresholdHigh()
    {
        var act = () => new ThresholdLowInput { SuppliedInput = 61 }.Validate(@break: 60, high: 60);
        act.Should().Throw<InputException>()
            .WithMessage("Threshold low must be less than or equal to threshold high. Current low: 61, high: 60.");
    }

    [Fact]
    public void MustBeMoreThanThresholdBreak()
    {
        var act = () => new ThresholdLowInput { SuppliedInput = 59 }.Validate(@break: 60, high: 60);
        act.Should().Throw<InputException>()
            .WithMessage("Threshold low must be more than or equal to threshold break. Current low: 59, break: 60.");
    }

    [Fact]
    public void CanBeEqualToThresholdBreak()
    {
        var input = 60;
        var options = new ThresholdLowInput { SuppliedInput = input }.Validate(@break: 60, high: 100);
        options.Should().Be(input);
    }

    [Fact]
    public void CanBeEqualToThresholdHigh()
    {
        var input = 60;
        var options = new ThresholdLowInput { SuppliedInput = input }.Validate(@break: 0, high: 60);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldAllow0()
    {
        var input = 0;
        var options = new ThresholdLowInput { SuppliedInput = input }.Validate(@break: 0, high: 100);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldAllow100()
    {
        var input = 100;
        var options = new ThresholdLowInput { SuppliedInput = input }.Validate(@break: 0, high: 100);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldBeDefaultValueWhenNull()
    {
        var input = new ThresholdLowInput { SuppliedInput = null };
        var options = input.Validate(@break: 0, high: 80);
        options.Should().Be(input.Default!.Value);
    }
}
