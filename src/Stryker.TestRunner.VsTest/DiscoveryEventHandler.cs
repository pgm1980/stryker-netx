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

    /// <summary>
    /// Blocks until <see cref="HandleDiscoveryComplete"/> has been called.
    /// </summary>
    public void WaitEnd()
    {
        lock (_lck)
        {
            while (!_discoveryDone)
            {
                Monitor.Wait(_lck);
            }
        }
    }

    /// <inheritdoc />
    public void HandleRawMessage(string rawMessage) => _messages.Add("Test Discovery Raw Message: " + rawMessage);

    /// <inheritdoc />
    public void HandleLogMessage(TestMessageLevel level, string? message) => _messages.Add("Test Discovery Message: " + message);
}
