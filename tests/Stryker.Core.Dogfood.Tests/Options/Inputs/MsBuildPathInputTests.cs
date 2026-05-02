using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 70 (v2.56.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MsBuildPathInputTests
{
    private readonly MockFileSystem _fileSystemMock = new();

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new MsBuildPathInput();
        target.HelpText.Should().Be("The path to the msbuild executable to use to build your .NET Framework application. Not used for .net (core).");
    }

    [Fact]
    public void ShouldReturnValidMsBuildPath()
    {
        var path = "C:/bla/test.exe";
        var target = new MsBuildPathInput { SuppliedInput = path };
        _fileSystemMock.AddFile(path, new MockFileData(""));

        var result = target.Validate(_fileSystemMock);
        result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("C:/bla/test")]
    [InlineData("test")]
    public void ShouldThrowOnNonExistingPath(string path)
    {
        var target = new MsBuildPathInput { SuppliedInput = path };

        var act = () => target.Validate(_fileSystemMock);

        act.Should().Throw<InputException>()
            .WithMessage($"Given MsBuild path '{path}' does not exist. Either provide a valid msbuild path or let stryker locate msbuild automatically.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldThrowOnEmptyValue(string value)
    {
        var target = new MsBuildPathInput { SuppliedInput = value };

        var act = () => target.Validate(_fileSystemMock);

        act.Should().Throw<InputException>()
            .WithMessage("MsBuild path cannot be empty. Either provide a valid msbuild path or let stryker locate msbuild automatically.");
    }
}
