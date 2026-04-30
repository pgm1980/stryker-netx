using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.Utilities.Logging;
using static Stryker.Abstractions.Testing.ITestRunner;
using CoverageCollector = Stryker.DataCollector.CoverageCollector;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// Pool of <see cref="VsTestRunner"/> workers used to run mutation tests in parallel.
/// </summary>
public sealed partial class VsTestRunnerPool : ITestRunner
{
    private readonly AutoResetEvent _runnerAvailableHandler = new(false);
    private readonly ConcurrentBag<VsTestRunner> _availableRunners = [];
    private readonly ILogger _logger;
    private readonly int _countOfRunners;

    /// <summary>The shared <see cref="VsTestContextInformation"/> instance used by all runners in the pool.</summary>
    public VsTestContextInformation Context { get; }

    /// <summary>
    /// this constructor is for test purposes
    /// </summary>
    /// <param name="vsTestContext">VsTest context.</param>
    /// <param name="forcedLogger">Optional override logger.</param>
    /// <param name="runnerBuilder">Factory used to create individual runners (for test substitution).</param>
    public VsTestRunnerPool(VsTestContextInformation vsTestContext,
        ILogger? forcedLogger,
        Func<VsTestContextInformation, int, VsTestRunner> runnerBuilder)
    {
        _logger = forcedLogger ?? ApplicationLogging.LoggerFactory.CreateLogger<VsTestRunnerPool>();
        Context = vsTestContext;
        _countOfRunners = Math.Max(1, Context.Options.Concurrency);
        Initialize(runnerBuilder);
    }

    /// <summary>Initializes the runner pool from the supplied options.</summary>
    [ExcludeFromCodeCoverage(Justification = "It depends on the deployment of VsTest.")]
    public VsTestRunnerPool(IStrykerOptions options, IFileSystem? fileSystem = null)
    {
        Context = new VsTestContextInformation(options, fileSystem: fileSystem);
        _countOfRunners = Math.Max(1, options.Concurrency);
        _logger = ApplicationLogging.LoggerFactory.CreateLogger<VsTestRunnerPool>();
        Initialize();
    }

    /// <inheritdoc />
    public Task<bool> DiscoverTestsAsync(string assembly) => Task.FromResult(Context.AddTestSource(assembly));

    /// <inheritdoc />
    public ITestSet GetTests(IProjectAndTests project) => Context.GetTestsForSources(project.GetTestAssemblies());

    /// <inheritdoc />
    public Task<ITestRunResult> TestMultipleMutantsAsync(IProjectAndTests project, ITimeoutValueCalculator? timeoutCalc, IReadOnlyList<IMutant> mutants, TestUpdateHandler? update)
        => Task.FromResult(RunThis(runner => runner.TestMultipleMutants(project, timeoutCalc, mutants, update)));

    /// <inheritdoc />
    public Task<ITestRunResult> InitialTestAsync(IProjectAndTests project)
        => Task.FromResult(RunThis(runner => runner.InitialTest(project)));

    /// <inheritdoc />
    public IEnumerable<ICoverageRunResult> CaptureCoverage(IProjectAndTests project) => Context.Options.OptimizationMode.HasFlag(OptimizationModes.CaptureCoveragePerTest) ? CaptureCoverageTestByTest(project) : CaptureCoverageInOneGo(project);

    private void Initialize(Func<VsTestContextInformation, int, VsTestRunner>? runnerBuilder = null)
    {
        runnerBuilder ??= (context, i) => new VsTestRunner(context, i);
        // Fire-and-forget: runners are populated in parallel; consumers wait via _runnerAvailableHandler.
        // MA0134 demands the Task be observed, but the design intent is asynchronous warm-up without join.
        _ = Task.Run(() =>
            Parallel.For(0, _countOfRunners, (i, _) =>
            {
                _availableRunners.Add(runnerBuilder(Context, i));
                _runnerAvailableHandler.Set();
            }));
    }

    private Dictionary<Guid, ICoverageRunResult>.ValueCollection CaptureCoverageInOneGo(IProjectAndTests project) => ConvertCoverageResult(RunThis(runner => runner.RunCoverageSession(TestIdentifierList.EveryTest(), project).TestResults), false);

