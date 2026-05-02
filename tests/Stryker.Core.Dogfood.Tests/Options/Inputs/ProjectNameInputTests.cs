using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ProjectNameInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ProjectNameInput();
        // Cross-platform line-ending tolerance (Sprint 53 lesson)
        target.HelpText.Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("The organizational name for your project. Required when dashboard reporter is turned on.\nFor example: Your project might be called 'consumer-loans' and it might contains sub-modules 'consumer-loans-frontend' and 'consumer-loans-backend'. | default: ''");
    }

    [Fact]
    public void ShouldReturnName()
    {
        var input = new ProjectNameInput { SuppliedInput = "name" };

        var result = input.Validate();

        result.Should().Be("name");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var input = new ProjectNameInput { SuppliedInput = null! };

        var result = input.Validate();

        result.Should().Be(string.Empty);
    }
}
