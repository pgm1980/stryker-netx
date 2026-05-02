using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 70 (v2.56.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MutationLevelInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new MutationLevelInput();
        target.HelpText.Should().Be("Specify which mutation levels to place. Every higher level includes the mutations from the lower levels. | default: 'Standard' | allowed: Basic, Standard, Advanced, Complete");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new MutationLevelInput { SuppliedInput = null! };

        var result = target.Validate();

        result.Should().Be(MutationLevel.Standard);
    }

    [Fact]
    public void ShouldThrowOnInvalidMutationLevel()
    {
        var target = new MutationLevelInput { SuppliedInput = "gibberish" };

        var act = () => target.Validate();

        act.Should().Throw<InputException>()
            .WithMessage("The given mutation level (gibberish) is invalid. Valid options are: [Basic, Standard, Advanced, Complete]");
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("Complete")]
    public void ShouldReturnMutationLevel(string input)
    {
        var target = new MutationLevelInput { SuppliedInput = input };

        var result = target.Validate();

        result.Should().Be(MutationLevel.Complete);
    }
}
