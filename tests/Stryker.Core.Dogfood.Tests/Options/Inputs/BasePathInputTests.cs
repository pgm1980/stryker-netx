using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 63 (v2.49.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BasePathInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new BasePathInput();
        target.HelpText.Should().Be("The path from which stryker is started.");
    }

    [Fact]
    public void ShouldAllowExistingDir()
    {
        var target = new BasePathInput { SuppliedInput = "C:/MyDir/" };
        var fileSystemMock = new MockFileSystem();
        fileSystemMock.AddDirectory("C:/MyDir/");

        var result = target.Validate(fileSystemMock);

        result.Should().Be("C:/MyDir/");
    }

    [Fact]
    public void ShouldThrowOnNonexistentDir()
    {
        var target = new BasePathInput { SuppliedInput = "C:/MyDir/" };
        var fileSystemMock = new MockFileSystem();

        var act = () => target.Validate(fileSystemMock);

        act.Should().Throw<InputException>().WithMessage("Base path must exist.");
    }

    [Fact]
    public void ShouldThrowOnNull()
    {
        var target = new BasePathInput { SuppliedInput = null };
        var fileSystemMock = new MockFileSystem();

        var act = () => target.Validate(fileSystemMock);

        act.Should().Throw<InputException>().WithMessage("Base path can't be null or empty.");
    }
}
