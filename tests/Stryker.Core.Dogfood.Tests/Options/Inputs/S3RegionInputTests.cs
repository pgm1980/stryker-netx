using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 71 (v2.57.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class S3RegionInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new S3RegionInput();
        target.HelpText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenProviderNotS3()
    {
        var target = new S3RegionInput { SuppliedInput = "us-east-1" };

        var result = target.Validate(BaselineProvider.Dashboard, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenBaselineIsDisabled()
    {
        var target = new S3RegionInput { SuppliedInput = "us-east-1" };

        var result = target.Validate(BaselineProvider.S3, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnRegion_WhenValid()
    {
        var target = new S3RegionInput { SuppliedInput = "eu-west-1" };

        var result = target.Validate(BaselineProvider.S3, true);

        result.Should().Be("eu-west-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldReturnDefault_WhenRegionNotProvided(string? input)
    {
        var target = new S3RegionInput { SuppliedInput = input! };

        var result = target.Validate(BaselineProvider.S3, true);

        result.Should().BeEmpty();
    }
}