    private Dictionary<Guid, ICoverageRunResult>.ValueCollection CaptureCoverageTestByTest(IProjectAndTests project) => ConvertCoverageResult(CaptureCoveragePerIsolatedTests(project, Context.VsTests.Keys).TestResults, true);

    private SimpleRunResults CaptureCoveragePerIsolatedTests(IProjectAndTests project, IEnumerable<Guid> tests)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = _countOfRunners };
        var result = new SimpleRunResults();
        var results = new ConcurrentBag<IRunResults>();
        Parallel.ForEach(tests, options,
            testCase =>
                results.Add(RunThis(runner => runner.RunCoverageSession(new TestIdentifierList(testCase.ToString()), project))));

        return results.Aggregate(result, (runResults, singleResult) => runResults.Merge(singleResult));
    }

    private T RunThis<T>(Func<VsTestRunner, T> task)
    {
        VsTestRunner? runner;
        while (!_availableRunners.TryTake(out runner))
        {
            _runnerAvailableHandler.WaitOne();
        }

        try
        {
            return task(runner);
        }
        finally
        {
            _availableRunners.Add(runner);
            _runnerAvailableHandler.Set();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var runner in _availableRunners)
        {
            runner.Dispose();
        }
        _runnerAvailableHandler.Dispose();
    }

    private Dictionary<Guid, ICoverageRunResult>.ValueCollection ConvertCoverageResult(IEnumerable<TestResult> testResults, bool perIsolatedTest)
    {
        var seenTestCases = new HashSet<Guid>();
        var defaultConfidence = perIsolatedTest ? CoverageConfidence.Exact : CoverageConfidence.Normal;
        var resultCache = new Dictionary<Guid, ICoverageRunResult>();
        // initialize the map
        foreach (var testResult in testResults)
        {
            if (testResult.Outcome != TestOutcome.Passed && testResult.Outcome != TestOutcome.Failed)
            {
                // skip any test result that is not a pass or fail
                continue;
            }
            if (ConvertSingleResult(testResult, seenTestCases, defaultConfidence,
                    out var coverageRunResult) || coverageRunResult is null)
            {
                // we should skip this result
                continue;
            }

            // ensure we returns only entry per test
            var id = Guid.Parse(coverageRunResult.TestId);
            if (!resultCache.TryAdd(id, coverageRunResult))
            {
                resultCache[id].Merge(coverageRunResult);
            }
        }

        return resultCache.Values;
    }

    private bool ConvertSingleResult(TestResult testResult, HashSet<Guid> seenTestCases,
        CoverageConfidence defaultConfidence, out CoverageRunResult? coverageRunResult)
    {
        var (key, value) = testResult.GetProperties().FirstOrDefault(x => string.Equals(x.Key.Id, CoverageCollector.PropertyName, StringComparison.Ordinal));
        var testCaseId = testResult.TestCase.Id;
        var unexpected = false;
        var log = testResult.GetProperties().FirstOrDefault(x => string.Equals(x.Key.Id, CoverageCollector.CoverageLog, StringComparison.Ordinal)).Value?.ToString();
        if (!string.IsNullOrEmpty(log))
        {
            LogCoverageCollectorLog(_logger, log);
        }

        if (!Context.VsTests.TryGetValue(testCaseId, out var testDescription))
        {
            LogUnexpectedCoverageTestCase(_logger, testResult.TestCase.DisplayName);
            // add the test description to the referential
            testDescription = new VsTestDescription(new VsTestCase(testResult.TestCase));
            Context.VsTests.Add(testCaseId, testDescription);
            unexpected = true;
        }

        // is this a suspect test ?
        if (key == null)
        {
            if (seenTestCases.Contains(testCaseId))
            {
                // this is an extra result. Coverage data is already present in the already parsed result
                LogExtraResult(_logger, testResult.TestCase.DisplayName);
                coverageRunResult = null;
                return true;
            }

            // the coverage collector was not able to report anything ==> it has not been tracked by it, so we do not have coverage data
            // ==> we need it to use this test against every mutation
            LogNoCoverageData(_logger, testResult.TestCase.DisplayName);

            seenTestCases.Add(Guid.Parse(testDescription.Id));
            coverageRunResult = CoverageRunResult.Create(testDescription.Id.ToString(CultureInfo.InvariantCulture), CoverageConfidence.Dubious, [], [], []);
        }
        else
        {
            // we have coverage data
            seenTestCases.Add(Guid.Parse(testDescription.Id));
            var propertyPairValue = value as string;

            coverageRunResult = BuildCoverageRunResultFromCoverageInfo(propertyPairValue, testResult, testCaseId,
                unexpected ? CoverageConfidence.UnexpectedCase : defaultConfidence);
        }

        return false;
    }

    private CoverageRunResult BuildCoverageRunResultFromCoverageInfo(string? propertyPairValue, TestResult testResult,
        Guid testCaseId, CoverageConfidence level)
    {
        IEnumerable<int> coveredMutants;
        IEnumerable<int> staticMutants;
        IEnumerable<int> leakedMutants;

        if (string.IsNullOrWhiteSpace(propertyPairValue))
        {
            // do not attempt to parse empty strings
            LogTestNoCoverMutation(_logger, testResult.TestCase.DisplayName);
            coveredMutants = [];
            staticMutants = [];
        }
        else
        {
            var parts = propertyPairValue.Split(';');
            coveredMutants = string.IsNullOrEmpty(parts[0])
                ? []
                : parts[0].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture));
            // we identify mutants that are part of static code, unless we performed pertest capture
            staticMutants = parts.Length == 1 || string.IsNullOrEmpty(parts[1]) ||
                            Context.Options.OptimizationMode.HasFlag(OptimizationModes.CaptureCoveragePerTest)
                ? []
                : parts[1].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture));
        }

        // look for suspicious mutants
        var (testProperty, mutantOutsideTests) = testResult.GetProperties()
            .FirstOrDefault(x => string.Equals(x.Key.Id, CoverageCollector.OutOfTestsPropertyName, StringComparison.Ordinal));
        if (testProperty != null)
        {
            // we have some mutations that appeared outside any test, probably some run time test case generation, or some async logic.
            propertyPairValue = mutantOutsideTests as string;
            leakedMutants = string.IsNullOrEmpty(propertyPairValue)
                ? []
                : propertyPairValue.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture));
            LogMutationsOutsideTest(_logger, propertyPairValue ?? string.Empty);
        }
        else
        {
            leakedMutants = [];
        }

        return CoverageRunResult.Create(testCaseId.ToString(), level, coveredMutants, staticMutants, leakedMutants);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "VsTestRunner: Coverage collector log: {Log}.")]
    private static partial void LogCoverageCollectorLog(ILogger logger, string log);

    [LoggerMessage(Level = LogLevel.Warning, Message = "VsTestRunner: Coverage analysis run encountered a unexpected test case ({TestCase}), mutation tests may be inaccurate. Disable coverage analysis if you have doubts.")]
    private static partial void LogUnexpectedCoverageTestCase(ILogger logger, string testCase);

    [LoggerMessage(Level = LogLevel.Debug, Message = "VsTestRunner: Extra result for test {TestCase}, so no coverage data for it.")]
    private static partial void LogExtraResult(ILogger logger, string testCase);

    [LoggerMessage(Level = LogLevel.Debug, Message = "VsTestRunner: No coverage data for {TestCase}.")]
    private static partial void LogNoCoverageData(ILogger logger, string testCase);

    [LoggerMessage(Level = LogLevel.Debug, Message = "VsTestRunner: Test {TestCase} does not cover any mutation.")]
    private static partial void LogTestNoCoverMutation(ILogger logger, string testCase);

    [LoggerMessage(Level = LogLevel.Debug, Message = "VsTestRunner: Some mutations were executed outside any test (mutation ids: {MutationIds}).")]
    private static partial void LogMutationsOutsideTest(ILogger logger, string mutationIds);
}
