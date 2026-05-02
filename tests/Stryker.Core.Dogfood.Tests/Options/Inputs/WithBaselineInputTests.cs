using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class WithBaselineInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new WithBaselineInput();
        target.HelpText.Should().Be(@"EXPERIMENTAL: Use results stored in stryker dashboard to only test new mutants. | default: 'False'");
    }

    [Fact]
    public void ShouldBeEnabledWhenTrue()
    {
        var target = new WithBaselineInput { SuppliedInput = true };
        target.Validate().Should().BeTrue();
    }

    [Fact]
    public void ShouldProvideDefaultFalseWhenNull()
    {
        var target = new WithBaselineInput { SuppliedInput = null };
        target.Validate().Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEnabledWhenFalse()
    {
        var target = new WithBaselineInput { SuppliedInput = false };
        target.Validate().Should().BeFalse();
    }
}
