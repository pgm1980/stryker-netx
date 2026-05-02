using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class DiagModeInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DiagModeInput();
        // Cross-platform line-ending tolerance (Sprint 53 lesson)
        target.HelpText.Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("Stryker enters diagnostic mode. Useful when encountering issues.\nSetting this flag makes Stryker increase the debug level and log more information to help troubleshooting. | default: 'False'");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(null, false)]
    public void ShouldValidate(bool? input, bool expected)
    {
        var target = new DiagModeInput { SuppliedInput = input };

        var result = target.Validate();

        result.Should().Be(expected);
    }
}
