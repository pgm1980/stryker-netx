using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 68 (v2.54.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ThresholdHighInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ThresholdHighInput();
        target.HelpText.Should().Be("Minimum good mutation score. Must be higher than or equal to threshold low. | default: '80' | allowed: 0 - 100");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void MustBeBetween0and100(int thresholdHigh)
    {
        var act = () => new ThresholdHighInput { SuppliedInput = thresholdHigh }.Validate(low: 0);
        act.Should().Throw<InputException>().WithMessage("Threshold high must be between 0 and 100.");
    }

    [Fact]
    public void MustBeMoreThanOrEqualToThresholdLow()
    {
        var act = () => new ThresholdHighInput { SuppliedInput = 59 }.Validate(low: 60);
        act.Should().Throw<InputException>()
            .WithMessage("Threshold high must be higher than or equal to threshold low. Current high: 59, low: 60.");
    }

    [Fact]
    public void CanBeEqualToThresholdLow()
    {
        var input = 60;
        var options = new ThresholdHighInput { SuppliedInput = input }.Validate(low: 60);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldAllow0()
    {
        var input = 0;
        var options = new ThresholdHighInput { SuppliedInput = input }.Validate(low: 0);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldAllow100()
    {
        var input = 100;
        var options = new ThresholdHighInput { SuppliedInput = input }.Validate(low: 60);
        options.Should().Be(input);
    }

    [Fact]
    public void ShouldBeDefaultValueWhenNull()
    {
        var input = new ThresholdHighInput { SuppliedInput = null };
        var options = input.Validate(low: 60);
        options.Should().Be(input.Default!.Value);
    }
}
