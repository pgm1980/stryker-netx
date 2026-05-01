using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Spectre.Console;
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
/// Sprint 40 (v2.27.0) port of upstream stryker-net 4.14.1
/// src/Stryker.CLI/Stryker.CLI.UnitTest/FileConfigReaderTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `target.RunAsync(...)` upstream-pattern (fire-and-forget) → `await target.RunAsync(...)`.
/// xUnit cwd handling: explicit `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` (Sprint 39 lesson).
/// </summary>
[Collection("ConfigBuilderSequential")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
public sealed class FileConfigReaderTests : IDisposable
{
    private readonly string _originalDirectory;

    public FileConfigReaderTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        // xUnit doesn't set cwd to test output dir like MSTest does — set it explicitly so
        // ConfigBuilder finds resource fixtures (filled-stryker-config.json/yaml).
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    }

    public void Dispose() => Directory.SetCurrentDirectory(_originalDirectory);

    [Fact]
    public async Task WithNoArgumentsAndNoConfigFile_ShouldStartStrykerWithConfigOptions()
    {
        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds
            {
                High = 80,
                Low = 60,
                Break = 0,
            },
        };
        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory($"..{Path.DirectorySeparatorChar}");
        var runResults = new StrykerRunResult(options, 0.3);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>())).Returns(Task.FromResult(runResults)).Verifiable();
        var target = new StrykerCli(
            mock.Object,
            new ConfigBuilder(),
            Mock.Of<ILoggingInitializer>(),
            Mock.Of<IStrykerNugetFeedClient>(),
            Mock.Of<IAnsiConsole>(),
            Mock.Of<IFileSystem>());

        await target.RunAsync([]);

        mock.VerifyAll();

        Directory.SetCurrentDirectory(currentDirectory);
    }

    [Theory]
    [InlineData("--config-file")]
    [InlineData("-f")]
    public async Task WithJsonConfigFile_ShouldStartStrykerWithConfigFileOptions(string argName)
    {
        IStrykerInputs? actualInputs = null;
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds
            {
                High = 80,
                Low = 60,
                Break = 0,
            },
        };
        var runResults = new StrykerRunResult(options, 0.3);

        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Callback<IStrykerInputs>(c => actualInputs = c)
            .Returns(Task.FromResult(runResults))
            .Verifiable();

        var target = new StrykerCli(
            mock.Object,
            new ConfigBuilder(),
            Mock.Of<ILoggingInitializer>(),
            Mock.Of<IStrykerNugetFeedClient>(),
            Mock.Of<IAnsiConsole>(),
            Mock.Of<IFileSystem>());

        await target.RunAsync([argName, "filled-stryker-config.json"]);

        mock.VerifyAll();

        actualInputs.Should().NotBeNull();
        actualInputs!.AdditionalTimeoutInput.SuppliedInput.Should().Be(9999);
        actualInputs.VerbosityInput.SuppliedInput.Should().Be("trace");
        actualInputs.SourceProjectNameInput.SuppliedInput.Should().Be("ExampleProject.csproj");
        actualInputs.ReportersInput.SuppliedInput.Should().ContainSingle().Which.Should().Be(Reporter.Json.ToString());
        actualInputs.ConcurrencyInput.SuppliedInput.Should().Be(1);
        actualInputs.ThresholdBreakInput.SuppliedInput.Should().Be(20);
        actualInputs.ThresholdLowInput.SuppliedInput.Should().Be(30);
        actualInputs.ThresholdHighInput.SuppliedInput.Should().Be(40);
        actualInputs.MutateInput.SuppliedInput.Should().ContainSingle().Which.Should().Be("!**/Test.cs{1..100}{200..300}");
        actualInputs.CoverageAnalysisInput.SuppliedInput.Should().Be("perTest");
        actualInputs.DisableBailInput.SuppliedInput.Should().Be(true);
        actualInputs.IgnoreMutationsInput.SuppliedInput.Should().Contain("linq.FirstOrDefault");
        actualInputs.IgnoredMethodsInput.SuppliedInput.Should().Contain("Log*");
        actualInputs.TestCaseFilterInput.SuppliedInput.Should().Be("(FullyQualifiedName~UnitTest1&TestCategory=CategoryA)|Priority=1");
        actualInputs.DashboardUrlInput.SuppliedInput.Should().Be("https://alternative-stryker-dashboard.io");
        actualInputs.BreakOnInitialTestFailureInput.SuppliedInput.Should().NotBeNull().And.Be(false);
    }

    [Fact]
    public async Task WithYamlConfigFile_ShouldStartStrykerWithConfigFileOptions()
    {
        IStrykerInputs? actualInputs = null;
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds
            {
                High = 80,
                Low = 60,
                Break = 0,
            },
        };
        var runResults = new StrykerRunResult(options, 0.3);

        var mock = new Mock<IStrykerRunner>(MockBehavior.Strict);
        mock.Setup(x => x.RunMutationTestAsync(It.IsAny<IStrykerInputs>()))
            .Callback<IStrykerInputs>(c => actualInputs = c)
            .Returns(Task.FromResult(runResults))
            .Verifiable();

        var target = new StrykerCli(
            mock.Object,
            new ConfigBuilder(),
            Mock.Of<ILoggingInitializer>(),
            Mock.Of<IStrykerNugetFeedClient>(),
            Mock.Of<IAnsiConsole>(),
            Mock.Of<IFileSystem>());

        await target.RunAsync(["-f", "filled-stryker-config.yaml"]);

        mock.VerifyAll();

        actualInputs.Should().NotBeNull();
        actualInputs!.AdditionalTimeoutInput.SuppliedInput.Should().Be(9999);
        actualInputs.VerbosityInput.SuppliedInput.Should().Be("trace");
        actualInputs.SourceProjectNameInput.SuppliedInput.Should().Be("ExampleProject.csproj");
        actualInputs.ReportersInput.SuppliedInput.Should().ContainSingle().Which.Should().Be(Reporter.Json.ToString());
        actualInputs.ConcurrencyInput.SuppliedInput.Should().Be(1);
        actualInputs.ThresholdBreakInput.SuppliedInput.Should().Be(20);
        actualInputs.ThresholdLowInput.SuppliedInput.Should().Be(30);
        actualInputs.ThresholdHighInput.SuppliedInput.Should().Be(40);
        actualInputs.MutateInput.SuppliedInput.Should().ContainSingle().Which.Should().Be("!**/Test.cs{1..100}{200..300}");
        actualInputs.CoverageAnalysisInput.SuppliedInput.Should().Be("perTest");
        actualInputs.DisableBailInput.SuppliedInput.Should().Be(true);
        actualInputs.IgnoreMutationsInput.SuppliedInput.Should().Contain("linq.FirstOrDefault");
        actualInputs.IgnoredMethodsInput.SuppliedInput.Should().Contain("Log*");
        actualInputs.TestCaseFilterInput.SuppliedInput.Should().Be("(FullyQualifiedName~UnitTest1&TestCategory=CategoryA)|Priority=1");
        actualInputs.DashboardUrlInput.SuppliedInput.Should().Be("https://alternative-stryker-dashboard.io");
        actualInputs.BreakOnInitialTestFailureInput.SuppliedInput.Should().NotBeNull().And.Be(true);
    }
}
