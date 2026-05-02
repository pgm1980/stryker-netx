using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 68 (v2.54.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class OutputPathInputTests
{
    private readonly MockFileSystem _fileSystemMock = new();

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new OutputPathInput();
        target.HelpText.Should().Be("Changes the output path for Stryker logs and reports. This can be an absolute or relative path.");
    }

    [Theory]
    [InlineData("C:/bla/test")]
    [InlineData("test")]
    public void ShouldReturnValidOutputPath(string outputPath)
    {
        var target = new OutputPathInput { SuppliedInput = outputPath };
        _fileSystemMock.AddDirectory(outputPath);

        var result = target.Validate(_fileSystemMock);

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("C:/bla/test")]
    [InlineData("test")]
    public void ShouldThrowOnNonExistingPath(string outputPath)
    {
        var target = new OutputPathInput { SuppliedInput = outputPath };

        var act = () => target.Validate(_fileSystemMock);

        act.Should().Throw<InputException>().WithMessage("Outputpath should exist");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldThrowOnEmptyValue(string? value)
    {
        var target = new OutputPathInput { SuppliedInput = value! };

        var act = () => target.Validate(_fileSystemMock);

        act.Should().Throw<InputException>().WithMessage("Outputpath can't be null or whitespace");
    }
}
