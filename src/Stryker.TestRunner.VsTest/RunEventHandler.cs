using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// VsTest <see cref="ITestRunEventsHandler"/> implementation that aggregates test results and exposes them
/// via <see cref="IRunResults"/>. The class deliberately keeps the upstream Stryker.NET 4.14.1 name
/// (<c>RunEventHandler</c>); CA1711 is project-wide silenced for matched-suffix VsTest event handlers.
/// </summary>
public sealed partial class RunEventHandler : ITestRunEventsHandler
{
    private readonly ILogger _logger;
    private readonly string _runnerId;
    private readonly IDictionary<Guid, VsTestDescription> _vsTests;
    private readonly Dictionary<Guid, TestRun> _runs = [];
    private readonly Dictionary<Guid, TestCase> _inProgress = [];
    private SimpleRunResults _currentResults = new();
    private readonly List<TestResult> _rawResults = [];
    private int _initialResultsCount;
    // MA0158 suggests System.Threading.Lock here, but we use Monitor.Pulse/Wait which only operate on object-typed sync roots.
    // The new Lock type is monitor-incompatible (CS9216) when converted, so we keep a plain object as the sync primitive.
#pragma warning disable MA0158 // Monitor.Pulse/Wait require object-typed sync root, not System.Threading.Lock
    private readonly object _lck = new();
#pragma warning restore MA0158
    private bool _completed;

    /// <summary>Fired whenever a chunk of new results is received.</summary>
    public event EventHandler? ResultsUpdated;

    /// <summary>Set to true to mark the session as cancelled by the orchestrator.</summary>
    public bool CancelRequested { get; set; }

    /// <summary>True when VsTest reported a fatal error in the run.</summary>
    public bool Failed { get; private set; }

    /// <summary>Initializes a new <see cref="RunEventHandler"/>.</summary>
    public RunEventHandler(IDictionary<Guid, VsTestDescription> vsTests, ILogger logger, string runnerId)
    {
        _vsTests = vsTests;
        _logger = logger;
        _runnerId = runnerId;
    }

    private void CaptureTestResults(IEnumerable<TestResult> results)
    {
        var testResults = results as TestResult[] ?? [.. results];
        _rawResults.AddRange(testResults);
        AnalyzeRawTestResults(testResults);
    }

    private void AnalyzeRawTestResults(IEnumerable<TestResult> testResults)
    {
        foreach (var testResult in testResults)
        {
            var id = testResult.TestCase.Id;
            if (!_runs.TryGetValue(id, out var run))
            {
                if (_vsTests.TryGetValue(id, out var test))
                {
                    run = new TestRun(test);
                }
                else
                {
                    // unknown id. Probable cause: test name has changed due to some parameter having changed
                    run = new TestRun(new VsTestDescription(new VsTestCase(testResult.TestCase)));
                }
                _runs[id] = run;
            }

            if (run.IsComplete())
            {
                // unexpected result, report it
                _currentResults.TestResults.Add(testResult);
            }
            else if (run.AddResult(testResult))
            {
                var aggregate = run.Result();
                if (aggregate is not null)
                {
                    _currentResults.TestResults.Add(aggregate);
                }
                _inProgress.Remove(id);
            }
        }
    }

    /// <summary>Returns the raw (non-aggregated) results captured so far.</summary>
    public IRunResults GetRawResults() => new SimpleRunResults(_rawResults, _currentResults.TestsInTimeout);

    /// <summary>Returns the aggregated current results.</summary>
    public IRunResults GetResults() => _currentResults;

    /// <inheritdoc />
    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        if (testRunChangedArgs is null)
        {
            return;
        }

        if (testRunChangedArgs.ActiveTests != null)
        {
            foreach (var activeTest in testRunChangedArgs.ActiveTests)
            {
                _inProgress[activeTest.Id] = activeTest;
            }
        }

        if (testRunChangedArgs.NewTestResults == null || !testRunChangedArgs.NewTestResults.Any())
        {
            return;
        }

