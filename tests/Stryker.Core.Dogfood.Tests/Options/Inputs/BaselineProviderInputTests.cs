using System.Collections.Generic;
using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 73 (v2.59.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BaselineProviderInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new BaselineProviderInput();
        target.HelpText.Should().Be("Choose a storage location for dashboard compare. Set to Dashboard provider when the dashboard reporter is turned on. | default: 'disk' | allowed: Dashboard, Disk, AzureFileStorage, S3");
    }

    [Fact]
    public void ShouldSetDefault_WhenBaselineIsDisabled()
    {
        var target = new BaselineProviderInput { SuppliedInput = "azurefilestorage" };

        var result = target.Validate([], false);

        result.Should().Be(BaselineProvider.Disk);
    }

    public static IEnumerable<object[]> EmptyOrBaselineReporters() =>
    [
        [System.Array.Empty<Reporter>()],
        [new[] { Reporter.Baseline }],
    ];

    [Theory]
    [MemberData(nameof(EmptyOrBaselineReporters))]
    public void ShouldSetDefault_WhenInputIsNullAndDashboardReporterIsNotEnabled(Reporter[] reporters)
    {
        var target = new BaselineProviderInput { SuppliedInput = null! };

        var result = target.Validate(reporters, true);

        result.Should().Be(BaselineProvider.Disk);
    }

    public static IEnumerable<object[]> DashboardReporters() =>
    [
        [new[] { Reporter.Dashboard }],
        [new[] { Reporter.Dashboard, Reporter.Baseline }],
    ];

    [Theory]
    [MemberData(nameof(DashboardReporters))]
    public void ShouldSetDashboard_WhenInputIsNullAndDashboardReporterIsEnabled(Reporter[] reporters)
    {
        var target = new BaselineProviderInput { SuppliedInput = null! };

        var result = target.Validate(reporters, true);

        result.Should().Be(BaselineProvider.Dashboard);
    }

    [Theory]
    [InlineData("disk")]
    [InlineData("Disk")]
    public void ShouldSetDisk(string value)
    {
        var target = new BaselineProviderInput { SuppliedInput = value };

        var result = target.Validate([Reporter.Dashboard], true);

        result.Should().Be(BaselineProvider.Disk);
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("Dashboard")]
    public void ShouldSetDashboard(string value)
    {
        var target = new BaselineProviderInput { SuppliedInput = value };

        var result = target.Validate([Reporter.Dashboard], true);

        result.Should().Be(BaselineProvider.Dashboard);
    }

    [Theory]
    [InlineData("azurefilestorage")]
    [InlineData("AzureFileStorage")]
    public void ShouldSetAzureFileStorage(string value)
    {
        var target = new BaselineProviderInput { SuppliedInput = value };

        var result = target.Validate([Reporter.Dashboard], true);

        result.Should().Be(BaselineProvider.AzureFileStorage);
    }

    [Theory]
    [InlineData("s3")]
    [InlineData("S3")]
    public void ShouldSetS3Storage(string value)
    {
        var target = new BaselineProviderInput { SuppliedInput = value };

        var result = target.Validate([Reporter.Dashboard], true);

        result.Should().Be(BaselineProvider.S3);
    }

    [Fact]
    public void ShouldThrowException_OnInvalidInput()
    {
        var target = new BaselineProviderInput { SuppliedInput = "invalid" };

        var act = () => target.Validate([Reporter.Dashboard], true);

        act.Should().Throw<InputException>()
            .WithMessage("Baseline storage provider 'invalid' does not exist");
    }
}
