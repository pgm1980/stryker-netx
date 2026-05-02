using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/LoggerMockExtensions.cs.
/// Sprint 96 (v2.82.0): generalized to also support [LoggerMessage] source-gen state objects
/// (which generate strongly-typed structs whose ToString() returns the formatted message).
///
/// Moq helpers around the <c>Microsoft.Extensions.Logging.ILogger</c> shape so
/// tests can assert on log calls by message + level without the verbose
/// <c>Verify(x =&gt; x.Log(level, EventId, It.Is&lt;...&gt;, ...))</c> boilerplate.
/// </summary>
public static class LoggerMockExtensions
{
    /// <summary>Sprint 96: enables IsEnabled for all log levels so production code that guards
    /// Log calls with `if (logger.IsEnabled(...))` (CA1873) actually emits log calls in tests.
    /// Call once per Mock&lt;ILogger&lt;T&gt;&gt; before any .Verify(...) on that mock that depends on
    /// IsEnabled-guarded log statements.</summary>
#pragma warning disable CA1873
    public static void EnableAllLogLevels<T>(this Mock<ILogger<T>> mock) =>
        mock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
#pragma warning restore CA1873

    /// <summary>Verifies that the given message was logged with the given LogLevel exactly once.</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message) =>
        mock.Verify(logLevel, message, Times.Once);

    /// <summary>Verifies that the given message was logged with the given LogLevel the supplied number of times (factory variant).</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message, Func<Times> times) =>
        mock.Verify(logLevel, message, times());

    /// <summary>Verifies that the given message was logged with the given LogLevel the supplied number of times.
    /// Sprint 96: matches both classical structured-logging state (anonymous-type / IReadOnlyList&lt;KeyValuePair&gt;)
    /// AND [LoggerMessage] source-gen value-type state structs. The match strategy uses the formatter's
    /// rendered output (state.ToString()) — which is identical for both code paths.</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message, Times times) =>
        // CA1873 flags `x.Log(...)` calls inside expressions as "potentially expensive logging that should be guarded"
        // — false positive: this is an Moq.Verify call describing an EXPECTED log invocation, not a runtime log call,
        // so the guard would silence the verification rather than save runtime cost.
#pragma warning disable CA1873
        mock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => MatchesRenderedMessage(o, message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
#pragma warning restore CA1873

    /// <summary>Compares the structured-logging state object's rendered output against an expected message.
    /// Handles both anonymous-type/dictionary state and [LoggerMessage]-generated value-type state structs:
    /// both expose ToString() that returns the formatted message.</summary>
    private static bool MatchesRenderedMessage(object? state, string expected)
    {
        if (state is null)
        {
            return false;
        }
        var rendered = state.ToString();
        return rendered is not null && string.Equals(expected, rendered, StringComparison.OrdinalIgnoreCase);
    }
}
