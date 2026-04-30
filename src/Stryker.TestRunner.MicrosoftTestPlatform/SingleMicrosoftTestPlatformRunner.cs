using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using static Stryker.Abstractions.Testing.ITestRunner;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Individual test runner instance that handles test execution with mutation-specific
/// environment variables. Used by MicrosoftTestPlatformRunnerPool.
/// Maintains persistent test server connections per assembly to reduce process startup overhead.
/// Uses file-based mutant control to allow changing the active mutant without restarting processes.
/// </summary>
public partial class SingleMicrosoftTestPlatformRunner : IDisposable
{
    private readonly int _id;
    private readonly IDictionary<string, List<TestNode>> _testsByAssembly;
    private readonly IDictionary<string, MtpTestDescription> _testDescriptions;
    private readonly TestSet _testSet;
    private readonly Lock _discoveryLock;
    private readonly ILogger _logger;
    private readonly string _mutantFilePath;
    private readonly string _coverageFilePath;
    private readonly IStrykerOptions? _options;

    private readonly Dictionary<string, AssemblyTestServer> _assemblyServers = new(StringComparer.Ordinal);
    private readonly Lock _serverLock = new();
    private bool _disposed;
    private bool _coverageMode;

    private string RunnerId => $"MtpRunner-{_id}";

    /// <summary>
    /// Initializes a new instance of <see cref="SingleMicrosoftTestPlatformRunner"/>.
    /// </summary>
    public SingleMicrosoftTestPlatformRunner(
        int id,
        IDictionary<string, List<TestNode>> testsByAssembly,
        IDictionary<string, MtpTestDescription> testDescriptions,
        TestSet testSet,
        Lock discoveryLock,
        ILogger logger,
        IStrykerOptions? options = null)
    {
        _id = id;
        _testsByAssembly = testsByAssembly;
        _testDescriptions = testDescriptions;
        _testSet = testSet;
        _discoveryLock = discoveryLock;
        _logger = logger;
        _options = options;

        // Create unique file paths for this runner to communicate with the test process
        _mutantFilePath = Path.Combine(Path.GetTempPath(), $"stryker-mutant-{_id}.txt");
        _coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{_id}.txt");

        // Initialize with no active mutation
        WriteMutantIdToFile(-1);
    }

    /// <summary>Discovers tests in the specified assembly.</summary>
    public Task<bool> DiscoverTestsAsync(string assembly) => DiscoverTestsInternalAsync(assembly);

    /// <summary>Performs the initial test run for the specified project.</summary>
    public Task<ITestRunResult> InitialTestAsync(IProjectAndTests project)
    {
        var assemblies = project.GetTestAssemblies();
        return RunAllTestsAsync(assemblies, mutantId: -1, mutants: null, update: null);
    }

    /// <summary>Tests the specified mutants, optionally updating progress via the supplied callback.</summary>
    public Task<ITestRunResult> TestMultipleMutantsAsync(
        IProjectAndTests project,
        ITimeoutValueCalculator? timeoutCalc,
        IReadOnlyList<IMutant> mutants,
        TestUpdateHandler? update)
    {
        var assemblies = project.GetTestAssemblies();

        // Determine which mutant to activate
        // When testing a single mutant, activate it; otherwise use -1 (no mutation)
        var mutantId = mutants.Count == 1 ? mutants[0].Id : -1;

        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            var mutantList = string.Join(",", mutants.Select(m => m.Id));
            LogTestingMutants(_logger, RunnerId, mutantList, mutantId);
        }

