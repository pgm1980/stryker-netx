using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 63 (v2.49.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class AdditionalTimeoutMsInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new AdditionalTimeoutInput();
        target.HelpText.Should().Be(
            "A timeout is calculated based on the initial unit test run before mutating.\nTo prevent infinite loops Stryker cancels a testrun if it runs longer than the timeout value.\nIf you experience a lot of timeouts you might need to increase the timeout value. | default: '5000'"
                .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldAllowZero()
    {
        var target = new AdditionalTimeoutInput { SuppliedInput = 0 };
        var result = target.Validate();
        result.Should().Be(0);
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new AdditionalTimeoutInput { SuppliedInput = null };
        var result = target.Validate();
        result.Should().Be(5000);
    }

    [Fact]
    public void ShouldAllowMillion()
    {
        var target = new AdditionalTimeoutInput { SuppliedInput = 1000000 };
        var result = target.Validate();
        result.Should().Be(1000000);
    }

    [Fact]
    public void ShouldThrowAtNegative()
    {
        var target = new AdditionalTimeoutInput { SuppliedInput = -1 };
        var act = () => target.Validate();
        act.Should().Throw<InputException>().WithMessage("Timeout cannot be negative.");
    }
}
