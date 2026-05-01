using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/LoggerMockExtensions.cs.
/// Moq helpers around the <c>Microsoft.Extensions.Logging.ILogger</c> shape so
/// tests can assert on log calls by message + level without the verbose
/// <c>Verify(x =&gt; x.Log(level, EventId, It.Is&lt;...&gt;, ...))</c> boilerplate.
/// </summary>
public static class LoggerMockExtensions
{
    /// <summary>Verifies that the given message was logged with the given LogLevel exactly once.</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message) =>
        mock.Verify(logLevel, message, Times.Once);

    /// <summary>Verifies that the given message was logged with the given LogLevel the supplied number of times (factory variant).</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message, Func<Times> times) =>
        mock.Verify(logLevel, message, times());

    /// <summary>Verifies that the given message was logged with the given LogLevel the supplied number of times.</summary>
    public static void Verify<T>(this Mock<ILogger<T>> mock, LogLevel logLevel, string message, Times times) =>
        // CA1873 flags `x.Log(...)` calls inside expressions as "potentially expensive logging that should be guarded"
        // — false positive: this is an Moq.Verify call describing an EXPECTED log invocation, not a runtime log call,
        // so the guard would silence the verification rather than save runtime cost.
#pragma warning disable CA1873
        mock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(message, o.ToString(), StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
#pragma warning restore CA1873
}
