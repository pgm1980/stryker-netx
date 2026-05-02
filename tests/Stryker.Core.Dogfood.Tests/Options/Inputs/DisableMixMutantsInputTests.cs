using FluentAssertions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class DisableMixMutantsInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DisableMixMutantsInput();
        target.HelpText.Should().Be(@"Test each mutation in an isolated test run. | default: 'False'");
    }

    [Theory]
    [InlineData(false, OptimizationModes.None)]
    [InlineData(true, OptimizationModes.DisableMixMutants)]
    [InlineData(null, OptimizationModes.None)]
    public void ShouldValidate(bool? input, OptimizationModes expected)
    {
        var target = new DisableMixMutantsInput { SuppliedInput = input };

        var result = target.Validate();

        result.Should().Be(expected);
    }
}
