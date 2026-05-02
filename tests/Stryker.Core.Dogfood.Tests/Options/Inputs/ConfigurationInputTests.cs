using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ConfigurationInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ConfigurationInput();
        target.HelpText.Should().Be("Configuration to use when building the project(s) (e.g., 'Debug' or 'Release'). If not specified, the default configuration of the project(s) will be used.");
    }

    [Fact]
    public void ShouldReturnSuppliedInput()
    {
        var target = new ConfigurationInput { SuppliedInput = "Debug" };

        var result = target.Validate();

        result.Should().Be("Debug");
    }

    [Fact]
    public void ShouldReturnDefault()
    {
        var target = new ConfigurationInput { SuppliedInput = null };

        var result = target.Validate();

        result.Should().BeNull();
    }
}
