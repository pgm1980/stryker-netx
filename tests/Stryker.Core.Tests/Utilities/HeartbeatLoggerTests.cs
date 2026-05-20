// xUnit1030 vs MA0004 conflict — xUnit forbids ConfigureAwait(false) in test methods, but
// Meziantou.Analyzer (MA0004) requires it on every async call. xUnit wins per established
// stryker-netx convention (Sprint 32/33 memory note: "xUnit1030 + MA0004 file-level
// suppression for long test files with many async patterns"). File-level pragma keeps
// individual test methods clean.
#pragma warning disable MA0004 // ConfigureAwait(false) — superseded by xUnit1030 in test files
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stryker.Utilities.Heartbeat;
using Xunit;

namespace Stryker.Core.Tests.Utilities;

/// <summary>
/// Sprint 163 (ADR-043, §2 from Aisess Anomalies Report): unit tests for
/// <see cref="HeartbeatLogger"/>. Covers periodic heartbeat emission,
/// completion log on dispose, and FormatElapsed boundary cases.
/// </summary>
public sealed class HeartbeatLoggerTests
{
    /// <summary>
    /// Minimal in-memory logger that captures every emitted message+state pair
    /// for assertion. Thread-safe (the Timer callback runs on the threadpool).
    /// </summary>
    private sealed class CapturingLogger : ILogger
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Entries.Enqueue((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { /* no-op scope */ }
        }
    }

    /// <summary>
    /// Heartbeat with a sub-interval phase (dispose immediately) must emit
    /// exactly one log entry: the "completed in …" line. No periodic heartbeat
    /// fires because the dueTime has not elapsed.
    /// </summary>
    [Fact]
    public void Dispose_Without_Tick_Emits_Only_Completed_Log()
    {
        var logger = new CapturingLogger();

        using (var unused = new HeartbeatLogger(logger, "TestPhase", TimeSpan.FromSeconds(60)))
        {
            // Phase finishes immediately — no time for a heartbeat tick.
            _ = unused;
        }

        logger.Entries.Should().HaveCount(1, "only the completion log is expected when no heartbeat tick has fired");
        var entry = logger.Entries.Single();
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("TestPhase completed in")
            .And.MatchRegex(@"TestPhase completed in (\d+h )?\d+m \d+s\.");
    }

    /// <summary>
    /// Heartbeat with a short interval and a moderate-length phase emits at
    /// least one periodic heartbeat plus the completion log. The exact tick
    /// count is timing-dependent (threadpool jitter); we assert ≥1 to keep
    /// the test non-flaky under CI load.
    /// </summary>
    [Fact]
    public async Task Long_Phase_Emits_Periodic_Heartbeat_Plus_Completion()
    {
        var logger = new CapturingLogger();

        using (var unused = new HeartbeatLogger(logger, "PA", TimeSpan.FromMilliseconds(100)))
        {
            // Allow time for ≥2 heartbeat ticks. 500ms tolerates ~3× tick jitter.
            await Task.Delay(500);
            _ = unused;
        }

        var entries = logger.Entries.ToArray();
        entries.Should().HaveCountGreaterThanOrEqualTo(2, "expect at least one heartbeat plus the completion log");

        var heartbeats = entries.Where(e => e.Message.Contains("PA in progress", StringComparison.Ordinal)).ToArray();
        var completions = entries.Where(e => e.Message.Contains("PA completed in", StringComparison.Ordinal)).ToArray();

        heartbeats.Should().NotBeEmpty("the timer must have ticked at least once in 500ms with a 100ms interval");
        completions.Should().HaveCount(1, "exactly one completion log is expected");
    }

    /// <summary>
    /// After Dispose, the Timer must stop firing. Sleeping beyond several
    /// intervals must not produce any new log entries.
    /// </summary>
    [Fact]
    public async Task Dispose_Stops_Timer_From_Firing_Further()
    {
        var logger = new CapturingLogger();

        var heartbeat = new HeartbeatLogger(logger, "P", TimeSpan.FromMilliseconds(50));
        await Task.Delay(250);
        heartbeat.Dispose();
        var entriesAfterDispose = logger.Entries.Count;

        await Task.Delay(300);

        logger.Entries.Count.Should().Be(entriesAfterDispose, "no new log entries should appear after Dispose");
    }

