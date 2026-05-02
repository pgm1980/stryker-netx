using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 74 (v2.60.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class SolutionInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new SolutionInput();
        target.HelpText.Should().Be("Full path to your solution file. Required on dotnet framework.");
    }

    [Theory]
    [InlineData("solution.sln")]
    [InlineData("solution.slnx")]
    [InlineData("solution.sLNx")]
    public void ShouldReturnSolutionPathIfExists(string solutionFileName)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        var path = fileSystem.Path.Combine(dir, solutionFileName);
        fileSystem.AddDirectory(dir);
        fileSystem.AddFile(path, new MockFileData(""));

        var input = new SolutionInput { SuppliedInput = path };

        input.Validate(dir, fileSystem).Should().Be(path);
    }

    [Fact]
    public void ShouldReturnFullPathWhenRelativePathGiven()
    {
        var dir = Directory.GetCurrentDirectory();
        var relativePath = "./solution.sln";
        var fullPath = Path.Combine(dir, "solution.sln");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(dir);
        fileSystem.AddFile(fullPath, new MockFileData(""));
        fileSystem.Directory.SetCurrentDirectory(dir);

        var input = new SolutionInput { SuppliedInput = relativePath };

        input.Validate(dir, fileSystem).Should().Be(fullPath);
    }

    [Theory]
    [InlineData("solution.sln")]
    [InlineData("solution.slnx")]
    public void ShouldDiscoverSolutionFileIfSolutionPathIsNotSupplied(string solutionName)
    {
        var input = new SolutionInput { SuppliedInput = null };
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        var fullPath = fileSystem.Path.Combine(dir, solutionName);
        fileSystem.AddDirectory(dir);
        fileSystem.AddFile(fullPath, new MockFileData(""));
        fileSystem.Directory.SetCurrentDirectory(dir);

        input.Validate(dir, fileSystem).Should().Be(fullPath);
    }

    [Fact]
    public void ShouldThrowWhenMultipleSolutionFilesAreDiscovered()
    {
        var input = new SolutionInput { SuppliedInput = null };
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        var solution1 = Path.Combine(dir, "solution1.sln");
        var solution2 = Path.Combine(dir, "solution2.slnx");
        fileSystem.AddDirectory(dir);
        fileSystem.AddFile(solution1, new MockFileData(""));
        fileSystem.AddFile(solution2, new MockFileData(""));
        fileSystem.Directory.SetCurrentDirectory(dir);
        var errorMessage =
$"Expected exactly one solution file (.sln or .slnx), found more than one:\n{solution1}\n{solution2}\n"
            .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal);

        var act = () => input.Validate(dir, fileSystem);

        act.Should().Throw<InputException>().WithMessage(errorMessage);
    }

    [Fact]
    public void ShouldBeEmptyWhenNullAndCantBeDiscovered()
    {
        var input = new SolutionInput { SuppliedInput = null };
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(dir);
        fileSystem.Directory.SetCurrentDirectory(dir);

        input.Validate(dir, fileSystem).Should().BeNull();
    }

    [Fact]
    public void ShouldThrowWhenNotExists()
    {
        var input = new SolutionInput { SuppliedInput = "/c/root/bla/solution.sln" };
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(dir);

        var act = () => input.Validate(dir, fileSystem);

        act.Should().Throw<InputException>().WithMessage("Given path does not exist: /c/root/bla/solution.sln");
    }

    [Fact]
    public void ShouldThrowWhenPathIsNoSolutionFile()
    {
        var input = new SolutionInput { SuppliedInput = "/c/root/bla/solution.csproj" };
        var dir = Directory.GetCurrentDirectory();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(dir);

        var act = () => input.Validate(dir, fileSystem);

        act.Should().Throw<InputException>().WithMessage("Given path is not a solution file: /c/root/bla/solution.csproj");
    }
}
