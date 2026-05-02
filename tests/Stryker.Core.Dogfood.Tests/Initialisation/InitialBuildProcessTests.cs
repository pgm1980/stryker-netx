using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Core.Helpers.ProcessUtil;
using Stryker.Core.Initialisation;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 84 (v2.70.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Windows-only tests skipped via xUnit-native [Fact(Skip = ...)] when running on non-Windows
/// platforms (upstream's [TestMethodWithIgnoreIfSupport] + [IgnoreIf(nameof(Is.Unix))] is MSTest-specific).</summary>
public class InitialBuildProcessTests : TestBase
{
    private readonly string _cProjectsExampleCsproj = OperatingSystem.IsWindows() ? @"C:\Projects \Example.csproj" : "/usr/projects/Example.csproj";

    private readonly MockFileSystem _mockFileSystem = new(new Dictionary<string, MockFileData>(StringComparer.Ordinal)
    {
        [@"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"] = new("msbuild code"),
    });

    [Fact]
    public void InitialBuildProcess_ShouldThrowStrykerInputExceptionOnFail()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processMock.SetupProcessMockToReturn("", 1);

        var target = new InitialBuildProcess(processMock.Object, _mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        var act = () => target.InitialBuild(false, _cProjectsExampleCsproj, null!);

        act.Should().Throw<InputException>()
            .Which.Details.Should().Be("Initial build of targeted project failed. Please make sure the targeted project is buildable. You can reproduce this error yourself using: \"dotnet build Example.csproj\"");
    }

    [Fact(Skip = "Windows-only (DotnetFramework + MSBuild.exe path test). xUnit lacks IgnoreIf.")]
    public void InitialBuildProcess_WithPathAsBuildCommand_ShouldThrowStrykerInputExceptionOnFailWithQuotes()
    {
        // S1186: skip placeholder for Windows-only DotnetFramework MSBuild.exe path test.
    }

    [Fact(Skip = "Windows-only (DotnetFramework). xUnit lacks IgnoreIf.")]
    public void InitialBuildProcess_WithPathAsBuildCommand_TriesWithMsBuildIfDotnetFails()
    {
        // S1186: skip placeholder for Windows-only test.
    }

    [Fact]
    public void InitialBuildProcess_ShouldNotThrowExceptionOnSuccess()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processMock.SetupProcessMockToReturn("");

        var target = new InitialBuildProcess(processMock.Object, _mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        target.InitialBuild(false, "/", "/");

        processMock.Verify(p => p.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), 0), Times.Once);
    }

    [Theory(Skip = "Windows-only (DotnetFramework MSBuild.exe path). xUnit lacks IgnoreIf.")]
    [InlineData(@"C:\Windows\Microsoft.Net\Framework64\v2.0.50727\MSBuild.exe")]
    [InlineData(@"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe")]
    [InlineData(@"C:\Windows\Microsoft.Net\Framework\v2.0.50727\MSBuild.exe")]
    public void InitialBuildProcess_ShouldRunMsBuildOnDotnetFramework(string msBuildLocation) { _ = msBuildLocation; }

    [Fact(Skip = "Windows-only (DotnetFramework). xUnit lacks IgnoreIf.")]
    public void InitialBuildProcess_ShouldUseCustomMsbuildIfNotNull()
    {
        // S1186: skip placeholder for Windows-only test.
    }

    [Fact]
    public void InitialBuildProcess_ShouldUseProvidedConfiguration()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        var mockFileSystem = new MockFileSystem();
        processMock.SetupProcessMockToReturn("");

        var target = new InitialBuildProcess(processMock.Object, mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        target.InitialBuild(false, "/", "./ExampleProject.sln", "TheDebug");

        processMock.Verify(x => x.Start(It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(argumentsParam => argumentsParam.Contains("-c TheDebug", StringComparison.Ordinal)),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void InitialBuildProcess_ShouldUseProvidedPlatform()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        var mockFileSystem = new MockFileSystem();
        processMock.SetupProcessMockToReturn("");

        var target = new InitialBuildProcess(processMock.Object, mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        target.InitialBuild(false, "/", "./ExampleProject.sln", "TheDebug", "AnyCPU");

        processMock.Verify(x => x.Start(It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(argumentsParam => argumentsParam.Contains("-c TheDebug", StringComparison.Ordinal)
                                                && argumentsParam.Contains("--property:Platform=AnyCPU", StringComparison.Ordinal)),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void InitialBuildProcess_ShouldRunDotnetBuildIfNotDotnetFramework()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processMock.SetupProcessMockToReturn("");

        var target = new InitialBuildProcess(processMock.Object, _mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        target.InitialBuild(false, "./ExampleProject.csproj", null!);

        processMock.Verify(x => x.Start(It.IsAny<string>(),
                It.Is<string>(applicationParam => applicationParam.Contains("dotnet", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(argumentsParam => argumentsParam.Contains("build", StringComparison.Ordinal)),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void InitialBuildProcess_ShouldUseSolutionPathIfSet()
    {
        var processMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processMock.SetupProcessMockToReturn("");

        var target = new InitialBuildProcess(processMock.Object, _mockFileSystem, TestLoggerFactory.CreateLogger<InitialBuildProcess>());

        target.InitialBuild(false, "", "./ExampleProject.sln");

        processMock.Verify(x => x.Start(It.IsAny<string>(),
                It.Is<string>(applicationParam => applicationParam.Contains("dotnet", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(argumentsParam => argumentsParam.Contains("ExampleProject.sln", StringComparison.Ordinal)),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                It.IsAny<int>()),
            Times.Once);
    }
}
