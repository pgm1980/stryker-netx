using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using FluentAssertions;
using Stryker.CLI.Logging;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.CLI.Tests.Logging;

/// <summary>
/// Sprint 37 (v2.24.0) port of upstream stryker-net 4.14.1
/// src/Stryker.CLI/Stryker.CLI.UnitTest/Logging/InputBuilderTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class InputBuilderTests
{
    [Fact]
    public void ShouldAddGitIgnore()
    {
        var fileSystemMock = new MockFileSystem();
        var basePath = Directory.GetCurrentDirectory();
        var target = new LoggingInitializer();

        var inputs = new StrykerInputs();
        inputs.BasePathInput.SuppliedInput = basePath;
        target.SetupLogOptions(inputs, fileSystemMock);

        var gitIgnoreFile =
            fileSystemMock.AllFiles.Single(x => x.EndsWith(Path.Combine(".gitignore"), StringComparison.Ordinal));
        gitIgnoreFile.Should().NotBeNull();
        DateTime.TryParse(Directory.GetParent(gitIgnoreFile)!.Name.Split(".")[0], CultureInfo.InvariantCulture, out _).Should().BeTrue();
        var fileContents = fileSystemMock.GetFile(gitIgnoreFile).Contents;
        Encoding.Default.GetString(fileContents).Should().Be("*");
    }

    [Fact]
    public void ShouldAddGitIgnoreWithAbsolutePath()
    {
        var fileSystemMock = new MockFileSystem();
        var target = new LoggingInitializer();

        var inputs = new StrykerInputs();
        inputs.OutputPathInput.SuppliedInput = Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "tmp", "path");
        target.SetupLogOptions(inputs, fileSystemMock);

        var gitIgnoreFile =
            fileSystemMock.AllFiles.FirstOrDefault(x => x.EndsWith(Path.Combine("tmp", "path", ".gitignore"), StringComparison.Ordinal));
        gitIgnoreFile.Should().NotBeNull();
        var fileContents = fileSystemMock.GetFile(gitIgnoreFile).Contents;
        Encoding.Default.GetString(fileContents).Should().Be("*");
    }

    [Fact]
    public void ShouldAddGitIgnoreWithRelativePath()
    {
        var fileSystemMock = new MockFileSystem();
        var basePath = Directory.GetCurrentDirectory();
        var target = new LoggingInitializer();

        var inputs = new StrykerInputs();
        inputs.BasePathInput.SuppliedInput = basePath;
        inputs.OutputPathInput.SuppliedInput = "output";
        target.SetupLogOptions(inputs, fileSystemMock);

        var gitIgnoreFile =
            fileSystemMock.AllFiles.FirstOrDefault(x => x.EndsWith(Path.Combine("output", ".gitignore"), StringComparison.Ordinal));
        gitIgnoreFile.Should().NotBeNull();
        var fileContents = fileSystemMock.GetFile(gitIgnoreFile).Contents;
        Encoding.Default.GetString(fileContents).Should().Be("*");
    }
}
