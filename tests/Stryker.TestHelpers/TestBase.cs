using Microsoft.Extensions.Logging;
using Stryker.Utilities.Logging;

namespace Stryker.TestHelpers;

/// <summary>
/// Shared base class for unit-test classes that touch Stryker.Core or any
/// other module which initialises a logger via
/// <see cref="ApplicationLogging.LoggerFactory"/>. The bootstrap that normally
/// seeds the factory (CLI entry point) does not run in unit-test processes,
/// so consumers either inherit from this base or seed the factory themselves
/// (the <c>Sprint 20 IntegrationTestBase</c> uses the <c>NullLoggerFactory</c>
/// idiom for the same reason).
///
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/TestBase.cs.
/// </summary>
public abstract class TestBase
{
    protected TestBase() =>
        ApplicationLogging.LoggerFactory = new LoggerFactory();
}
