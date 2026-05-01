using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.Abstractions;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 35 (v2.22.0) port of upstream stryker-net 4.14.1
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/SingleMicrosoftTestPlatformRunnerTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `new object()` discoveryLock → `new Lock()` (Sprint 2 .NET 10 modernisation).
/// `[TestInitialize]`/`[TestCleanup]` → ctor + IDisposable.
/// `[TestMethod, Timeout(1000)]` → `[Fact]` (xUnit has no per-test timeout; tests are fast enough).
/// `RunTestsInternalAsync` → `RunAssemblyTestsInternalAsync` (production rename).
/// TestableRunner.Dispose(bool): public override → protected override (production tightening).
/// TestableRunner ctor `object discoveryLock` → `Lock discoveryLock`.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
public sealed class SingleMicrosoftTestPlatformRunnerTests : IDisposable
{
    private readonly Dictionary<string, List<TestNode>> _testsByAssembly;
    private readonly Dictionary<string, MtpTestDescription> _testDescriptions;
    private readonly TestSet _testSet;
    private readonly Lock _discoveryLock;

    public SingleMicrosoftTestPlatformRunnerTests()
    {
        _testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        _testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        _testSet = new TestSet();
        _discoveryLock = new Lock();
    }

    public void Dispose()
    {
        // Clean up any temporary coverage files created during tests
        for (var id = 1; id <= 20; id++)
        {
            var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{id}.txt");
            try
            {
                if (File.Exists(coverageFilePath))
                {
                    File.Delete(coverageFilePath);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup errors
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup errors
            }
        }
    }

    private SingleMicrosoftTestPlatformRunner CreateRunner(int id = 0) =>
        new(id, _testsByAssembly, _testDescriptions, _testSet, _discoveryLock, NullLogger.Instance);

    [Fact]
    public async Task InitialTestAsync_CallsRunTestsInternalAsync_AndHandlesServerCreationFailure()
    {
        var project = new Mock<IProjectAndTests>();
        var invalidAssembly = "/path/to/nonexistent.dll";
        project.Setup(x => x.GetTestAssemblies()).Returns([invalidAssembly]);

        using var runner = CreateRunner(0);

        var result = await runner.InitialTestAsync(project.Object);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
        result.FailingTests.Should().NotBeNull();
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestMultipleMutantsAsync_CallsRunTestsInternalAsync_WithNonExistentAssembly()
    {
        var project = new Mock<IProjectAndTests>();
        var invalidAssembly = "/invalid/path/test.dll";
        project.Setup(x => x.GetTestAssemblies()).Returns([invalidAssembly]);

        var mutant = new Mock<IMutant>();
        mutant.Setup(x => x.Id).Returns(1);
        var mutants = new List<IMutant> { mutant.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunTestsInternalAsync_HandlesExceptionPath_WhenServerCreationFails()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/nonexistent.dll"]);

        using var runner = CreateRunner(0);

        var result = await runner.InitialTestAsync(project.Object);

        result.Should().NotBeNull();
        var testRunResult = result as TestRunResult;
        testRunResult.Should().NotBeNull();
        testRunResult!.FailingTests.Should().NotBeNull();
    }

    [Theory]
    [InlineData("assembly-a.dll")]
    [InlineData("assembly-b.dll")]
    [InlineData("Some/Path/To/Assembly.dll")]
    public void GetDiscoveredTests_ReturnsNull_WhenAssemblyNotRegistered(string assembly)
    {
        using var runner = CreateRunner();

        var result = runner.GetDiscoveredTests(assembly);

        result.Should().BeNull();
    }

    [Fact]
    public void GetDiscoveredTests_ReturnsTests_WhenAssemblyIsRegistered()
    {
        var tests = new List<TestNode>
        {
            new("uid-1", "Test1", "test", "passed"),
            new("uid-2", "Test2", "test", "failed"),
        };
        _testsByAssembly["my-assembly.dll"] = tests;

        using var runner = CreateRunner();

        var result = runner.GetDiscoveredTests("my-assembly.dll");

        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result[0].Uid.Should().Be("uid-1");
        result[1].Uid.Should().Be("uid-2");
    }

    [Fact]
    public void GetDiscoveredTests_ReturnsEmptyList_WhenAssemblyHasNoTests()
    {
        _testsByAssembly["empty.dll"] = [];

        using var runner = CreateRunner();

        var result = runner.GetDiscoveredTests("empty.dll");

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(100, 500, 500)]
    [InlineData(0, 500, 500)]
    [InlineData(250, 1000, 1000)]
    public void CalculateAssemblyTimeout_ReturnsExpectedTimeout(
        int initialRunTimeMs, int calculatorReturns, int expectedMs)
    {
        var testNode = new TestNode("uid-1", "Test1", "test", "passed");
        var discoveredTests = new List<TestNode> { testNode };

        var desc = new MtpTestDescription(testNode);
        desc.RegisterInitialTestResult(new MtpTestResult(TimeSpan.FromMilliseconds(initialRunTimeMs)));
        _testDescriptions["uid-1"] = desc;

        var timeoutCalc = new Mock<ITimeoutValueCalculator>();
        timeoutCalc.Setup(x => x.CalculateTimeoutValue(It.IsAny<int>()))
            .Returns(calculatorReturns);

        using var runner = CreateRunner();

        var result = runner.CalculateAssemblyTimeout(discoveredTests, timeoutCalc.Object, "test.dll");

        result.Should().NotBeNull();
        result!.Value.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Fact]
    public void CalculateAssemblyTimeout_SumsMultipleTestDurations()
    {
        var test1 = new TestNode("uid-1", "Test1", "test", "passed");
        var test2 = new TestNode("uid-2", "Test2", "test", "passed");
        var discoveredTests = new List<TestNode> { test1, test2 };

        var desc1 = new MtpTestDescription(test1);
        desc1.RegisterInitialTestResult(new MtpTestResult(TimeSpan.FromMilliseconds(100)));
        _testDescriptions["uid-1"] = desc1;

        var desc2 = new MtpTestDescription(test2);
        desc2.RegisterInitialTestResult(new MtpTestResult(TimeSpan.FromMilliseconds(200)));
        _testDescriptions["uid-2"] = desc2;

        var capturedEstimate = -1;
        var timeoutCalc = new Mock<ITimeoutValueCalculator>();
        timeoutCalc.Setup(x => x.CalculateTimeoutValue(It.IsAny<int>()))
            .Callback<int>(ms => capturedEstimate = ms)
            .Returns(999);

        using var runner = CreateRunner();

        runner.CalculateAssemblyTimeout(discoveredTests, timeoutCalc.Object, "test.dll");

        capturedEstimate.Should().Be(300);
    }

    [Fact]
    public void CalculateAssemblyTimeout_SkipsTestsWithoutDescription()
    {
        var test1 = new TestNode("uid-1", "Test1", "test", "passed");
        var testWithoutDesc = new TestNode("uid-unknown", "Unknown", "test", "passed");
        var discoveredTests = new List<TestNode> { test1, testWithoutDesc };

        var desc1 = new MtpTestDescription(test1);
        desc1.RegisterInitialTestResult(new MtpTestResult(TimeSpan.FromMilliseconds(150)));
        _testDescriptions["uid-1"] = desc1;

        var capturedEstimate = -1;
        var timeoutCalc = new Mock<ITimeoutValueCalculator>();
        timeoutCalc.Setup(x => x.CalculateTimeoutValue(It.IsAny<int>()))
            .Callback<int>(ms => capturedEstimate = ms)
            .Returns(777);

        using var runner = CreateRunner();

        runner.CalculateAssemblyTimeout(discoveredTests, timeoutCalc.Object, "test.dll");

        capturedEstimate.Should().Be(150);
    }

    [Fact]
    public async Task HandleAssemblyTimeoutAsync_AddsAllTestUidsToTimedOutList()
    {
        var tests = new List<TestNode>
        {
            new("uid-1", "Test1", "test", "passed"),
            new("uid-2", "Test2", "test", "passed"),
            new("uid-3", "Test3", "test", "passed"),
        };
        var timedOutTests = new List<string>();

        using var runner = CreateRunner();

        await runner.HandleAssemblyTimeoutAsync("some-assembly.dll", tests, timedOutTests);

        timedOutTests.Should().Equal("uid-1", "uid-2", "uid-3");
    }

    [Fact]
    public async Task HandleAssemblyTimeoutAsync_AppendsToExistingTimedOutList()
    {
        var tests = new List<TestNode> { new("uid-new", "NewTest", "test", "passed") };
        var timedOutTests = new List<string> { "uid-existing" };

        using var runner = CreateRunner();
        await runner.HandleAssemblyTimeoutAsync("assembly.dll", tests, timedOutTests);

        timedOutTests.Count.Should().Be(2);
        timedOutTests.Should().Contain("uid-existing");
        timedOutTests.Should().Contain("uid-new");
    }

    [Fact]
    public async Task HandleAssemblyTimeoutAsync_HandlesEmptyTestList()
    {
        var tests = new List<TestNode>();
        var timedOutTests = new List<string>();

        using var runner = CreateRunner();

        await runner.HandleAssemblyTimeoutAsync("assembly.dll", tests, timedOutTests);

        timedOutTests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunTestsInternalAsync_ReturnsFailedResult_WhenServerCreationFails()
    {
        using var runner = CreateRunner();

        var (result, timedOut) = await runner.RunAssemblyTestsInternalAsync("/nonexistent/assembly.dll", null, null);

        timedOut.Should().BeFalse();
        result.Should().NotBeNull();
        result.ResultMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("/path/a.dll")]
    [InlineData("/another/path/b.dll")]
    public async Task RunTestsInternalAsync_CatchesException_AndReturnsResult(string assembly)
    {
        using var runner = CreateRunner();

        var (result, timedOut) = await runner.RunAssemblyTestsInternalAsync(assembly, null, null);

        timedOut.Should().BeFalse();
        result.Should().BeOfType<TestRunResult>();
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunTestsInternalAsync_WithTimeout_StillReturnsResult_WhenServerFails()
    {
        using var runner = CreateRunner();
        var timeout = TimeSpan.FromMilliseconds(100);

        var (result, timedOut) = await runner.RunAssemblyTestsInternalAsync("/nonexistent.dll", null, timeout);

        timedOut.Should().BeFalse();
        result.Should().NotBeNull();
        result.FailingTests.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsInternalAsync_WithTimeout_DoesNotHangOnRealAssembly()
    {
        var fakeAssemblyPath = "/path/to/fake/test.dll";
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns([fakeAssemblyPath]);

        var mutant = new Mock<IMutant>();
        mutant.Setup(x => x.Id).Returns(1);
        var mutants = new List<IMutant> { mutant.Object };

        var timeoutCalc = new Mock<ITimeoutValueCalculator>();
        timeoutCalc.Setup(x => x.CalculateTimeoutValue(It.IsAny<int>())).Returns(1);

        var testNode = new TestNode("test1", "TestMethod1", "passed", "passed");
        _testsByAssembly[fakeAssemblyPath] = [testNode];
        _testDescriptions["test1"] = new MtpTestDescription(testNode);

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, timeoutCalc.Object, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsInternalAsync_RegistersTestResults_InTestDescriptions()
    {
        var testNode = new TestNode("test1", "TestMethod1", "passed", "passed");
        var mtpTestDesc = new MtpTestDescription(testNode);
        _testDescriptions["test1"] = mtpTestDesc;

        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/nonexistent/assembly.dll"]);

        using var runner = CreateRunner(0);

        var result = await runner.InitialTestAsync(project.Object);

        _testDescriptions.Should().ContainKey("test1");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsInternalAsync_CalculatesDuration()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/nonexistent.dll"]);

        using var runner = CreateRunner(0);

        var startTime = DateTime.UtcNow;
        var result = await runner.InitialTestAsync(project.Object);
        var endTime = DateTime.UtcNow;

        result.Should().NotBeNull();
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.Duration.Should().BeLessThan(endTime - startTime + TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RunTestsInternalAsync_WithMultipleMutants_UsesNegativeOneMutantId()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/test.dll"]);

        var mutant1 = new Mock<IMutant>();
        mutant1.Setup(x => x.Id).Returns(1);
        var mutant2 = new Mock<IMutant>();
        mutant2.Setup(x => x.Id).Returns(2);
        var mutants = new List<IMutant> { mutant1.Object, mutant2.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsInternalAsync_WithSingleMutant_UsesMutantId()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/test.dll"]);

        var mutant = new Mock<IMutant>();
        mutant.Setup(x => x.Id).Returns(42);
        var mutants = new List<IMutant> { mutant.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsInternalAsync_IncludesResultMessage_OnError()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/invalid/assembly.dll"]);

        using var runner = CreateRunner(0);

        var result = await runner.InitialTestAsync(project.Object);

        result.Should().NotBeNull();
        result.ResultMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldReturnFalse_WhenAssemblyNotFound()
    {
        using var runner = CreateRunner(0);

        var result = await runner.DiscoverTestsAsync("/nonexistent/assembly.dll");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InitialTestAsync_ShouldReturnTestRunResult()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns(["/nonexistent/assembly.dll"]);

        using var runner = CreateRunner(0);

        var result = await runner.InitialTestAsync(project.Object);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
        result.FailingTests.Should().NotBeNull();
    }

    [Fact]
    public async Task TestMultipleMutantsAsync_ShouldReturnTestRunResult_WithNoAssemblies()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns([]);

        var mutant = new Mock<IMutant>();
        mutant.Setup(x => x.Id).Returns(1);
        var mutants = new List<IMutant> { mutant.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
        result.FailingTests.Should().NotBeNull();
    }

    [Fact]
    public async Task TestMultipleMutantsAsync_ShouldUseCorrectMutantId_WhenSingleMutant()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns([]);

        var mutant = new Mock<IMutant>();
        mutant.Setup(x => x.Id).Returns(42);
        var mutants = new List<IMutant> { mutant.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        mutant.Verify(x => x.Id, Times.AtLeastOnce);
    }

    [Fact]
    public async Task TestMultipleMutantsAsync_ShouldUseNoMutationId_WhenMultipleMutants()
    {
        var project = new Mock<IProjectAndTests>();
        project.Setup(x => x.GetTestAssemblies()).Returns([]);

        var mutant1 = new Mock<IMutant>();
        mutant1.Setup(x => x.Id).Returns(1);
        var mutant2 = new Mock<IMutant>();
        mutant2.Setup(x => x.Id).Returns(2);
        var mutants = new List<IMutant> { mutant1.Object, mutant2.Object };

        using var runner = CreateRunner(0);

        var result = await runner.TestMultipleMutantsAsync(project.Object, null, mutants, null);

        result.Should().NotBeNull();
        result.ExecutedTests.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldCleanUpResources()
    {
        var testableRunner = new TestableRunner(
            123,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        testableRunner.MutantFilePath.Should().NotBeNull();
        var mutantFilePath = testableRunner.MutantFilePath;

        File.WriteAllText(mutantFilePath, "-1");
        File.Exists(mutantFilePath).Should().BeTrue("Mutant file should exist before disposal");

        testableRunner.Dispose();

        testableRunner.DisposedFlagWasSet.Should().BeTrue("_disposed flag should be set to true");
        testableRunner.DisposeLogicExecutedCount.Should().Be(1, "Dispose logic should execute once on first call");
        File.Exists(mutantFilePath).Should().BeFalse("Mutant file should be deleted after disposal");

        testableRunner.Dispose();

        testableRunner.DisposeLogicExecutedCount.Should().Be(1, "Dispose logic should only execute once due to _disposed flag check preventing re-execution");
    }

    [Fact]
    public void SetCoverageMode_ShouldEnableCoverageMode()
    {
        using var runner = new TestableRunnerForCoverage(
            1,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        runner.SetCoverageMode(true);

        runner.IsCoverageModeEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetCoverageMode_ShouldDisableCoverageMode()
    {
        using var runner = new TestableRunnerForCoverage(
            2,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        runner.SetCoverageMode(true);

        runner.SetCoverageMode(false);

        runner.IsCoverageModeEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetCoverageMode_ShouldBeIdempotent_WhenCalledWithTrue()
    {
        using var runner = new TestableRunnerForCoverage(
            3,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        runner.SetCoverageMode(true);

        runner.SetCoverageMode(true);

        runner.IsCoverageModeEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetCoverageMode_ShouldBeIdempotent_WhenCalledWithFalse()
    {
        using var runner = new TestableRunnerForCoverage(
            4,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        runner.IsCoverageModeEnabled.Should().BeFalse();

        runner.SetCoverageMode(false);

        runner.IsCoverageModeEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetCoverageMode_ShouldDeleteCoverageFile_WhenEnabling()
    {
        using var runner = new TestableRunnerForCoverage(
            5,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,2,3;4,5,6");
        File.Exists(runner.CoverageFilePath).Should().BeTrue();

        runner.SetCoverageMode(true);

        File.Exists(runner.CoverageFilePath).Should().BeFalse("Coverage file should be deleted when enabling coverage mode");
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmptyLists_WhenFileDoesNotExist()
    {
        using var runner = new TestableRunnerForCoverage(
            6,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        if (File.Exists(runner.CoverageFilePath))
        {
            File.Delete(runner.CoverageFilePath);
        }

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().BeEmpty();
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmptyLists_WhenFileIsEmpty()
    {
        using var runner = new TestableRunnerForCoverage(
            7,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, string.Empty);

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().BeEmpty();
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmptyLists_WhenFileContainsOnlyWhitespace()
    {
        using var runner = new TestableRunnerForCoverage(
            8,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "   \n\t  ");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().BeEmpty();
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldParseCoveredMutantsOnly()
    {
        using var runner = new TestableRunnerForCoverage(
            9,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,2,3");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 2, 3);
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldParseBothCoveredAndStaticMutants()
    {
        using var runner = new TestableRunnerForCoverage(
            10,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,2,3;4,5,6");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 2, 3);
        staticMutants.Should().Equal(4, 5, 6);
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleEmptyCoveredSection()
    {
        using var runner = new TestableRunnerForCoverage(
            11,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, ";4,5,6");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().BeEmpty();
        staticMutants.Should().Equal(4, 5, 6);
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleEmptyStaticSection()
    {
        using var runner = new TestableRunnerForCoverage(
            12,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,2,3;");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 2, 3);
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleDataWithSpaces()
    {
        using var runner = new TestableRunnerForCoverage(
            13,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, " 1 , 2 , 3 ; 4 , 5 , 6 ");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 2, 3);
        staticMutants.Should().Equal(4, 5, 6);
    }

    [Fact]
    public void ReadCoverageData_ShouldSkipInvalidNumbers()
    {
        using var runner = new TestableRunnerForCoverage(
            14,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,invalid,3;4,bad,6");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 3);
        staticMutants.Should().Equal(4, 6);
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleMixedValidAndInvalidData()
    {
        using var runner = new TestableRunnerForCoverage(
            15,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "1,,3,notanumber,5;,,7,xyz,9");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(1, 3, 5);
        staticMutants.Should().Equal(7, 9);
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmptyLists_OnFileReadException()
    {
        using var runner = new TestableRunnerForCoverage(
            16,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        using var fileStream = new FileStream(runner.CoverageFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var writer = new StreamWriter(fileStream);
        writer.Write("1,2,3;4,5,6");
        writer.Flush();

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().BeEmpty();
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleSingleMutantId()
    {
        using var runner = new TestableRunnerForCoverage(
            17,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "42;");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(42);
        staticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleLargeNumbers()
    {
        using var runner = new TestableRunnerForCoverage(
            18,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "2147483647;2147483646");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(2147483647);
        staticMutants.Should().Equal(2147483646);
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleNegativeNumbers()
    {
        using var runner = new TestableRunnerForCoverage(
            19,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        File.WriteAllText(runner.CoverageFilePath, "-1,2,-3;4,-5");

        var (coveredMutants, staticMutants) = runner.ReadCoverageData();

        coveredMutants.Should().Equal(-1, 2, -3);
        staticMutants.Should().Equal(4, -5);
    }

    private sealed class TestableRunner : SingleMicrosoftTestPlatformRunner
    {
        private readonly int _id;
        private int _disposeLogicExecutedCount;

        public TestableRunner(
            int id,
            Dictionary<string, List<TestNode>> testsByAssembly,
            Dictionary<string, MtpTestDescription> testDescriptions,
            TestSet testSet,
            Lock discoveryLock,
            ILogger logger)
            : base(id, testsByAssembly, testDescriptions, testSet, discoveryLock, logger)
        {
            _id = id;
        }

        public bool DisposedFlagWasSet { get; private set; }

        public int DisposeLogicExecutedCount => _disposeLogicExecutedCount;

        public string MutantFilePath => Path.Combine(Path.GetTempPath(), $"stryker-mutant-{_id}.txt");

        protected override void Dispose(bool disposing)
        {
            var disposedField = typeof(SingleMicrosoftTestPlatformRunner).GetField(
                "_disposed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var wasDisposedBefore = (bool)disposedField!.GetValue(this)!;

            base.Dispose(disposing);

            var wasDisposedAfter = (bool)disposedField!.GetValue(this)!;

            if (!wasDisposedBefore && wasDisposedAfter)
            {
                _disposeLogicExecutedCount++;
                DisposedFlagWasSet = true;
            }
        }
    }

    private sealed class TestableRunnerForCoverage : SingleMicrosoftTestPlatformRunner
    {
        private readonly int _id;

        public TestableRunnerForCoverage(
            int id,
            Dictionary<string, List<TestNode>> testsByAssembly,
            Dictionary<string, MtpTestDescription> testDescriptions,
            TestSet testSet,
            Lock discoveryLock,
            ILogger logger)
            : base(id, testsByAssembly, testDescriptions, testSet, discoveryLock, logger)
        {
            _id = id;
        }

        public string CoverageFilePath => Path.Combine(Path.GetTempPath(), $"stryker-coverage-{_id}.txt");

        public bool IsCoverageModeEnabled
        {
            get
            {
                var coverageField = typeof(SingleMicrosoftTestPlatformRunner).GetField(
                    "_coverageMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (bool)coverageField!.GetValue(this)!;
            }
        }
    }
}
