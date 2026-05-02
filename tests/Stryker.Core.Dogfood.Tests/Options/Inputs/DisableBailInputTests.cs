using FluentAssertions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class DisableBailInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DisableBailInput();
        target.HelpText.Should().Be(@"Disable abort unit testrun as soon as the first unit test fails. | default: 'False'");
    }

    [Theory]
    [InlineData(false, OptimizationModes.None)]
    [InlineData(true, OptimizationModes.DisableBail)]
    [InlineData(null, OptimizationModes.None)]
    public void ShouldValidate(bool? input, OptimizationModes expected)
    {
        var target = new DisableBailInput { SuppliedInput = input };

        var result = target.Validate();

        result.Should().Be(expected);
    }
}
