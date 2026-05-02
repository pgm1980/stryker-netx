using System;
using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 73 (v2.59.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Note: tests use Environment.SetEnvironmentVariable (process-wide) — not parallelizable
/// with each other. xUnit by default runs each [Fact] in its own thread within a class but
/// these tests reset the env var in finally blocks.</summary>
public class DashboardApiKeyInputTests
{
    private const string StrykerDashboardApiKey = "STRYKER_DASHBOARD_API_KEY";

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DashboardApiKeyInput();
        target.HelpText.Should().Be("Api key for dashboard reporter.");
    }

    [Fact]
    public void ShouldThrowWhenNull()
    {
        var key = Environment.GetEnvironmentVariable(StrykerDashboardApiKey);
        var target = new DashboardApiKeyInput();
        try
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, string.Empty);

            var act = () => target.Validate(true, BaselineProvider.Dashboard, [Reporter.Dashboard]);

            act.Should().Throw<InputException>()
                .Which.Message.Should().Contain($"An API key is required when the {Reporter.Dashboard} reporter is turned on! You can get an API key at {DashboardUrlInput.DefaultUrl}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, key);
        }
    }

    [Fact]
    public void ShouldSkipValidationWhenDashboardNotEnabled()
    {
        var key = Environment.GetEnvironmentVariable(StrykerDashboardApiKey);
        var target = new DashboardApiKeyInput();
        try
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, string.Empty);

            var result = target.Validate(false, BaselineProvider.Disk, [Reporter.ClearText]);

            result.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, key);
        }
    }

    [Fact]
    public void ShouldTakeEnvironmentVariableValueWhenAvailable()
    {
        var key = Environment.GetEnvironmentVariable(StrykerDashboardApiKey);
        var target = new DashboardApiKeyInput();
        try
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, "my key");

            var result = target.Validate(true, BaselineProvider.Dashboard, [Reporter.Dashboard]);

            result.Should().Be("my key");
        }
        finally
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, key);
        }
    }

    [Fact]
    public void ShouldOverrideEnvironmentVariableWhenInputSupplied()
    {
        var key = Environment.GetEnvironmentVariable(StrykerDashboardApiKey);
        var target = new DashboardApiKeyInput { SuppliedInput = "my key" };
        try
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, "not my key");

            var result = target.Validate(true, BaselineProvider.Dashboard, [Reporter.Dashboard]);

            result.Should().Be("my key");
        }
        finally
        {
            Environment.SetEnvironmentVariable(StrykerDashboardApiKey, key);
        }
    }
}
