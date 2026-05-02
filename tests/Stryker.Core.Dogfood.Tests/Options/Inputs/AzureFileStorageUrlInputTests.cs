using FluentAssertions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 72 (v2.58.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class AzureFileStorageUrlInputTests
{
    private const string ValidUrlInput = "http://example.com:8042";

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new AzureFileStorageUrlInput();
        target.HelpText.Should().Be(
            "The url for the Azure File Storage is only needed when the Azure baseline provider is selected.\nThe url should look something like this:\nhttps://STORAGE_NAME.file.core.windows.net/FILE_SHARE_NAME\nNote, the url might be different depending on where your file storage is hosted. | default: ''"
                .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldReturnDefault_WhenProviderNotAzureFileStorage()
    {
        var target = new AzureFileStorageUrlInput { SuppliedInput = null! };

        var result = target.Validate(BaselineProvider.Dashboard, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnDefault_WhenBaselineIsDisabled()
    {
        var target = new AzureFileStorageUrlInput { SuppliedInput = null! };

        var result = target.Validate(BaselineProvider.AzureFileStorage, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldAllowUri()
    {
        var target = new AzureFileStorageUrlInput { SuppliedInput = ValidUrlInput };

        var result = target.Validate(BaselineProvider.AzureFileStorage, true);

        result.Should().Be("http://example.com:8042");
    }

    [Fact]
    public void ShouldThrowException_WhenAzureStorageUrlAndSASNull()
    {
        var target = new AzureFileStorageUrlInput { SuppliedInput = null! };

        var act = () => target.Validate(BaselineProvider.AzureFileStorage, true);

        act.Should().Throw<InputException>()
            .WithMessage("The Azure File Storage url is required when Azure File Storage is used for dashboard compare.");
    }

    [Fact]
    public void ShouldThrowException_OnInvalidUri()
    {
        var target = new AzureFileStorageUrlInput { SuppliedInput = "test" };

        var act = () => target.Validate(BaselineProvider.AzureFileStorage, true);

        act.Should().Throw<InputException>()
            .WithMessage("The Azure File Storage url is not a valid Uri: test");
    }
}
