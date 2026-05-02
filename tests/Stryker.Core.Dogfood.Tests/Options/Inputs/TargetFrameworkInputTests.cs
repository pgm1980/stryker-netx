using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 67 (v2.53.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class TargetFrameworkInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new TargetFrameworkInput();
        target.HelpText.Should().Be("The framework to build the project against.");
    }

    [Fact]
    public void ShouldHaveDefaultNull()
    {
        var target = new TargetFrameworkInput { SuppliedInput = null };

        var result = target.Validate();

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldThrowOnEmptyInput(string input)
    {
        var target = new TargetFrameworkInput { SuppliedInput = input };

        var act = () => target.Validate();

        act.Should().Throw<InputException>().WithMessage(
            "Target framework cannot be empty. " +
            "Please provide a valid value from this list: " +
            "https://docs.microsoft.com/en-us/dotnet/standard/frameworks");
    }

    [Fact]
    public void ShouldReturnFramework()
    {
        var target = new TargetFrameworkInput { SuppliedInput = "netcoreapp3.1" };

        var result = target.Validate();

        result.Should().Be("netcoreapp3.1");
    }
}
