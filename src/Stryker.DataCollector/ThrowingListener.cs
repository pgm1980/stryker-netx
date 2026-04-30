using System;
using System.Diagnostics;

namespace Stryker.DataCollector;

internal sealed class ThrowingListener : TraceListener
{
    public override void Fail(string? message) => throw new ArgumentException(message, nameof(message));

    public override void Fail(string? message, string? detailMessage) => throw new ArgumentException(detailMessage, message);

    public override void Write(string? message)
    {
        // Intentionally empty — Trace.Write calls are silenced during mutation runs.
    }

    public override void WriteLine(string? message)
    {
        // Intentionally empty — Trace.WriteLine calls are silenced during mutation runs.
    }
}
