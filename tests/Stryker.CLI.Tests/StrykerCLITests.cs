using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NuGet.Versioning;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Testing;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.CLI;
using Stryker.CLI.Clients;
using Stryker.CLI.Logging;
using Stryker.Configuration;
using Stryker.Configuration.Options;
using Stryker.Core;
using Stryker.Core.Initialisation;
using Xunit;

namespace Stryker.CLI.Tests;

/// <summary>
/// Sprint 41 (v2.28.0) port of upstream stryker-net 4.14.1
/// src/Stryker.CLI/Stryker.CLI.UnitTest/StrykerCLITests.cs (largest CLI test file at 539 LOC, ~31 [TestMethod]s + ~12 [DataRow] cases).
/// **Closes the CLI dogfood track** (Sprints 37-41).
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
public class StrykerCLITests
{
    private IStrykerInputs _inputs = null!;
    private readonly StrykerCli _target;
    private readonly StrykerOptions _options;
    private readonly StrykerRunResult _runResults;
    private readonly Mock<IStrykerRunner> _strykerRunnerMock = new(MockBehavior.Strict);
    private readonly Mock<IStrykerNugetFeedClient> _nugetClientMock = new(MockBehavior.Strict);
    private readonly Mock<ILoggingInitializer> _loggingInitializerMock = new();

    public StrykerCLITests()
    {
        _options = new StrykerOptions { Thresholds = new Thresholds { Break = 0 } };
        _runResults = new StrykerRunResult(_options, 0.3);
        _strykerRunnerMock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Callback<IStrykerInputs>(c => _inputs = c)
            .Returns(Task.FromResult(_runResults))
            .Verifiable();
        _nugetClientMock.Setup(x => x.GetLatestVersionAsync()).Returns(Task.FromResult(new SemanticVersion(10, 0, 0)));
        var configBuilder = new ConfigBuilder();
        var consoleMock = new Mock<IAnsiConsole>();
        var fileSystemMock = new Mock<IFileSystem>();
        _target = new StrykerCli(_strykerRunnerMock.Object, configBuilder, _loggingInitializerMock.Object, _nugetClientMock.Object, consoleMock.Object, fileSystemMock.Object);
    }

