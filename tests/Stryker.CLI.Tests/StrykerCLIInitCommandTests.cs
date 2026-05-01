using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Spectre.Console.Testing;
using Stryker.CLI;
using Stryker.CLI.Clients;
using Stryker.CLI.Logging;
using Stryker.Configuration.Options.Inputs;
using Stryker.Core;
using Xunit;

namespace Stryker.CLI.Tests;

/// <summary>
/// Sprint 38 (v2.25.0) port of upstream stryker-net 4.14.1
/// src/Stryker.CLI/Stryker.CLI.UnitTest/StrykerCLIInitCommandTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `target.RunAsync(...)` upstream-pattern (fire-and-forget) → `await target.RunAsync(...)` (xUnit-MA0134-clean).
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S6966:Awaitable method should be used",
    Justification = "Tests use sync File operations on MockFileSystem (in-memory); perf-not-test-concern.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "Test asserts behavior of IFileSystem interface; perf-not-test-concern (Sprint 28 lesson).")]
public class StrykerCLIInitCommandTests
{
    private readonly StrykerCli _target;
    private readonly Mock<IStrykerRunner> _strykerRunnerMock = new(MockBehavior.Strict);
    private readonly Mock<IStrykerNugetFeedClient> _nugetClientMock = new(MockBehavior.Strict);
    private readonly Mock<ILoggingInitializer> _loggingInitializerMock = new();
    private readonly IFileSystem _fileSystemMock = new MockFileSystem();
    private readonly TestConsole _consoleMock = new TestConsole().EmitAnsiSequences();

    public StrykerCLIInitCommandTests()
    {
        _target = new StrykerCli(
            _strykerRunnerMock.Object,
            new ConfigBuilder(),
            _loggingInitializerMock.Object,
            _nugetClientMock.Object,
            _consoleMock,
            _fileSystemMock);
    }

    [Fact]
    public async Task Init()
    {
        await _target.RunAsync(["init"]);

        _strykerRunnerMock.VerifyAll();

        _fileSystemMock.File.Exists("stryker-config.json").Should().BeTrue();
        var configFile = _fileSystemMock.File.ReadAllText("stryker-config.json");
        var config = JsonSerializer.Deserialize<FileBasedInputOuter>(configFile)!.Input!;

        config.AdditionalTimeout.Should().Be(new AdditionalTimeoutInput().Default);
        config.Verbosity.Should().Be(new VerbosityInput().Default);
        config.Project.Should().Be(new ProjectNameInput().Default);
        config.Reporters.Should().BeEquivalentTo(new ReportersInput().Default);
        config.Concurrency.Should().Be(new ConcurrencyInput().Default);
        config.Thresholds!.Break.Should().Be(new ThresholdBreakInput().Default);
        config.Thresholds.Low.Should().Be(new ThresholdLowInput().Default);
        config.Thresholds.High.Should().Be(new ThresholdHighInput().Default);
        config.Mutate.Should().BeEquivalentTo(new MutateInput().Default);
        config.MutationLevel.Should().Be(new MutationLevelInput().Default);
        config.CoverageAnalysis.Should().Be(new CoverageAnalysisInput().Default);
        config.DisableBail.Should().Be(new DisableBailInput().Default);
        config.IgnoreMutations.Should().BeEquivalentTo(new IgnoreMutationsInput().Default);
        config.IgnoreMethods.Should().BeEquivalentTo(new IgnoreMethodsInput().Default);
        config.TestCaseFilter.Should().Be(new TestCaseFilterInput().Default);
        config.TestProjects.Should().BeEquivalentTo(new TestProjectsInput().Default);
        config.DashboardUrl.Should().Be(new DashboardUrlInput().Default);
        config.BreakOnInitialTestFailure.Should().Be(new BreakOnInitialTestFailureInput().Default);
    }

    [Theory]
    [InlineData("init", "--config-file", "test.json")]
    [InlineData("init", "-f", "test.json")]
    public async Task InitCustomPath(params string[] args)
    {
        await _target.RunAsync(args);

        _strykerRunnerMock.VerifyAll();

        _fileSystemMock.File.Exists(args[^1]).Should().BeTrue();
    }

    [Fact]
    public async Task InitOverwrite()
    {
        // make sure the file exists before calling init
        _fileSystemMock.File.WriteAllText("stryker-config.json", "test");
        // deny overwrite
        _consoleMock.Input.PushKey(ConsoleKey.Enter);

        await _target.RunAsync(["init"]);

        _strykerRunnerMock.VerifyAll();

        _fileSystemMock.File.Exists("stryker-config.json").Should().BeTrue();
        var configFile = _fileSystemMock.File.ReadAllText("stryker-config.json");
        configFile.Should().Be("test");
    }

    [Fact]
    public async Task InitOverwriteConfirm()
    {
        // make sure the file exists before calling init
        _fileSystemMock.File.WriteAllText("stryker-config.json", "test");
        // confirm overwrite
        _consoleMock.Input.PushKey(ConsoleKey.Y);
        _consoleMock.Input.PushKey(ConsoleKey.Enter);

        await _target.RunAsync(["init"]);

        _strykerRunnerMock.VerifyAll();

        _fileSystemMock.File.Exists("stryker-config.json").Should().BeTrue();
        var configFile = _fileSystemMock.File.ReadAllText("stryker-config.json");
        var deserialized = JsonSerializer.Deserialize<FileBasedInputOuter>(configFile);
        deserialized!.Input.Should().NotBeNull();
    }

    [Fact]
    public async Task InitOverride()
    {
        await _target.RunAsync([
            "init",
            "--verbosity", "debug",
            "--project", "testProject",
            "--reporter", "dots",
            "--concurrency", "1",
            "--break-at", "10",
            "--threshold-low", "20",
            "--threshold-high", "30",
            "--mutate", "test*.cs",
            "--mutation-level", "advanced",
            "--disable-bail",
            "--test-project", "testProject",
            "--break-on-initial-test-failure",
        ]);

        _strykerRunnerMock.VerifyAll();

        _fileSystemMock.File.Exists("stryker-config.json").Should().BeTrue();
        var configFile = _fileSystemMock.File.ReadAllText("stryker-config.json");
        var config = JsonSerializer.Deserialize<FileBasedInputOuter>(configFile)!.Input!;

        config.Verbosity.Should().Be("debug");
        config.Project.Should().Be("testProject");
        config.Reporters.Should().ContainSingle().Which.Should().Be("dots");
        config.Concurrency.Should().Be(1);
        config.Thresholds!.Break.Should().Be(10);
        config.Thresholds.Low.Should().Be(20);
        config.Thresholds.High.Should().Be(30);
        config.Mutate.Should().ContainSingle().Which.Should().Be("test*.cs");
        config.MutationLevel.Should().Be("advanced");
        config.DisableBail.Should().Be(true);
        config.TestProjects.Should().ContainSingle().Which.Should().Be("testProject");
        config.BreakOnInitialTestFailure.Should().Be(true);
    }
}
