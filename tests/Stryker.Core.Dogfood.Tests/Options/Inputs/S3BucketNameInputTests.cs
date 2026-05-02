using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 71 (v2.57.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class S3BucketNameInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new S3BucketNameInput();
        target.HelpText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenProviderNotS3()
    {
        var target = new S3BucketNameInput { SuppliedInput = null! };

        var result = target.Validate(BaselineProvider.Dashboard, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenBaselineIsDisabled()
    {
        var target = new S3BucketNameInput { SuppliedInput = "my-bucket" };

        var result = target.Validate(BaselineProvider.S3, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnBucketName_WhenValid()
    {
        var target = new S3BucketNameInput { SuppliedInput = "my-stryker-bucket" };

        var result = target.Validate(BaselineProvider.S3, true);

        result.Should().Be("my-stryker-bucket");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldThrowException_WhenBucketNameMissing(string? input)
    {
        var target = new S3BucketNameInput { SuppliedInput = input! };

        var act = () => target.Validate(BaselineProvider.S3, true);

        act.Should().Throw<InputException>()
            .WithMessage("The S3 bucket name is required when S3 is used as the baseline provider.");
    }
}