        return RunAllTestsAsync(assemblies, mutantId, mutants, update, timeoutCalc);
    }

    /// <summary>Resets the test servers, allowing assemblies to be reloaded for a fresh test run.</summary>
    public Task ResetServerAsync()
    {
        LogResettingServers(_logger, RunnerId);

        lock (_serverLock)
        {
            foreach (var server in _assemblyServers.Values)
            {
                server.Dispose();
            }
            _assemblyServers.Clear();
        }

        LogServersResetComplete(_logger, RunnerId);
        return Task.CompletedTask;
    }

    private void WriteMutantIdToFile(int mutantId)
    {
        try
        {
            File.WriteAllText(_mutantFilePath, mutantId.ToString(CultureInfo.InvariantCulture));
            LogWroteMutantId(_logger, RunnerId, mutantId, _mutantFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToWriteMutantId(_logger, ex, RunnerId, _mutantFilePath);
        }
    }

    private Dictionary<string, string?> BuildEnvironmentVariables()
    {
        var envVars = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["STRYKER_MUTANT_FILE"] = _mutantFilePath
        };

        // Add coverage filename when in coverage mode (MutantControl will combine with temp path)
        if (_coverageMode)
        {
            envVars["STRYKER_COVERAGE_FILE"] = Path.GetFileName(_coverageFilePath);
        }

        return envVars;
    }

    /// <summary>
    /// Enables or disables coverage capture mode. When enabled, the test process will track
    /// which mutations are covered and write the data to a file on process exit.
    /// </summary>
    public void SetCoverageMode(bool enabled)
    {
        lock (_serverLock)
        {
            if (_coverageMode == enabled)
            {
                // Already in the desired state; no action needed
                return;
            }

            _coverageMode = enabled;
            LogCoverageMode(_logger, RunnerId, enabled ? "enabled" : "disabled");

            // Reset servers to apply the new environment variables
            foreach (var server in _assemblyServers.Values)
            {
                server.Dispose();
            }
            _assemblyServers.Clear();
        }

        // Clean up any existing coverage file, even when enabling, to ensure we start fresh
        DeleteCoverageFile();
    }

    /// <summary>
    /// Reads coverage data from the coverage file written by the test process.
    /// Returns the covered mutants and static mutants as separate lists.
    /// </summary>
    public (IReadOnlyList<int> CoveredMutants, IReadOnlyList<int> StaticMutants) ReadCoverageData()
    {
        if (!File.Exists(_coverageFilePath))
        {
            LogCoverageFileNotFound(_logger, RunnerId, _coverageFilePath);
            return (Array.Empty<int>(), Array.Empty<int>());
        }

        try
        {
            var content = File.ReadAllText(_coverageFilePath).Trim();
            LogReadCoverageData(_logger, RunnerId, content);

            if (string.IsNullOrEmpty(content))
            {
                return (Array.Empty<int>(), Array.Empty<int>());
            }

            var parts = content.Split(';');
            var coveredMutants = ParseMutantIds(parts.Length > 0 ? parts[0] : string.Empty);
            var staticMutants = ParseMutantIds(parts.Length > 1 ? parts[1] : string.Empty);

            return (coveredMutants, staticMutants);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToReadCoverage(_logger, ex, RunnerId, _coverageFilePath);
            return (Array.Empty<int>(), Array.Empty<int>());
        }
    }

    private static IReadOnlyList<int> ParseMutantIds(string idString)
    {
        if (string.IsNullOrWhiteSpace(idString))
        {
            return [];
        }

        return [.. idString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)];
    }

    private void DeleteCoverageFile()
    {
        try
        {
            if (File.Exists(_coverageFilePath))
            {
                File.Delete(_coverageFilePath);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToDeleteCoverage(_logger, ex, RunnerId, _coverageFilePath);
        }
    }

    private async Task<AssemblyTestServer> GetOrCreateServerAsync(string assembly)
    {
        AssemblyTestServer? server;
        lock (_serverLock)
        {
            if (_assemblyServers.TryGetValue(assembly, out server) && server.IsInitialized)
            {
                return server;
            }
        }

        var environmentVariables = BuildEnvironmentVariables();
        server = new AssemblyTestServer(assembly, environmentVariables, _logger, RunnerId, _options);

        var started = await server.StartAsync().ConfigureAwait(false);
        if (!started)
        {
            throw new InvalidOperationException($"Failed to start test server for {assembly}");
        }

        lock (_serverLock)
        {
            _assemblyServers[assembly] = server;
        }

        return server;
    }

    private async Task<bool> DiscoverTestsInternalAsync(string assembly)
    {
        try
        {
            var server = await GetOrCreateServerAsync(assembly).ConfigureAwait(false);
            var tests = await server.DiscoverTestsAsync().ConfigureAwait(false);

            lock (_discoveryLock)
            {
                _testsByAssembly[assembly] = tests;

                foreach (var test in tests.Where(t => !_testDescriptions.ContainsKey(t.Uid)))
                {
                    var mtpTestDescription = new MtpTestDescription(test);
                    _testDescriptions[test.Uid] = mtpTestDescription;
                    _testSet.RegisterTest(mtpTestDescription.Description);
                }
            }

            LogDiscoveredTests(_logger, RunnerId, tests.Count, assembly);
            return tests.Count > 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToDiscover(_logger, ex, RunnerId, assembly);
            return false;
        }
    }

    internal List<TestNode>? GetDiscoveredTests(string assembly)
    {
        lock (_discoveryLock)
        {
            return _testsByAssembly.TryGetValue(assembly, out var tests) ? tests : null;
        }
    }

    internal TimeSpan? CalculateAssemblyTimeout(List<TestNode> discoveredTests, ITimeoutValueCalculator timeoutCalc, string assembly)
    {
        var estimatedTimeMs = (int)discoveredTests
            .Where(t => _testDescriptions.TryGetValue(t.Uid, out _))
            .Sum(t =>
            {
                lock (_discoveryLock)
                {
                    return _testDescriptions.TryGetValue(t.Uid, out var desc)
                        ? desc.InitialRunTime.TotalMilliseconds
                        : 0;
                }
            });

        var timeoutMs = timeoutCalc.CalculateTimeoutValue(estimatedTimeMs);
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            var assemblyName = Path.GetFileName(assembly);
            LogUsingTimeout(_logger, RunnerId, timeoutMs, assemblyName);
        }

        return TimeSpan.FromMilliseconds(timeoutMs);
    }

    internal async Task HandleAssemblyTimeoutAsync(string assembly, List<TestNode> discoveredTests, List<string> allTimedOutTests)
    {
        var assemblyFileName = Path.GetFileName(assembly);
        LogTestRunTimedOut(_logger, RunnerId, assemblyFileName);

        allTimedOutTests.AddRange(discoveredTests.Select(t => t.Uid));

        AssemblyTestServer? server;
        lock (_serverLock)
        {
            _assemblyServers.TryGetValue(assembly, out server);
        }

        if (server is not null)
        {
            LogRestartingServer(_logger, RunnerId, assemblyFileName);
            try
            {
                await server.RestartAsync(force: true).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailedRestartServer(_logger, ex, RunnerId, assemblyFileName);
                lock (_serverLock)
                {
                    _assemblyServers.Remove(assembly);
                }
            }
        }
    }

    private sealed class TestRunAccumulator
    {
        private readonly List<string> _executedTests = [];
        private readonly List<string> _failedTests = [];
        private readonly List<string> _messages = [];
        private readonly List<string> _errorMessages = [];
        private int _totalDiscoveredTests;
        private int _totalExecutedTests;

        public List<string> TimedOutTests { get; } = [];
        public bool HasTimeout { get; set; }
        public TimeSpan TotalDuration { get; private set; }

        public void Aggregate(TestRunResult result, List<TestNode>? discoveredTests)
        {
            if (result.ExecutedTests.IsEveryTest)
            {
                _totalExecutedTests += discoveredTests?.Count ?? 0;
            }
            else
            {
                var executedIds = result.ExecutedTests.GetIdentifiers().ToList();
                _executedTests.AddRange(executedIds);
                _totalExecutedTests += executedIds.Count;
            }

            _failedTests.AddRange(result.FailingTests.GetIdentifiers());
            TotalDuration += result.Duration;
            _messages.AddRange(result.Messages ?? []);

            if (!string.IsNullOrWhiteSpace(result.ResultMessage))
            {
                _errorMessages.Add(result.ResultMessage);
            }
        }

        public void AddDiscoveredCount(int count) => _totalDiscoveredTests += count;

        public ITestIdentifiers BuildExecutedTests() =>
            _totalDiscoveredTests > 0 && _totalExecutedTests >= _totalDiscoveredTests
                ? TestIdentifierList.EveryTest()
                : new TestIdentifierList(_executedTests);

        public TestIdentifierList BuildFailedTests() => new(_failedTests);

        public TestIdentifierList BuildTimedOutTests() => new(TimedOutTests);

        public string BuildErrorMessage() => string.Join(Environment.NewLine, _errorMessages);

        public IEnumerable<string> Messages => _messages;
    }

    internal async Task<ITestRunResult> RunAllTestsAsync(
        IReadOnlyList<string> assemblies,
        int mutantId,
        IReadOnlyList<IMutant>? mutants,
        TestUpdateHandler? update,
        ITimeoutValueCalculator? timeoutCalc = null)
    {
        try
        {
            WriteMutantIdToFile(mutantId);
            var accumulator = new TestRunAccumulator();
            await RunAssembliesAsync(assemblies, accumulator, timeoutCalc).ConfigureAwait(false);
            return BuildAggregatedResult(accumulator, mutants, update);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToRunTests(_logger, ex, RunnerId, mutantId);
            return new TestRunResult(false, ex.Message);
        }
    }

    private async Task RunAssembliesAsync(IReadOnlyList<string> assemblies, TestRunAccumulator accumulator, ITimeoutValueCalculator? timeoutCalc)
    {
        foreach (var assembly in assemblies)
        {
            var (result, timedOut, discoveredTests) = await RunAssemblyTestsAsync(assembly, timeoutCalc).ConfigureAwait(false);

            if (discoveredTests is not null)
            {
                accumulator.AddDiscoveredCount(discoveredTests.Count);

                if (timedOut)
                {
                    accumulator.HasTimeout = true;
                    await HandleAssemblyTimeoutAsync(assembly, discoveredTests, accumulator.TimedOutTests).ConfigureAwait(false);
                }
            }

            if (result is not null)
            {
                accumulator.Aggregate(result, discoveredTests);
            }
        }
    }

    private TestRunResult BuildAggregatedResult(TestRunAccumulator accumulator, IReadOnlyList<IMutant>? mutants, TestUpdateHandler? update)
    {
        var executedTests = accumulator.BuildExecutedTests();
        var failedTestIds = accumulator.BuildFailedTests();
        var timedOutTestIds = accumulator.BuildTimedOutTests();

        IEnumerable<MtpTestDescription> testDescriptionValues;
        lock (_discoveryLock)
        {
            testDescriptionValues = [.. _testDescriptions.Values];
        }

        if (update is not null && mutants is not null)
        {
            update.Invoke(mutants, failedTestIds, executedTests, timedOutTestIds);
        }

        if (accumulator.HasTimeout)
        {
            return TestRunResult.TimedOut(
                testDescriptionValues,
                executedTests,
                failedTestIds,
                timedOutTestIds,
                accumulator.BuildErrorMessage(),
                accumulator.Messages,
                accumulator.TotalDuration);
        }

        return new TestRunResult(
            testDescriptionValues,
            executedTests,
            failedTestIds,
            timedOutTestIds,
            accumulator.BuildErrorMessage(),
            accumulator.Messages,
            accumulator.TotalDuration);
    }

    internal virtual async Task<(TestRunResult? Result, bool TimedOut, List<TestNode>? DiscoveredTests)> RunAssemblyTestsAsync(
        string assembly,
        ITimeoutValueCalculator? timeoutCalc)
    {
        if (!File.Exists(assembly))
        {
            return (null, false, null);
        }

        var discoveredTests = GetDiscoveredTests(assembly);

        TimeSpan? timeout = null;
        if (timeoutCalc is not null && discoveredTests is not null)
        {
            timeout = CalculateAssemblyTimeout(discoveredTests, timeoutCalc, assembly);
        }

        var (testResults, timedOut) = await RunAssemblyTestsInternalAsync(assembly, null, timeout).ConfigureAwait(false);

        return (testResults as TestRunResult, timedOut, discoveredTests);
    }

    internal async Task<(ITestRunResult Result, bool TimedOut)> RunAssemblyTestsInternalAsync(
        string assembly,
        Func<TestNode, bool>? testUidFilter,
        TimeSpan? timeout = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Get or create the server for this assembly (reuses existing server)
            var server = await GetOrCreateServerAsync(assembly).ConfigureAwait(false);
            var tests = ResolveTestsForAssembly(assembly);
            var testsToRun = tests?.Where(t => testUidFilter is null || testUidFilter(t)).ToArray();

            var (testResults, timedOut) = await server.RunTestsAsync(testsToRun, timeout).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            var finishedTests = testResults.Where(x => x.Node.ExecutionState is not "in-progress").ToList();
            var failedTests = finishedTests.Where(x => x.Node.ExecutionState is "failed").Select(x => x.Node.Uid).ToList();

            RegisterFinishedTestTimings(finishedTests, duration);

            var result = BuildAssemblyResult(finishedTests, failedTests, tests?.Count ?? 0, duration);
            return (result, timedOut);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (new TestRunResult(false, ex.Message), false);
        }
    }

    private List<TestNode>? ResolveTestsForAssembly(string assembly)
    {
        lock (_discoveryLock)
        {
            return _testsByAssembly.TryGetValue(assembly, out var assemblyTests) ? assemblyTests : null;
        }
    }

    private void RegisterFinishedTestTimings(List<TestNodeUpdate> finishedTests, TimeSpan duration)
    {
        lock (_discoveryLock)
        {
            // MTP doesn't report per-test timing, so approximate with the average
            var perTestDuration = finishedTests.Count > 0
                ? TimeSpan.FromTicks(duration.Ticks / finishedTests.Count)
                : TimeSpan.Zero;

            foreach (var testResult in finishedTests.Where(tr => _testDescriptions.ContainsKey(tr.Node.Uid)))
            {
                var testDescription = _testDescriptions[testResult.Node.Uid];
                testDescription.RegisterInitialTestResult(new MtpTestResult(perTestDuration));
            }
        }
    }

    private TestRunResult BuildAssemblyResult(List<TestNodeUpdate> finishedTests, List<string> failedTests, int totalDiscoveredTests, TimeSpan duration)
    {
        var errorMessagesStr = string.Join(Environment.NewLine,
            finishedTests.Where(x => x.Node.ExecutionState is "failed")
                .Select(x => $"{x.Node.DisplayName}{Environment.NewLine}{Environment.NewLine}Test failed"));

        var messages = finishedTests.Select(x =>
            $"{x.Node.DisplayName}{Environment.NewLine}{Environment.NewLine}State: {x.Node.ExecutionState}");

        var executedTestCount = finishedTests.Count;
        var executedTests = totalDiscoveredTests > 0 && executedTestCount >= totalDiscoveredTests
            ? TestIdentifierList.EveryTest()
            : new TestIdentifierList(finishedTests.Select(x => x.Node.Uid));

        var failedTestIds = new TestIdentifierList(failedTests);

        IEnumerable<MtpTestDescription> testDescriptionValues;
        lock (_discoveryLock)
        {
            testDescriptionValues = [.. _testDescriptions.Values];
        }

        return new TestRunResult(
            testDescriptionValues,
            executedTests,
            failedTestIds,
            TestIdentifierList.NoTest(),
            errorMessagesStr,
            messages,
            duration);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged resources used by this <see cref="SingleMicrosoftTestPlatformRunner"/>.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_serverLock)
            {
                foreach (var server in _assemblyServers.Values)
                {
                    server.Dispose();
                }
                _assemblyServers.Clear();
            }

            // Clean up temp files
            try
            {
                if (File.Exists(_mutantFilePath))
                {
                    File.Delete(_mutantFilePath);
                }
                if (File.Exists(_coverageFilePath))
                {
                    File.Delete(_coverageFilePath);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ignore cleanup errors
                LogFailedToCleanUp(_logger, ex, RunnerId);
            }
        }
        _disposed = true;
    }

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Testing mutant(s) [{Mutants}] with active mutation ID: {MutantId}")]
    private static partial void LogTestingMutants(ILogger logger, string runnerId, string mutants, int mutantId);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Resetting test servers to reload assemblies")]
    private static partial void LogResettingServers(ILogger logger, string runnerId);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Test servers reset complete")]
    private static partial void LogServersResetComplete(ILogger logger, string runnerId);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Wrote mutant ID {MutantId} to file {FilePath}")]
    private static partial void LogWroteMutantId(ILogger logger, string runnerId, int mutantId, string filePath);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "{RunnerId}: Failed to write mutant ID to file {FilePath}")]
    private static partial void LogFailedToWriteMutantId(ILogger logger, Exception ex, string runnerId, string filePath);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Coverage mode {Status}")]
    private static partial void LogCoverageMode(ILogger logger, string runnerId, string status);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Coverage file not found at {Path}")]
    private static partial void LogCoverageFileNotFound(ILogger logger, string runnerId, string path);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Read coverage data: {Content}")]
    private static partial void LogReadCoverageData(ILogger logger, string runnerId, string content);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "{RunnerId}: Failed to read coverage file at {Path}")]
    private static partial void LogFailedToReadCoverage(ILogger logger, Exception ex, string runnerId, string path);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "{RunnerId}: Failed to delete coverage file at {Path}")]
    private static partial void LogFailedToDeleteCoverage(ILogger logger, Exception ex, string runnerId, string path);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Discovered {TestCount} tests in {Assembly}")]
    private static partial void LogDiscoveredTests(ILogger logger, string runnerId, int testCount, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Failed to discover tests in {Assembly}")]
    private static partial void LogFailedToDiscover(ILogger logger, Exception ex, string runnerId, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Using {TimeoutMs} ms as test run timeout for {Assembly}")]
    private static partial void LogUsingTimeout(ILogger logger, string runnerId, int timeoutMs, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Test run timed out for {Assembly}")]
    private static partial void LogTestRunTimedOut(ILogger logger, string runnerId, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Restarting test server for {Assembly} after timeout")]
    private static partial void LogRestartingServer(ILogger logger, string runnerId, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Failed to restart test server for {Assembly} after timeout. Creating a new server on next use.")]
    private static partial void LogFailedRestartServer(ILogger logger, Exception ex, string runnerId, string assembly);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{RunnerId}: Failed to run tests for mutant ID {MutantId}")]
    private static partial void LogFailedToRunTests(ILogger logger, Exception ex, string runnerId, int mutantId);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "{RunnerId}: Failed to clean up temp files")]
    private static partial void LogFailedToCleanUp(ILogger logger, Exception ex, string runnerId);
}
