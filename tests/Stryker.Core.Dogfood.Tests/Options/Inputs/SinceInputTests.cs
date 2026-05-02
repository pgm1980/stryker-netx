using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 72 (v2.58.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class SinceInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new SinceInput();
        target.HelpText.Should().Be("Enables diff compare. Only test changed files. | default: 'False'");
    }

    [Fact]
    public void ShouldBeEnabledWhenTrue()
    {
        var target = new SinceInput { SuppliedInput = true };

        var result = target.Validate(withBaseline: null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeEnabledWhenTrueEvenIfWithBaselineFalse()
    {
        var target = new SinceInput { SuppliedInput = true };

        var result = target.Validate(withBaseline: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProvideDefaultWhenNull()
    {
        var target = new SinceInput();

        var result = target.Validate(withBaseline: null);

        result.Should().Be(target.Default!.Value);
    }

    [Fact]
    public void ShouldNotBeEnabledWhenFalse()
    {
        var target = new SinceInput { SuppliedInput = false };

        var result = target.Validate(withBaseline: null);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeImplicitlyEnabledWithBaseline()
    {
        var sinceEnabled = new SinceInput().Validate(withBaseline: true);

        sinceEnabled.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeAllowedToExplicitlyEnableWithBaseline()
    {
        var sinceEnabled = new SinceInput { SuppliedInput = true };

        var act = () => sinceEnabled.Validate(withBaseline: true);

        act.Should().Throw<InputException>().WithMessage("The since and baseline features are mutually exclusive.");
    }
}
