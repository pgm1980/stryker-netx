using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 67 (v2.53.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class SinceTargetInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new SinceTargetInput();
        target.HelpText.Should().Be("The target branch/commit to compare with the current codebase when the since feature is enabled. | default: 'master'");
    }

    [Fact]
    public void ShouldUseSuppliedInputWhenSinceEnabled()
    {
        var suppliedInput = "develop";
        var validatedSinceBranch = new SinceTargetInput { SuppliedInput = suppliedInput }.Validate(sinceEnabled: true);
        validatedSinceBranch.Should().Be(suppliedInput);
    }

    [Fact]
    public void ShouldUseDefaultWhenSinceEnabledAndInputNull()
    {
        var input = new SinceTargetInput();
        var validatedSinceBranch = input.Validate(sinceEnabled: true);
        validatedSinceBranch.Should().Be(input.Default);
    }

    [Fact]
    public void MustNotBeEmptyStringWhenSinceEnabled()
    {
        var act = () => new SinceTargetInput { SuppliedInput = "" }.Validate(sinceEnabled: true);

        act.Should().Throw<InputException>().WithMessage("The since target cannot be empty when the since feature is enabled");
    }

    [Fact]
    public void ShouldNotValidateSinceTargetWhenSinceDisabled()
    {
        var validatedSinceBranch = new SinceTargetInput { SuppliedInput = "develop" }.Validate(sinceEnabled: false);
        validatedSinceBranch.Should().Be("master");
    }
}
