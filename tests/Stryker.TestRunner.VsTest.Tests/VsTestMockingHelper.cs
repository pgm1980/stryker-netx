using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Testing;
using Stryker.Core.CoverageAnalysis;
using Stryker.Core.Initialisation;
using Stryker.Core.Mutants;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest.Helpers;
using Stryker.Utilities;
using CoverageCollector = Stryker.DataCollector.CoverageCollector;
using VsTestObjModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 26 (v2.13.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.VsTest.UnitTest/VsTestMockingHelper.cs.
/// Architecture-adapted from upstream's <c>Mock&lt;Buildalyzer.IAnalyzerResult&gt;</c>
/// to our <c>Mock&lt;Stryker.Abstractions.Analysis.IProjectAnalysis&gt;</c> via
/// the Sprint-25 <see cref="TestHelper.SetupProjectAnalyzerResult"/> adapter
/// (Sprint 1 Phase 9: Workspaces.MSBuild port replaced Buildalyzer with our
/// native abstraction). Property accesses rename <c>AnalyzerResult</c> →
/// <c>Analysis</c> at every consumer call site.
/// Framework conversion: MSTest → xUnit (helper has no [TestMethod] members).
/// </summary>
public class VsTestMockingHelper : TestBase
{
    protected Mutant Mutant { get; }
    protected Mutant OtherMutant { get; }
    private readonly string _testAssemblyPath;
    public SourceProjectInfo SourceProjectInfo { get; }
    private readonly ITestProjectsInfo _testProjectsInfo;
    private readonly MockFileSystem _fileSystem;
    private readonly Uri _nUnitUri;
    private readonly Uri _xUnitUri;
    private readonly VsTestObjModel.TestProperty _coverageProperty;
    private readonly VsTestObjModel.TestProperty _unexpectedCoverageProperty;
    protected static readonly TimeSpan TestDefaultDuration = TimeSpan.FromSeconds(1);
    private readonly string _filesystemRoot;

    public VsTestMockingHelper()
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _filesystemRoot = Path.GetPathRoot(currentDirectory)!;

        var sourceFile = File.ReadAllText(currentDirectory + "/TestResources/ExampleSourceFile.cs");
        var testProjectPath = FilePathUtils.NormalizePathSeparators(Path.Combine(_filesystemRoot, "TestProject", "TestProject.csproj"))!;
        var projectUnderTestPath = FilePathUtils.NormalizePathSeparators(Path.Combine(_filesystemRoot, "ExampleProject", "ExampleProject.csproj"))!;
        const string DefaultTestProjectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>netcoreapp2.0</TargetFramework>
                    <IsPackable>false</IsPackable>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version = "15.5.0" />
                    <PackageReference Include="xunit" Version="2.3.1" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.1" />
                    <DotNetCliToolReference Include="dotnet-xunit" Version="2.3.1" />
                </ItemGroup>
                <ItemGroup>
                    <ProjectReference Include="..\ExampleProject\ExampleProject.csproj" />
                </ItemGroup>
            </Project>
            """;
        _testAssemblyPath = FilePathUtils.NormalizePathSeparators(Path.Combine(_filesystemRoot, "_firstTest", "bin", "Debug", "TestApp.dll"))!;
        _nUnitUri = new Uri("exec://nunit");
        _xUnitUri = new Uri("executor://xunit/VsTestRunner2/netcoreapp");
        var firstTest = BuildCase("T0");
        var secondTest = BuildCase("T1");

        _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.Ordinal)
        {
            [projectUnderTestPath] = new MockFileData(DefaultTestProjectFileContents),
            [Path.Combine(_filesystemRoot, "ExampleProject", "Recursive.cs")!] = new MockFileData(sourceFile),
            [Path.Combine(_filesystemRoot, "ExampleProject", "OneFolderDeeper", "Recursive.cs")!] = new MockFileData(sourceFile),
            [testProjectPath] = new MockFileData(DefaultTestProjectFileContents),
            [_testAssemblyPath] = new MockFileData("Bytecode"),
            [Path.Combine(_filesystemRoot, "app", "bin", "Debug", "AppToTest.dll")!] = new MockFileData("Bytecode"),
        });
        _coverageProperty = VsTestObjModel.TestProperty.Register(CoverageCollector.PropertyName, CoverageCollector.PropertyName, typeof(string), typeof(VsTestObjModel.TestResult));
        _unexpectedCoverageProperty = VsTestObjModel.TestProperty.Register(CoverageCollector.OutOfTestsPropertyName, CoverageCollector.OutOfTestsPropertyName, typeof(string), typeof(VsTestObjModel.TestResult));
        Mutant = new Mutant { Id = 0 };
        OtherMutant = new Mutant { Id = 1 };
        _testProjectsInfo = BuildTestProjectsInfo();
        SourceProjectInfo = BuildSourceProjectInfo();

        TestCases = [firstTest, secondTest];
    }

    internal SourceProjectInfo BuildSourceProjectInfo(IEnumerable<Mutant>? mutants = null)
    {
        var content = new CsharpFolderComposite();
        content.Add(new CsharpFileLeaf { Mutants = mutants ?? [Mutant, OtherMutant] });
        return new SourceProjectInfo
        {
            Analysis = TestHelper.SetupProjectAnalyzerResult(
                properties: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["TargetDir"] = Path.Combine(_filesystemRoot, "app", "bin", "Debug"),
                    ["TargetFileName"] = "AppToTest.dll",
                    ["Language"] = "C#",
                },
                targetFramework: "netcoreapp2.1").Object,
            ProjectContents = content,
            TestProjectsInfo = _testProjectsInfo,
        };
    }

    internal ITestProjectsInfo BuildTestProjectsInfo() =>
        new TestProjectsInfo(_fileSystem)
        {
            TestProjects =
            [
                new TestProject(_fileSystem, TestHelper.SetupProjectAnalyzerResult(
                    properties: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["TargetDir"] = Path.GetDirectoryName(_testAssemblyPath)!,
                        ["TargetFileName"] = Path.GetFileName(_testAssemblyPath),
                    },
                    targetFramework: "netcoreapp2.1").Object),
            ],
        };

    protected IReadOnlyList<VsTestObjModel.TestCase> TestCases { get; }

    private static void DiscoverTests(ITestDiscoveryEventsHandler discoveryEventsHandler, IReadOnlyCollection<VsTestObjModel.TestCase> tests, bool aborted)
    {
        discoveryEventsHandler.HandleDiscoveredTests(tests);
        discoveryEventsHandler.HandleDiscoveryComplete(tests.Count, null, aborted);
    }

    protected VsTestObjModel.TestCase BuildCase(string name, TestFrameworks framework = TestFrameworks.xUnit, string? displayName = null) =>
        new(name, framework == TestFrameworks.xUnit ? _xUnitUri : _nUnitUri, _testAssemblyPath) { Id = Guid.NewGuid(), DisplayName = displayName ?? name };

    protected VsTestObjModel.TestCase FindOrBuildCase(string testResultId) =>
        TestCases.FirstOrDefault(t => string.Equals(t.FullyQualifiedName, testResultId, StringComparison.Ordinal)) ?? BuildCase(testResultId);

    private static void MockTestRun(ITestRunEventsHandler testRunEvents, IReadOnlyList<VsTestObjModel.TestResult> testResults, VsTestObjModel.TestCase? timeOutTest = null)
    {
        if (testResults.Count == 0)
        {
            return;
        }
        var timer = new Stopwatch();
        testRunEvents.HandleTestRunStatsChange(
            new TestRunChangedEventArgs(new TestRunStatistics(0, null), null, timeOutTest == null ? null : [timeOutTest]));

        for (var i = 0; i < testResults.Count; i++)
        {
            testResults[i].StartTime = DateTimeOffset.Now;
            Thread.Sleep(1);
            testResults[i].EndTime = DateTimeOffset.Now + testResults[i].Duration;
            testRunEvents.HandleTestRunStatsChange(new TestRunChangedEventArgs(
                new TestRunStatistics(i + 1, null), [testResults[i]], null));
        }

        if (timeOutTest != null)
        {
            testRunEvents.HandleTestRunStatsChange(new TestRunChangedEventArgs(
                new TestRunStatistics(testResults.Count, null), null, [timeOutTest]));
        }

        Thread.Sleep(1);
        testRunEvents.HandleTestRunComplete(
            new TestRunCompleteEventArgs(new TestRunStatistics(testResults.Count, null), false, false, null, null, timer.Elapsed),
            new TestRunChangedEventArgs(null, [], []),
            null,
            null);
    }

    protected void SetupMockTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, bool testResult, IReadOnlyList<VsTestObjModel.TestCase> testCases)
    {
        var results = new List<(string, bool)>(testCases.Count);
        results.AddRange(testCases.Select(t => (t.FullyQualifiedName, testResult)));
        SetupMockTestRun(mockVsTest, results);
    }

    protected void SetupMockTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, IEnumerable<(string id, bool success)> testResults, IReadOnlyList<VsTestObjModel.TestCase>? testCases = null)
    {
        var results = new List<VsTestObjModel.TestResult>();
        testCases ??= [.. TestCases];
        foreach (var (testResultId, success) in testResults)
        {
            var testCase = testCases.FirstOrDefault(t => string.Equals(t.FullyQualifiedName, testResultId, StringComparison.Ordinal)) ?? BuildCase(testResultId);
            results.Add(new VsTestObjModel.TestResult(testCase)
            {
                Outcome = success ? VsTestObjModel.TestOutcome.Passed : VsTestObjModel.TestOutcome.Failed,
                ComputerName = ".",
                Duration = TestDefaultDuration,
            });
        }
        SetupMockTestRun(mockVsTest, results);
    }

    protected void SetupMockTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyList<VsTestObjModel.TestResult> results) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<string>>(t => t.Any(source => string.Equals(source, _testAssemblyPath, StringComparison.Ordinal))),
                It.Is<string>(settings => !settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<string> _, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
                MockTestRun(testRunEvents, results));

    protected void SetupFailingTestRun(Mock<IVsTestConsoleWrapper> mockVsTest) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<string>>(t => t.Any(source => string.Equals(source, _testAssemblyPath, StringComparison.Ordinal))),
                It.Is<string>(settings => !settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<string> _, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
                _ = Task.Run(() =>
                {
                    var timer = new Stopwatch();
                    testRunEvents.HandleTestRunStatsChange(
                        new TestRunChangedEventArgs(new TestRunStatistics(0, null), null, null));
                    Thread.Sleep(10);
                    testRunEvents.HandleTestRunComplete(
                        new TestRunCompleteEventArgs(new TestRunStatistics(0, null), false, false,
                            new TransationLayerException("VsTest Crashed"), null, timer.Elapsed),
                        new TestRunChangedEventArgs(null, [], []),
                        null, null);
                }));

    protected void SetupFrozenTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, int repeated = 1) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<string>>(t => t.Any(source => string.Equals(source, _testAssemblyPath, StringComparison.Ordinal))),
                It.Is<string>(settings => !settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<string> _, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
                _ = Task.Run(() =>
                {
                    testRunEvents.HandleTestRunStatsChange(
                        new TestRunChangedEventArgs(new TestRunStatistics(0, null), null, null));
                    if (repeated-- <= 0)
                    {
                        testRunEvents.HandleTestRunComplete(
                            new TestRunCompleteEventArgs(new TestRunStatistics(0, null), false, false,
                                null, null, TimeSpan.FromMilliseconds(10)),
                            new TestRunChangedEventArgs(null, [], []),
                            null, null);
                    }
                    else
                    {
                        testRunEvents.HandleTestRunComplete(
                            new TestRunCompleteEventArgs(new TestRunStatistics(0, null), false, false,
                                new TransationLayerException("fake", null), null, TimeSpan.FromMilliseconds(10)),
                            new TestRunChangedEventArgs(null, [], []),
                            null, null);
                    }
                }));

    protected void SetupFrozenVsTest(Mock<IVsTestConsoleWrapper> mockVsTest, int repeated = 1) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<string>>(t => t.Any(source => string.Equals(source, _testAssemblyPath, StringComparison.Ordinal))),
                It.Is<string>(settings => !settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<string> _, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
            {
                testRunEvents.HandleTestRunStatsChange(
                    new TestRunChangedEventArgs(new TestRunStatistics(0, null), null, null));
                testRunEvents.HandleTestRunComplete(
                    new TestRunCompleteEventArgs(new TestRunStatistics(0, null), false, false,
                        null, null, TimeSpan.FromMilliseconds(10)),
                    new TestRunChangedEventArgs(null, [], []),
                    null, null);
                if (repeated-- > 0)
                {
                    Thread.Sleep(1000);
                }
            });

    protected void SetupMockCoverageRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyDictionary<string, string> coverageResults) =>
        SetupMockCoverageRun(mockVsTest, GenerateCoverageTestResults(coverageResults));

    protected void SetupMockCoverageRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyList<VsTestObjModel.TestResult> results) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<string>>(t => t.Any(source => string.Equals(source, _testAssemblyPath, StringComparison.Ordinal))),
                It.Is<string>(settings => settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<string> _, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
                MockTestRun(testRunEvents, results));

    private List<VsTestObjModel.TestResult> GenerateCoverageTestResults(IReadOnlyDictionary<string, string> coverageResults)
    {
        var results = new List<VsTestObjModel.TestResult>(coverageResults.Count);
        foreach (var (key, value) in coverageResults)
        {
            var result = new VsTestObjModel.TestResult(FindOrBuildCase(key))
            {
                DisplayName = key,
                Outcome = VsTestObjModel.TestOutcome.Passed,
                ComputerName = ".",
            };
            if (value != null)
            {
                var coveredList = value.Split('|');
                result.SetPropertyValue(_coverageProperty, coveredList[0]);
                if (coveredList.Length > 1)
                {
                    result.SetPropertyValue(_unexpectedCoverageProperty, coveredList[1]);
                }
            }
            results.Add(result);
        }
        return results;
    }

    protected void SetupMockCoveragePerTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyDictionary<string, string> coverageResults) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.Is<IEnumerable<VsTestObjModel.TestCase>>(t => t.Any()),
                It.Is<string>(settings => settings.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<VsTestObjModel.TestCase> testCases, string _, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
            {
                var results = new List<VsTestObjModel.TestResult>(coverageResults.Count);
                foreach (var (key, value) in coverageResults)
                {
                    var coveredList = value.Split('|');
                    if (!testCases.Any(t => string.Equals(t.DisplayName, key, StringComparison.Ordinal)))
                    {
                        continue;
                    }
                    results.Add(BuildCoverageTestResult(key, coveredList));
                }
                MockTestRun(testRunEvents, results);
            });

    protected VsTestObjModel.TestResult BuildCoverageTestResult(string key, string[] coveredList)
    {
        var result = new VsTestObjModel.TestResult(FindOrBuildCase(key))
        {
            DisplayName = key,
            Outcome = VsTestObjModel.TestOutcome.Passed,
            ComputerName = ".",
        };
        result.SetPropertyValue(_coverageProperty, coveredList[0]);
        if (coveredList.Length > 1)
        {
            result.SetPropertyValue(_unexpectedCoverageProperty, coveredList[1]);
        }
        return result;
    }

    protected static void SetupMockPartialTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyDictionary<string, string> results) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.IsAny<IEnumerable<VsTestObjModel.TestCase>>(),
                It.Is<string>(s => !s.Contains("<Coverage", StringComparison.Ordinal)),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<VsTestObjModel.TestCase> sources, string settings, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
            {
                var collector = new CoverageCollector();
                var start = new TestSessionStartArgs { Configuration = settings };
                var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
                collector.Initialize(mock.Object);
                collector.TestSessionStart(start);

                var mutants = collector.MutantList;
                if (!results.ContainsKey(mutants))
                {
                    throw new InvalidOperationException($"Unexpected mutant run {mutants}");
                }

                var tests = sources.ToList();
                var data = results[mutants].Split(',').Select(e => e.Split('=')).ToList();
                if (data.Count != tests.Count)
                {
                    throw new InvalidOperationException($"Invalid number of tests for mutant run {mutants}: found {tests.Count}, expected {data.Count}");
                }

                var runResults = new List<VsTestObjModel.TestResult>(data.Count);
                foreach (var strings in data)
                {
                    var matchingTest = tests.FirstOrDefault(t => string.Equals(t.FullyQualifiedName, strings[0], StringComparison.Ordinal))
                        ?? throw new InvalidOperationException($"Test {strings[0]} not run for mutant {mutants}.");
                    var result = new VsTestObjModel.TestResult(matchingTest)
                    {
                        Outcome = string.Equals(strings[1], "F", StringComparison.Ordinal) ? VsTestObjModel.TestOutcome.Failed : VsTestObjModel.TestOutcome.Passed,
                        ComputerName = ".",
                    };
                    runResults.Add(result);
                }
                MockTestRun(testRunEvents, runResults);
                collector.TestSessionEnd(new TestSessionEndArgs());
            });

    protected static void SetupMockTimeOutTestRun(Mock<IVsTestConsoleWrapper> mockVsTest, IReadOnlyDictionary<string, string> results, string timeoutTest) =>
        mockVsTest.Setup(x =>
            x.RunTestsWithCustomTestHost(
                It.IsAny<IEnumerable<VsTestObjModel.TestCase>>(),
                It.IsAny<string>(),
                It.Is<TestPlatformOptions>(o => o != null && o.TestCaseFilter == null),
                It.IsAny<ITestRunEventsHandler>(),
                It.IsAny<ITestHostLauncher>())).Callback(
            (IEnumerable<VsTestObjModel.TestCase> sources, string settings, TestPlatformOptions _, ITestRunEventsHandler testRunEvents, ITestHostLauncher _) =>
            {
                var collector = new CoverageCollector();
                var start = new TestSessionStartArgs { Configuration = settings };
                var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
                VsTestObjModel.TestCase? timeOutTestCase = null;
                collector.Initialize(mock.Object);
                collector.TestSessionStart(start);

                var mutants = collector.MutantList;
                if (!results.ContainsKey(mutants))
                {
                    throw new InvalidOperationException($"Unexpected mutant run {mutants}");
                }

                var tests = sources.ToList();
                var data = results[mutants].Split(',').Select(e => e.Split('=')).ToList();
                if (data.Count != tests.Count)
                {
                    throw new InvalidOperationException($"Invalid number of tests for mutant run {mutants}: found {tests.Count}, expected {data.Count}");
                }

                var runResults = new List<VsTestObjModel.TestResult>(data.Count);
                foreach (var strings in data)
                {
                    var matchingTest = tests.FirstOrDefault(t => string.Equals(t.FullyQualifiedName, strings[0], StringComparison.Ordinal))
                        ?? throw new InvalidOperationException($"Test {strings[0]} not run for mutant {mutants}.");
                    if (string.Equals(matchingTest.FullyQualifiedName, timeoutTest, StringComparison.Ordinal))
                    {
                        timeOutTestCase = matchingTest;
                    }
                    var result = new VsTestObjModel.TestResult(matchingTest)
                    {
                        Outcome = string.Equals(strings[1], "F", StringComparison.Ordinal) ? VsTestObjModel.TestOutcome.Failed : VsTestObjModel.TestOutcome.Passed,
                        ComputerName = ".",
                    };
                    runResults.Add(result);
                }
                MockTestRun(testRunEvents, runResults, timeOutTestCase);
                collector.TestSessionEnd(new TestSessionEndArgs());
            });

    protected Mock<IVsTestConsoleWrapper> BuildVsTestRunnerPool(
        IStrykerOptions options,
        out VsTestRunnerPool runner,
        IReadOnlyCollection<VsTestObjModel.TestCase>? testCases = null,
        ITestProjectsInfo? testProjectsInfo = null)
    {
        testCases ??= [.. TestCases];
        var mockedVsTestConsole = new Mock<IVsTestConsoleWrapper>(MockBehavior.Strict);
        mockedVsTestConsole.Setup(x => x.StartSession());
        mockedVsTestConsole.Setup(x => x.InitializeExtensions(It.IsAny<IEnumerable<string>>()));
        mockedVsTestConsole.Setup(x => x.AbortTestRun());
        mockedVsTestConsole.Setup(x => x.EndSession());

        mockedVsTestConsole.Setup(x =>
            x.DiscoverTests(
                It.Is<IEnumerable<string>>(d => d.Any(e => string.Equals(e, _testAssemblyPath, StringComparison.Ordinal))),
                It.IsAny<string>(),
                It.IsAny<ITestDiscoveryEventsHandler>()))
            .Callback((IEnumerable<string> _, string _, ITestDiscoveryEventsHandler handler) => DiscoverTests(handler, testCases, false));
        var context = new VsTestContextInformation(
            options,
            new Mock<IVsTestHelper>().Object,
            _fileSystem,
            _ => mockedVsTestConsole.Object,
            hostBuilder: _ => new MockStrykerTestHostLauncher(false),
            NullLogger.Instance);
        foreach (var path in (testProjectsInfo ?? _testProjectsInfo).GetTestAssemblies())
        {
            context.AddTestSource(path);
        }
        runner = new VsTestRunnerPool(context, NullLogger.Instance,
            (information, _) => new VsTestRunner(information, 0, NullLogger.Instance));
        return mockedVsTestConsole;
    }

    protected MutationTestProcess BuildMutationTestProcess(
        VsTestRunnerPool runner,
        IStrykerOptions options,
        IReadOnlyList<VsTestObjModel.TestCase>? tests = null,
        SourceProjectInfo? sourceProject = null)
    {
        var testRunResult = new TestRunResult(
            [],
            new TestIdentifierList((tests ?? TestCases).Select(t => t.Id.ToString())),
            TestIdentifierList.NoTest(),
            TestIdentifierList.NoTest(),
            string.Empty,
            [],
            TimeSpan.Zero);
        var input = new MutationTestInput
        {
            SourceProjectInfo = sourceProject ?? SourceProjectInfo,
            TestRunner = runner,
            InitialTestRun = new InitialTestRun(testRunResult, new TimeoutValueCalculator(500)),
            TestProjectsInfo = _testProjectsInfo,
        };
        var mutator = new CsharpMutationProcess(_fileSystem, TestLoggerFactory.CreateLogger<CsharpMutationProcess>());
        var executor = new MutationTestExecutor(TestLoggerFactory.CreateLogger<MutationTestExecutor>())
        {
            TestRunner = runner,
        };
        var coverageAnalyser = new CoverageAnalyser(TestLoggerFactory.CreateLogger<CoverageAnalyser>());
        var process = new MutationTestProcess(executor, coverageAnalyser, mutator, TestLoggerFactory.CreateLogger<MutationTestProcess>());
        process.Initialize(input, options, null!);
        return process;
    }

    private sealed class MockStrykerTestHostLauncher : IStrykerTestHostLauncher
    {
        public MockStrykerTestHostLauncher(bool isDebug) => IsDebug = isDebug;

        // Stryker's host-launcher protocol routes the actual host-launch through the
        // VsTest framework — these mock methods are never invoked because the upstream
        // tests use the in-process callback path. Throwing documents the intent.
        public int LaunchTestHost(VsTestObjModel.TestProcessStartInfo defaultTestHostStartInfo) =>
            throw new NotSupportedException("Mock host-launcher: real launch not exercised by upstream tests.");

        public int LaunchTestHost(VsTestObjModel.TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Mock host-launcher: real launch not exercised by upstream tests.");

        public bool IsDebug { get; }
    }
}
