using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// VsTest <see cref="ITestDiscoveryEventsHandler"/> implementation that aggregates discovered test cases
/// and signals completion via Monitor.
/// </summary>
public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    private readonly IList<string> _messages;
    // MA0158 suggests System.Threading.Lock here, but we use Monitor.Pulse/Wait which only operate on object-typed sync roots.
    // The new Lock type is monitor-incompatible (CS9216) when converted, so we keep a plain object as the sync primitive.
#pragma warning disable MA0158 // Monitor.Pulse/Wait require object-typed sync root, not System.Threading.Lock
    private readonly object _lck = new();
#pragma warning restore MA0158
    private bool _discoveryDone;

    /// <summary>
    /// Discovered test cases collected during the run.
    /// </summary>
    public IList<TestCase> DiscoveredTestCases { get; private set; }

    /// <summary>
    /// True when the discovery was aborted by the test platform.
    /// </summary>
    public bool Aborted { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="DiscoveryEventHandler"/> with the supplied message sink.
    /// </summary>
    public DiscoveryEventHandler(IList<string> messages)
    {
        DiscoveredTestCases = [];
        _messages = messages;
    }

    /// <inheritdoc />
    public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases)
    {
        if (discoveredTestCases != null)
        {
            foreach (var tc in discoveredTestCases)
            {
                DiscoveredTestCases.Add(tc);
            }
        }
    }

    /// <inheritdoc />
    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            foreach (var tc in lastChunk)
            {
                DiscoveredTestCases.Add(tc);
            }
        }

        Aborted = isAborted;
        lock (_lck)
        {
            _discoveryDone = true;
            Monitor.Pulse(_lck);
        }
    }

    // Sprint 182 (issue #297b, I-07b): generous ceiling — discovery of huge suites is slow,
    // but a vstest.console crash before DiscoveryComplete must not hang the run forever.
    private static readonly TimeSpan DefaultDiscoveryTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Blocks until <see cref="HandleDiscoveryComplete"/> has been called or the default
    /// five-minute ceiling elapses; a timeout surfaces through <see cref="Aborted"/>.
    /// </summary>
    public void WaitEnd() => WaitEnd(DefaultDiscoveryTimeout);

    /// <summary>
    /// Blocks until <see cref="HandleDiscoveryComplete"/> has been called or the given
    /// timeout elapses.
    /// </summary>
    /// <param name="timeout">maximum time to wait for the discovery to complete</param>
    /// <returns>true when the discovery completed; false on timeout (with <see cref="Aborted"/> set)</returns>
    public bool WaitEnd(TimeSpan timeout)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        lock (_lck)
        {
            while (!_discoveryDone)
            {
                var remaining = timeout - elapsed.Elapsed;
                if (remaining <= TimeSpan.Zero || !Monitor.Wait(_lck, remaining))
                {
                    Aborted = true;
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void HandleRawMessage(string rawMessage) => _messages.Add("Test Discovery Raw Message: " + rawMessage);

    /// <inheritdoc />
    public void HandleLogMessage(TestMessageLevel level, string? message) => _messages.Add("Test Discovery Message: " + message);
}
