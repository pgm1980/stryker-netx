using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 71 (v2.57.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class S3EndpointInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new S3EndpointInput();
        target.HelpText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenProviderNotS3()
    {
        var target = new S3EndpointInput { SuppliedInput = "https://minio.example.com" };

        var result = target.Validate(BaselineProvider.Dashboard, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenBaselineIsDisabled()
    {
        var target = new S3EndpointInput { SuppliedInput = "https://minio.example.com" };

        var result = target.Validate(BaselineProvider.S3, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnEndpoint_WhenValid()
    {
        var target = new S3EndpointInput { SuppliedInput = "https://minio.example.com:9000" };

        var result = target.Validate(BaselineProvider.S3, true);

        result.Should().Be("https://minio.example.com:9000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldReturnDefault_WhenEndpointNotProvided(string? input)
    {
        var target = new S3EndpointInput { SuppliedInput = input! };

        var result = target.Validate(BaselineProvider.S3, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldThrowException_OnInvalidUri()
    {
        var target = new S3EndpointInput { SuppliedInput = "not-a-url" };

        var act = () => target.Validate(BaselineProvider.S3, true);

        act.Should().Throw<InputException>()
            .WithMessage("The S3 endpoint is not a valid Uri: not-a-url");
    }
}
