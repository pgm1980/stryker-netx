using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Stryker.Utilities.Heartbeat;

/// <summary>
/// Sprint 163 (ADR-043, §2 from Aisess Anomalies Report): emits a periodic
/// <see cref="LogLevel.Information"/> heartbeat log line while a long-running
/// phase is in progress, and a <c>completed in …</c> log line on
/// <see cref="Dispose"/>. The phase runs on the caller's thread; the heartbeat
/// runs on a separate <see cref="Timer"/> thread, so the user sees a live
/// "I'm still alive" signal even when the phase's primary thread is blocked
/// inside a synchronous call (e.g. <c>MSBuildWorkspace.OpenProjectAsync().Result</c>
/// during project analysis).
/// </summary>
/// <remarks>
/// <para>
/// Usage pattern: <c>using var heartbeat = new HeartbeatLogger(logger, "Project analysis");</c>
/// at phase-entry. The default 30-second interval matches the bug-reporter's
/// suggestion (§2 + §10 wishlist item #2 + #5). The Timer callback is
/// non-reentrant via an Interlocked CAS guard: if a previous callback hasn't
/// finished, the next tick is silently skipped (no log spam, no
/// out-of-order timing).
/// </para>
/// <para>
/// Threading: the Timer fires on the .NET threadpool. <see cref="ILogger"/>
/// implementations from <c>Microsoft.Extensions.Logging</c> are thread-safe;
/// the consumed providers (Serilog console + file in this project) serialize
/// writes internally.
/// </para>
/// </remarks>
public sealed partial class HeartbeatLogger : IDisposable
{
    /// <summary>
    /// Default heartbeat interval — 30 seconds. Matches the bug-reporter's
    /// stated request (Aisess Anomalies Report, §2 "Suggested diagnostic
    /// improvements" and §10 "Wishlist" item #2).
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger _logger;
    private readonly string _phase;
    private readonly TimeSpan _interval;
    private readonly Stopwatch _stopwatch;
    private readonly Timer _timer;
    private int _isInCallback;
    private int _isDisposed;

    /// <summary>
    /// Initializes a heartbeat for the named <paramref name="phase"/>, emitting
    /// a progress log every <paramref name="interval"/>. The phase is treated
    /// as "started" at construction-time; <see cref="Dispose"/> emits the
    /// "completed" log and stops the timer.
    /// </summary>
    /// <param name="logger">Target logger (must be non-null).</param>
    /// <param name="phase">
    /// Human-readable phase name (e.g. <c>"Project analysis"</c>,
    /// <c>"Initial test run"</c>). Appears in the periodic and completion logs.
    /// </param>
    /// <param name="interval">
    /// Heartbeat interval. Defaults to <see cref="DefaultInterval"/> (30s) when
    /// <c>null</c>. Must be greater than zero. The first heartbeat fires after
    /// one full interval — sub-interval phases produce only the completion log.
    /// </param>
    public HeartbeatLogger(ILogger logger, string phase, TimeSpan? interval = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(phase);
        var effectiveInterval = interval ?? DefaultInterval;
        if (effectiveInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), effectiveInterval, "Interval must be > 0.");
        }

        _logger = logger;
        _phase = phase;
        _interval = effectiveInterval;
        _stopwatch = Stopwatch.StartNew();
        // Use one-shot pattern (dueTime = interval, period = Infinite); the callback
        // re-arms the timer after each successful tick. Prevents reentrancy when the
        // log emit takes longer than the interval.
        _timer = new Timer(OnTimerTick, state: null, dueTime: effectiveInterval, period: Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Stops the timer (synchronously waiting for any in-flight callback to
    /// complete), emits the <c>completed in …</c> log line, and releases
    /// resources. Idempotent — calling <see cref="Dispose"/> more than once is
    /// a no-op.
    /// </summary>
    public void Dispose()
    {
        // Idempotent: only the first call does the work.
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        // Stop the timer and wait for any in-flight callback to finish, so we don't
        // race a heartbeat log against the "completed" log.
        using (var waitHandle = new ManualResetEvent(initialState: false))
        {
            // Timer.Dispose(WaitHandle) signals when all callbacks have completed.
            // Returns false only if the timer is already disposed — safe to ignore.
            _ = _timer.Dispose(waitHandle);
            _ = waitHandle.WaitOne(TimeSpan.FromSeconds(2));
        }

        _stopwatch.Stop();
        // CA1873 false-positive: the outer `IsEnabled` already guards the FormatElapsed
        // call, but the analyzer cannot see through the if-statement to the source-gen
        // partial's internal IsEnabled. FormatElapsed is O(1) (a few int ops + one
        // small-string allocation), so even without the guard the cost would be
        // negligible vs. the disk I/O of the log emit itself.
#pragma warning disable CA1873 // Avoid potentially expensive logging argument evaluation
        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogPhaseCompleted(_logger, _phase, FormatElapsed(_stopwatch.Elapsed));
        }
#pragma warning restore CA1873
    }

    /// <summary>
    /// Timer callback. Uses an Interlocked CAS guard to skip ticks when a
    /// previous emit is still in flight or after the heartbeat has been
    /// disposed (defence-in-depth: the WaitHandle wait in <see cref="Dispose"/>
    /// already serializes the last callback, but a race remains possible
    /// between the disposed check and the actual emit).
    /// </summary>
    private void OnTimerTick(object? state)
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            return;
        }
        if (Interlocked.CompareExchange(ref _isInCallback, 1, 0) != 0)
        {
            // Previous callback still in flight — skip this tick. Will be re-armed
            // by the in-flight callback on its completion.
            return;
        }
        try
        {
            // CA1873 false-positive: the outer `IsEnabled` already guards the FormatElapsed
            // call, but the analyzer cannot see through the combined-condition if-statement
            // to the source-gen partial's internal IsEnabled. FormatElapsed is O(1).
#pragma warning disable CA1873 // Avoid potentially expensive logging argument evaluation
            if (Volatile.Read(ref _isDisposed) == 0 && _logger.IsEnabled(LogLevel.Information))
            {
                LogHeartbeat(_logger, _phase, FormatElapsed(_stopwatch.Elapsed));
            }
#pragma warning restore CA1873
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Intentionally swallow logger exceptions: a heartbeat must NEVER kill
            // the process. Worst case: the user sees no heartbeat (degrades to the
            // pre-Sprint-163 §2 silent-hang UX) but the phase still completes.
        }
        finally
        {
            _ = Interlocked.Exchange(ref _isInCallback, 0);
            // Re-arm the timer for the next tick — but only if we haven't been
            // disposed in the meantime.
            if (Volatile.Read(ref _isDisposed) == 0)
            {
                try
                {
                    _ = _timer.Change(_interval, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // Race with Dispose — safe to ignore.
                }
            }
        }
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as a compact human-readable duration:
    /// <c>"Hh Mm Ss"</c> when hours > 0, otherwise <c>"Mm Ss"</c>. Used in
    /// both the heartbeat and completion log messages.
    /// </summary>
    internal static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }
        var hours = (int)elapsed.TotalHours;
        var minutes = elapsed.Minutes;
        var seconds = elapsed.Seconds;
        if (hours > 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{hours}h {minutes}m {seconds}s");
        }
        return string.Create(CultureInfo.InvariantCulture, $"{minutes}m {seconds}s");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Phase} in progress: {Elapsed} elapsed.")]
    private static partial void LogHeartbeat(ILogger logger, string phase, string elapsed);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Phase} completed in {Elapsed}.")]
    private static partial void LogPhaseCompleted(ILogger logger, string phase, string elapsed);
}
