using System;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class DashboardUrlInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DashboardUrlInput();
        target.HelpText.Should().Be(@"Alternative url for Stryker Dashboard. | default: 'https://dashboard.stryker-mutator.io'");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new DashboardUrlInput { SuppliedInput = null! };
        target.Validate().Should().Be("https://dashboard.stryker-mutator.io");
    }

    [Fact]
    public void ShouldAllowValidUri()
    {
        var target = new DashboardUrlInput { SuppliedInput = "http://example.com:8042" };
        target.Validate().Should().Be("http://example.com:8042");
    }

    [Fact]
    public void ShouldThrowOnInvalidUri()
    {
        var target = new DashboardUrlInput { SuppliedInput = "test" };

        Action act = () => target.Validate();
        act.Should().Throw<InputException>()
            .Which.Message.Should().Be("Stryker dashboard url 'test' is invalid.");
    }
}