        CaptureTestResults(testRunChangedArgs.NewTestResults);
        ResultsUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void HandleTestRunComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {
        LogTestRunComplete(_logger, _runnerId);
        if (lastChunkArgs?.ActiveTests != null)
        {
            foreach (var activeTest in lastChunkArgs.ActiveTests)
            {
                _inProgress[activeTest.Id] = activeTest;
            }
        }

        if (lastChunkArgs?.NewTestResults != null)
        {
            CaptureTestResults(lastChunkArgs.NewTestResults);
        }

        if (!testRunCompleteArgs.IsCanceled && (_inProgress.Count != 0 || _runs.Values.Any(t => !t.IsComplete())))
        {
            // report ongoing tests and test case with missing results as timeouts.
            _currentResults.SetTestsInTimeOut([.. _inProgress.Values
                .Union(_runs.Values.Where(t => !t.IsComplete()).Select(t => t.Result()!.TestCase))]);
        }

        ResultsUpdated?.Invoke(this, EventArgs.Empty);

        LogTestRunErrors(testRunCompleteArgs);

        lock (_lck)
        {
            _completed = true;
            Monitor.Pulse(_lck);
        }
    }

    private void LogTestRunErrors(TestRunCompleteEventArgs testRunCompleteArgs)
    {
        if (testRunCompleteArgs.Error == null)
        {
            return;
        }

        if (testRunCompleteArgs.Error.GetType() == typeof(TransationLayerException))
        {
            LogVsTestCrashed(_logger, testRunCompleteArgs.Error, _runnerId);
            Failed = true;
        }
        else if (testRunCompleteArgs.Error.InnerException is System.IO.IOException sock)
        {
            LogTestSessionUnexpected(_logger, sock, _runnerId);
        }
        else if (!CancelRequested)
        {
            LogVsTestError(_logger, testRunCompleteArgs.Error, _runnerId);
        }
    }

    /// <inheritdoc />
    public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public void HandleRawMessage(string rawMessage)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            LogRawMessage(_logger, _runnerId, rawMessage);
        }
    }

    /// <summary>Resets internal state for a new VsTest session.</summary>
    public void StartSession()
    {
        _completed = false;
        Failed = false;
        _initialResultsCount = _rawResults.Count;
        _inProgress.Clear();
        _runs.Clear();
    }

    /// <summary>Discards the most recent batch of results so the run can be retried.</summary>
    public void DiscardCurrentRun()
    {
        // remove all raw results from this run
        _rawResults.RemoveRange(_initialResultsCount, _rawResults.Count - _initialResultsCount);
        // we reanalyze results gathered so far, in an event sourced way
        _runs.Clear();
        _currentResults = new SimpleRunResults([], _currentResults.TestsInTimeout);
        AnalyzeRawTestResults(_rawResults);
    }

    /// <summary>Blocks until completion or the timeout elapses.</summary>
    public bool Wait(int timeOut, out bool slept)
    {
        lock (_lck)
        {
            var watch = new Stopwatch();
            watch.Start();

            while (!_completed && watch.ElapsedMilliseconds < timeOut)
            {
                Monitor.Wait(_lck, Math.Max(0, (int)(timeOut - watch.ElapsedMilliseconds)));
            }

            slept = watch.ElapsedMilliseconds - timeOut > 30 * 1000;
            if (slept)
            {
                LogComputerSlept(_logger, _runnerId);
            }

            return _completed;
        }
    }

    /// <inheritdoc />
    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        var levelFinal = level switch
        {
            TestMessageLevel.Informational => LogLevel.Debug,
            TestMessageLevel.Warning => LogLevel.Warning,
            TestMessageLevel.Error => LogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var levelString = levelFinal.ToString();
            LogVsTestLog(_logger, _runnerId, levelString, message ?? string.Empty);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "{RunnerId}: Received testrun complete.")]
    private static partial void LogTestRunComplete(ILogger logger, string runnerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{RunnerId}: VsTest may have crashed, triggering VsTest restart!")]
    private static partial void LogVsTestCrashed(ILogger logger, Exception ex, string runnerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RunnerId}: Test session ended unexpectedly.")]
    private static partial void LogTestSessionUnexpected(ILogger logger, Exception ex, string runnerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{RunnerId}: VsTest error.")]
    private static partial void LogVsTestError(ILogger logger, Exception ex, string runnerId);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{RunnerId}: {RawMessage} [RAW]")]
    private static partial void LogRawMessage(ILogger logger, string runnerId, string rawMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RunnerId}: the computer slept during the testing, need to retry")]
    private static partial void LogComputerSlept(ILogger logger, string runnerId);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{RunnerId}: [{LevelFinal}] {Message}")]
    private static partial void LogVsTestLog(ILogger logger, string runnerId, string levelFinal, string message);
}