    /// <summary>
    /// Dispose is idempotent — calling twice is a no-op, not a double-log
    /// emission nor an exception.
    /// </summary>
    [Fact]
    public void Dispose_Is_Idempotent()
    {
        var logger = new CapturingLogger();

        var heartbeat = new HeartbeatLogger(logger, "P", TimeSpan.FromSeconds(60));
        heartbeat.Dispose();
        var entriesAfterFirstDispose = logger.Entries.Count;

        Action secondDispose = heartbeat.Dispose;
        secondDispose.Should().NotThrow();

        logger.Entries.Count.Should().Be(entriesAfterFirstDispose, "the second Dispose must not emit a second completion log");
    }

    /// <summary>
    /// Constructor must reject invalid arguments. Logger=null, phase=null/empty,
    /// interval≤0 all throw, before any timer/stopwatch resources are allocated.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Rejects_Non_Positive_Interval(int intervalMs)
    {
        Action act = () => _ = new HeartbeatLogger(new CapturingLogger(), "P", TimeSpan.FromMilliseconds(intervalMs));
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("interval");
    }

    [Fact]
    public void Constructor_Rejects_Null_Logger()
    {
        Action act = () => _ = new HeartbeatLogger(null!, "P");
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Rejects_Empty_Phase()
    {
        Action act = () => _ = new HeartbeatLogger(new CapturingLogger(), string.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("phase");
    }

    /// <summary>
    /// FormatElapsed produces "Mm Ss" for sub-hour durations and "Hh Mm Ss"
    /// for ≥1-hour durations. Exercised via direct invocation (internal
    /// visible via InternalsVisibleTo or via Reflection — here, we route
    /// through the heartbeat's completion log which contains the formatted
    /// elapsed string).
    /// </summary>
    [Theory]
    [InlineData(0, 0, 5, "0m 5s")]
    [InlineData(0, 1, 30, "1m 30s")]
    [InlineData(0, 59, 59, "59m 59s")]
    [InlineData(1, 0, 0, "1h 0m 0s")]
    [InlineData(2, 30, 15, "2h 30m 15s")]
    [InlineData(12, 0, 1, "12h 0m 1s")]
    public void FormatElapsed_Produces_Expected_String(int hours, int minutes, int seconds, string expected)
    {
        // Use reflection to invoke the internal static FormatElapsed for boundary tests
        var method = typeof(HeartbeatLogger).GetMethod("FormatElapsed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull("FormatElapsed is internal-static and must exist");

        var elapsed = new TimeSpan(hours, minutes, seconds);
        var result = (string?)method!.Invoke(null, [elapsed]);

        result.Should().Be(expected, string.Create(CultureInfo.InvariantCulture, $"Expected {expected} for {hours}h {minutes}m {seconds}s"));
    }

    /// <summary>
    /// FormatElapsed maps negative durations to zero (defensive against
    /// time-going-backwards edge cases). Behaviour: returns "0m 0s".
    /// </summary>
    [Fact]
    public void FormatElapsed_Negative_Returns_Zero()
    {
        var method = typeof(HeartbeatLogger).GetMethod("FormatElapsed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (string?)method!.Invoke(null, [TimeSpan.FromSeconds(-5)]);

        result.Should().Be("0m 0s", "negative durations are clamped to zero by the format helper");
    }

    /// <summary>
    /// Smoke test: invariant-culture formatting is preserved across cultures
    /// (so the heartbeat log doesn't render "1,5h 0m 0s" on de-DE locales).
    /// </summary>
    [Fact]
    public void FormatElapsed_Uses_InvariantCulture()
    {
        var method = typeof(HeartbeatLogger).GetMethod("FormatElapsed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var result = (string?)method!.Invoke(null, [new TimeSpan(0, 1, 30)]);
            result.Should().Be("1m 30s", "format must remain culture-invariant");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}
