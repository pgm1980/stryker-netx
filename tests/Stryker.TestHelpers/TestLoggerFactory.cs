using Microsoft.Extensions.Logging;
using Moq;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 25 (v2.12.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/TestLoggerFactory.cs.
/// Convenience factory for Moq-backed <see cref="ILogger{T}"/> instances
/// used by tests that need a logger argument they don't actually verify on.
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>Creates a Moq-backed <see cref="ILogger{T}"/> object for tests.</summary>
    public static ILogger<T> CreateLogger<T>() => new Mock<ILogger<T>>().Object;

    /// <summary>Creates a Moq mock so tests that need to verify log calls retain the underlying mock.</summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>() => new();
}
