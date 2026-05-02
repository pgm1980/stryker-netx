using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BreakOnInitialTestFailureInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new BreakOnInitialTestFailureInput();
        target.HelpText.Should().Be(@"Instruct Stryker to break execution when at least one test failed on initial run. | default: 'False'");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ShouldTranslateInputToExpectedResult(bool? argValue, bool expected)
    {
        var validatedInput = new BreakOnInitialTestFailureInput { SuppliedInput = argValue }.Validate();

        validatedInput.Should().Be(expected);
    }
}