    [Fact]
    public async Task ShouldDisplayInfoOnHelp()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), Mock.Of<IStrykerNugetFeedClient>(), console, Mock.Of<IFileSystem>());

        await target.RunAsync(["--help"]);

        const string Expected = "Usage: Stryker [command] [options]";
        console.Output.Should().Contain(Expected);
    }

    [Fact]
    public async Task ShouldDisplayLogo()
    {
        var strykerRunnerMock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var strykerRunResult = new StrykerRunResult(_options, 0.3);

        strykerRunnerMock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Returns(Task.FromResult(strykerRunResult))
            .Verifiable();

        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new StrykerCli(strykerRunnerMock.Object, new ConfigBuilder(), _loggingInitializerMock.Object, _nugetClientMock.Object, console, Mock.Of<IFileSystem>());

        await target.RunAsync([]);

        // wait 20ms to let the getVersion call be handled
        await Task.Delay(20);

        var consoleOutput = console.Output;

        consoleOutput.Should().Contain("Version:");
        // Note: upstream logo message references Stryker.NET; stryker-netx may or may not match exactly.
        // Use a substring that's stable across both — "10.0.0" (from mock SemanticVersion).
        consoleOutput.Should().Contain("10.0.0");

        _nugetClientMock.Verify(x => x.GetLatestVersionAsync(), Times.Once);
    }

    [Fact]
    public async Task ShouldCallNugetClient()
    {
        await _target.RunAsync([]);

        _nugetClientMock.Verify(x => x.GetLatestVersionAsync(), Times.Once);
        _nugetClientMock.VerifyNoOtherCalls();
    }

    // ----- Sprint 141 (Bug #4) — additive --tool-version flag tests -----

    [Fact]
    public async Task ToolVersionFlag_LongForm_PrintsToolVersionAndReturnsZero()
    {
        // --tool-version short-circuits before any McMaster parsing → no NuGet client call,
        // no logo, no mutation run. Exit code 0.
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var nugetMock = new Mock<IStrykerNugetFeedClient>(MockBehavior.Strict);
        var consoleMock = new Mock<IAnsiConsole>();
        var fileSystemMock = new Mock<IFileSystem>();
        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), nugetMock.Object, consoleMock.Object, fileSystemMock.Object);

        var exitCode = await target.RunAsync(["--tool-version"]);

        exitCode.Should().Be(ExitCodes.Success);
        // Strict-mode mocks would throw if any calls happened — proves short-circuit before further parsing.
        mock.VerifyNoOtherCalls();
        nugetMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ToolVersionFlag_ShortForm_ReturnsZero()
    {
        // -T is the shorthand for --tool-version.
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var nugetMock = new Mock<IStrykerNugetFeedClient>(MockBehavior.Strict);
        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), nugetMock.Object, Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());

        var exitCode = await target.RunAsync(["-T"]);

        exitCode.Should().Be(ExitCodes.Success);
        mock.VerifyNoOtherCalls();
        nugetMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetToolVersionString_StripsCommitShaSuffix()
    {
        // GetToolVersionString reads AssemblyInformationalVersion. With DotNet.ReproducibleBuilds
        // active, that has the form "X.Y.Z+commit-sha" — we strip the +sha so users see "X.Y.Z".
        var version = StrykerCli.GetToolVersionString();
        version.Should().NotBeNullOrEmpty();
        version.Should().NotContain("+", "the +commit-sha suffix must be stripped from the user-facing version output");
    }

    [Fact]
    public async Task OnAlreadyNewestVersion_ShouldCallNugetClientForPreview()
    {
        // Sprint 139 (Bug #2 fix) changed Directory.Build.props local-build defaults from
        // "1.0.0-preview.1" to "0.0.0-localdev". The "AlreadyNewest" branch is only taken
        // when latestVersion is NOT strictly greater than currentVersion. With a release-tag
        // mock like SemanticVersion(0,0,0), release > prerelease in SemVer 2.0 → "Update"
        // branch fires (incorrectly for this test's intent). Use the same prerelease tag
        // as Directory.Build.props so currentVersion == latestVersion and the ELSE branch
        // (preview-check) runs.
        _nugetClientMock.Setup(x => x.GetLatestVersionAsync()).Returns(Task.FromResult(new SemanticVersion(0, 0, 0, "localdev")));
        _nugetClientMock.Setup(x => x.GetPreviewVersionAsync()).Returns(Task.FromResult(new SemanticVersion(20, 0, 0)));

        await _target.RunAsync([]);

        _nugetClientMock.VerifyAll();
    }

    [Fact]
    public async Task OnMutationScoreBelowThresholdBreak_ShouldReturn_ExitCodeBreakThresholdViolated()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var options = new StrykerOptions { Thresholds = new Thresholds { Break = 40 } };
        var strykerRunResult = new StrykerRunResult(options, 0.3);

        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Returns(Task.FromResult(strykerRunResult))
            .Verifiable();

        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), Mock.Of<IStrykerNugetFeedClient>(), Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());
        var result = await target.RunAsync([]);

        mock.Verify();
        target.ExitCode.Should().Be(ExitCodes.BreakThresholdViolated);
        result.Should().Be(ExitCodes.BreakThresholdViolated);
    }

    [Fact]
    public async Task OnMutationScoreEqualToNullAndThresholdBreakEqualTo0_ShouldReturnExitCode0()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var options = new StrykerOptions { Thresholds = new Thresholds { Break = 0 } };
        var strykerRunResult = new StrykerRunResult(options, double.NaN);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Returns(Task.FromResult(strykerRunResult))
            .Verifiable();

        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), Mock.Of<IStrykerNugetFeedClient>(), Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());
        var result = await target.RunAsync([]);

        mock.Verify();
        target.ExitCode.Should().Be(0);
        result.Should().Be(0);
    }

    [Fact]
    public async Task OnMutationScoreEqualToNullAndThresholdBreakAbove0_ShouldReturnExitCode0()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var options = new StrykerOptions { Thresholds = new Thresholds { Break = 40 } };
        var strykerRunResult = new StrykerRunResult(options, double.NaN);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Returns(Task.FromResult(strykerRunResult))
            .Verifiable();

        var target = new StrykerCli(mock.Object, new ConfigBuilder(), _loggingInitializerMock.Object, _nugetClientMock.Object, Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());
        var result = await target.RunAsync([]);

        mock.Verify();
        target.ExitCode.Should().Be(0);
        result.Should().Be(0);
    }

    [Fact]
    public async Task OnMutationScoreAboveThresholdBreak_ShouldReturnExitCode0()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var options = new StrykerOptions { Thresholds = new Thresholds { Break = 0 } };
        var strykerRunResult = new StrykerRunResult(options, 0.1);

        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>())).Returns(Task.FromResult(strykerRunResult)).Verifiable();

        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), Mock.Of<IStrykerNugetFeedClient>(), Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());
        var result = await target.RunAsync([]);

        mock.Verify();
        target.ExitCode.Should().Be(0);
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    public async Task ShouldNotStartStryker_WithHelpArgument(string argName)
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var target = new StrykerCli(mock.Object, new ConfigBuilder(), Mock.Of<ILoggingInitializer>(), Mock.Of<IStrykerNugetFeedClient>(), Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());

        await target.RunAsync([argName]);

        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrow_OnException()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Throws(new InvalidOperationException("Initial testrun failed"))
            .Verifiable();

        var target = new StrykerCli(mock.Object, new ConfigBuilder(), _loggingInitializerMock.Object, _nugetClientMock.Object, Mock.Of<IAnsiConsole>(), Mock.Of<IFileSystem>());

        Func<Task> act = async () => await target.RunAsync([]);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("--reporter")]
    [InlineData("-r")]
    public async Task ShouldPassReporterArgumentsToStryker_WithReporterArgument(string argName)
    {
        await _target.RunAsync([argName, Reporter.Html.ToString(), argName, Reporter.Dots.ToString()]);

        _strykerRunnerMock.VerifyAll();

        _inputs.ReportersInput.SuppliedInput.Should().Contain(Reporter.Html.ToString());
        _inputs.ReportersInput.SuppliedInput.Should().Contain(Reporter.Dots.ToString());
    }

    [Theory]
    [InlineData("--project")]
    [InlineData("-p")]
    public async Task ShouldPassProjectArgumentsToStryker_WithProjectArgument(string argName)
    {
        await _target.RunAsync([argName, "SomeProjectName.csproj"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.SourceProjectNameInput.SuppliedInput.Should().Be("SomeProjectName.csproj");
    }

    [Theory]
    [InlineData("--solution")]
    [InlineData("-s")]
    public async Task ShouldPassSolutionArgumentPlusBasePathToStryker_WithSolutionArgument(string argName)
    {
        await _target.RunAsync([argName, "SomeSolutionPath.sln"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.SolutionInput.SuppliedInput.Should().Be("SomeSolutionPath.sln");
    }

    [Theory]
    [InlineData("--test-project")]
    [InlineData("-tp")]
    public async Task ShouldPassTestProjectArgumentsToStryker_WithTestProjectArgument(string argName)
    {
        await _target.RunAsync([argName, "SomeProjectName1.csproj", argName, "SomeProjectName2.csproj"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.TestProjectsInput.SuppliedInput.Should().Contain("SomeProjectName1.csproj");
        _inputs.TestProjectsInput.SuppliedInput.Should().Contain("SomeProjectName2.csproj");
    }

    [Theory]
    [InlineData("--verbosity")]
    [InlineData("-V")]
    public async Task ShouldPassLogConsoleArgumentsToStryker_WithLogConsoleArgument(string argName)
    {
        await _target.RunAsync([argName, "Debug"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.VerbosityInput.SuppliedInput.Should().Be(LogEventLevel.Debug.ToString());
    }

    [Theory]
    [InlineData("--log-to-file")]
    [InlineData("-L")]
    public async Task ShouldPassLogFileArgumentsToStryker_WithLogLevelFileArgument(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _inputs.LogToFileInput.SuppliedInput!.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("--diag")]
    public async Task WithDevModeArgument_ShouldPassDevModeArgumentsToStryker(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _inputs.DiagModeInput.SuppliedInput!.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("--concurrency")]
    [InlineData("-c")]
    public async Task WithMaxConcurrentTestrunnerArgument_ShouldPassValidatedConcurrentTestrunnersToStryker(string argName)
    {
        await _target.RunAsync([argName, "4"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.ConcurrencyInput.SuppliedInput!.Value.Should().Be(4);
    }

    [Theory]
    [InlineData("--break-at")]
    [InlineData("-b")]
    public async Task WithCustomThresholdBreakParameter_ShouldPassThresholdBreakToStryker(string argName)
    {
        await _target.RunAsync([argName, "20"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.ThresholdBreakInput.SuppliedInput.Should().Be(20);
    }

    [Theory]
    [InlineData("--mutate")]
    [InlineData("-m")]
    public async Task ShouldPassFilePatternSetToStryker_WithMutateArgs(string argName)
    {
        var firstFileToExclude = "**/*Service.cs";
        var secondFileToExclude = "!**/MySpecialService.cs";
        var thirdFileToExclude = "**/MyOtherService.cs{1..10}{32..45}";

        await _target.RunAsync([argName, firstFileToExclude, argName, secondFileToExclude, argName, thirdFileToExclude]);

        _strykerRunnerMock.VerifyAll();

        var filePatterns = _inputs.MutateInput.SuppliedInput!.ToArray();
        filePatterns.Length.Should().Be(3);
        filePatterns.Should().Contain(firstFileToExclude);
        filePatterns.Should().Contain(secondFileToExclude);
        filePatterns.Should().Contain(thirdFileToExclude);
    }

    [Theory]
    [InlineData("--since")]
    public async Task ShouldEnableDiffFeatureWhenPassed(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _inputs.SinceInput.SuppliedInput!.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("--since")]
    public async Task ShouldSetGitDiffTargetWhenPassed(string argName)
    {
        await _target.RunAsync([$"{argName}:development"]);

        _strykerRunnerMock.VerifyAll();

        _inputs.SinceInput.SuppliedInput!.Value.Should().BeTrue();
        _inputs.SinceTargetInput.SuppliedInput.Should().Be("development");
    }

    [Theory]
    [InlineData("--mutation-level")]
    [InlineData("-l")]
    public async Task ShouldSetMutationLevelWhenPassed(string argName)
    {
        await _target.RunAsync([argName, "Advanced"]);

        _inputs.MutationLevelInput.SuppliedInput.Should().Be(MutationLevel.Advanced.ToString());
    }

    [Theory]
    [InlineData("--version", "master")]
    [InlineData("-v", "master")]
    public async Task ShouldSetProjectVersionFeatureWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.ProjectVersionInput.SuppliedInput.Should().Be("master");
    }

    [Theory]
    [InlineData("--dashboard-api-key", "1234567890")]
    public async Task ShouldSupplyDashboardApiKeyWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.DashboardApiKeyInput.SuppliedInput.Should().Be("1234567890");
    }

    [Theory]
    [InlineData("--test-runner", "mtp")]
    public async Task ShouldSupplyTestRunnerWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.TestRunnerInput.SuppliedInput.Should().Be("mtp");
    }

    [Theory]
    [InlineData("--with-baseline")]
    public async Task ShouldSupplyWithBaselineWhenPassed(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _inputs.WithBaselineInput.SuppliedInput!.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("-o", null)]
    [InlineData("-o:html", "html")]
    [InlineData("--open-report", null)]
    [InlineData("--open-report:dashboard", "dashboard")]
    public async Task ShouldSupplyOpenReportInputsWhenPassed(string arg, string? expected)
    {
        await _target.RunAsync([arg]);

        _strykerRunnerMock.VerifyAll();

        _inputs.OpenReportEnabledInput.SuppliedInput.Should().BeTrue();
        _inputs.OpenReportInput.SuppliedInput.Should().Be(expected);
    }

    [Theory]
    [InlineData("--azure-fileshare-sas", "sas")]
    public async Task ShouldSupplyAzureFileshareSasWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.AzureFileStorageSasInput.SuppliedInput.Should().Be("sas");
    }

    [Theory]
    [InlineData("--s3-bucket-name", "my-bucket")]
    public async Task ShouldSupplyS3BucketNameWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.S3BucketNameInput.SuppliedInput.Should().Be("my-bucket");
    }

    [Theory]
    [InlineData("--s3-endpoint", "https://minio.example.com:9000")]
    public async Task ShouldSupplyS3EndpointWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.S3EndpointInput.SuppliedInput.Should().Be("https://minio.example.com:9000");
    }

    [Theory]
    [InlineData("--s3-region", "us-east-1")]
    public async Task ShouldSupplyS3RegionWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.S3RegionInput.SuppliedInput.Should().Be("us-east-1");
    }

    [Theory]
    [InlineData("--break-on-initial-test-failure")]
    public async Task ShouldSupplyBreakOnInitialTestFailureWhenPassed(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _inputs.BreakOnInitialTestFailureInput.SuppliedInput.HasValue.Should().BeTrue();
        _inputs.BreakOnInitialTestFailureInput.SuppliedInput!.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("--target-framework", "net7.0")]
    public async Task ShouldSupplyTargetFrameworkWhenPassed(string arg, string value)
    {
        await _target.RunAsync([arg, value]);

        _strykerRunnerMock.VerifyAll();

        _inputs.TargetFrameworkInput.SuppliedInput.Should().Be("net7.0");
    }

    [Theory]
    [InlineData("--skip-version-check")]
    public async Task ShouldSupplyDisableCheckForNewerVersion(string argName)
    {
        await _target.RunAsync([argName]);

        _strykerRunnerMock.VerifyAll();

        _nugetClientMock.VerifyNoOtherCalls();
    }
}
