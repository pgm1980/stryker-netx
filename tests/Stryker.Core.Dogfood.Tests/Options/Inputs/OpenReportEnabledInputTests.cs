using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class OpenReportEnabledInputTests
{
    [Fact]
    public void ShouldHaveNoHelpText()
    {
        var target = new OpenReportEnabledInput();
        target.HelpText.Should().Be(@" | default: 'False'");
    }

    [Fact]
    public void ShouldSetToTrue()
    {
        var target = new OpenReportEnabledInput { SuppliedInput = true };
        target.Validate().Should().BeTrue();
    }

    [Fact]
    public void ShouldSetToFalse()
    {
        var target = new OpenReportEnabledInput { SuppliedInput = false };
        target.Validate().Should().BeFalse();
    }
}
