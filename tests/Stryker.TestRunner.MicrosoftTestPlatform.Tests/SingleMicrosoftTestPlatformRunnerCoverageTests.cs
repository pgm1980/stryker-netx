using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 34 (v2.21.0) port of upstream stryker-net 4.14.1
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/SingleMicrosoftTestPlatformRunnerCoverageTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `new object()` discoveryLock → `new Lock()` (Sprint 2 .NET 10 modernisation).
/// `[TestInitialize]` field assignment → constructor (xUnit pattern).
/// Tests use the test assembly itself for real test-discovery via subprocess.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
public class SingleMicrosoftTestPlatformRunnerCoverageTests
{
    private readonly Dictionary<string, List<TestNode>> _testsByAssembly;
    private readonly Dictionary<string, MtpTestDescription> _testDescriptions;
    private readonly TestSet _testSet;
    private readonly Lock _discoveryLock;

    public SingleMicrosoftTestPlatformRunnerCoverageTests()
    {
        _testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        _testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        _testSet = new TestSet();
        _discoveryLock = new Lock();
    }

    [Fact(Skip = "Behaviour delta: upstream MSTest test-host is MTP-native so runner.DiscoverTestsAsync(testAssembly) succeeds against the test assembly itself; stryker-netx tests use xUnit which is not MTP-compatible, so discovery returns False. Investigation sprint TBD.")]
    public async Task SetCoverageMode_ShouldEnableCoverageMode()
    {
        var runnerId = 600;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            // Create an existing coverage file that should be deleted
            await File.WriteAllTextAsync(coverageFilePath, "1,2,3");
            File.Exists(coverageFilePath).Should().BeTrue("Setup: coverage file should exist before test");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            // Create a test assembly to trigger server creation
            var testAssembly = typeof(SingleMicrosoftTestPlatformRunnerCoverageTests).Assembly.Location;
            await runner.DiscoverTestsAsync(testAssembly);

            // Enable coverage mode
            runner.SetCoverageMode(true);

            // The old coverage file should be deleted
            File.Exists(coverageFilePath).Should().BeFalse("Coverage file should be deleted when enabling coverage mode");

            // Servers should be disposed and will be recreated on next use with coverage env var
            // Verify we can still discover tests (which recreates servers)
            var result = await runner.DiscoverTestsAsync(testAssembly);
            result.Should().BeTrue("Server should be recreated successfully after enabling coverage mode");

            // Trying to enable again should be a no-op
            await File.WriteAllTextAsync(coverageFilePath, "test");
            runner.SetCoverageMode(true);
            File.Exists(coverageFilePath).Should().BeTrue("Should not delete file when mode is already enabled");
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact(Skip = "Behaviour delta: upstream MSTest test-host is MTP-native so runner.DiscoverTestsAsync(testAssembly) succeeds against the test assembly itself; stryker-netx tests use xUnit which is not MTP-compatible, so discovery returns False. Investigation sprint TBD.")]
    public async Task SetCoverageMode_ShouldDisableCoverageMode()
    {
        var runnerId = 601;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var testAssembly = typeof(SingleMicrosoftTestPlatformRunnerCoverageTests).Assembly.Location;

            // Enable coverage mode first
            runner.SetCoverageMode(true);
            await runner.DiscoverTestsAsync(testAssembly);

            // Create a coverage file
            await File.WriteAllTextAsync(coverageFilePath, "1,2,3");
            File.Exists(coverageFilePath).Should().BeTrue("Setup: coverage file should exist");

            // Disable coverage mode
            runner.SetCoverageMode(false);

            // The coverage file should be deleted when changing modes (clean start)
            File.Exists(coverageFilePath).Should().BeFalse("Coverage file should be deleted when disabling coverage mode");

            // Servers should be disposed and will be recreated without coverage env var
            var result = await runner.DiscoverTestsAsync(testAssembly);
            result.Should().BeTrue("Server should be recreated successfully after disabling coverage mode");

            // Trying to disable again should be a no-op (no servers disposed, no file deletion)
            await File.WriteAllTextAsync(coverageFilePath, "test");
            runner.SetCoverageMode(false);
            File.Exists(coverageFilePath).Should().BeTrue("Should not delete file when mode is already disabled");
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact(Skip = "Behaviour delta: upstream MSTest test-host is MTP-native so runner.DiscoverTestsAsync(testAssembly) succeeds against the test assembly itself; stryker-netx tests use xUnit which is not MTP-compatible, so discovery returns False. Investigation sprint TBD.")]
    public async Task SetCoverageMode_ShouldNoOp_WhenModeIsAlreadySet()
    {
        var runnerId = 602;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var testAssembly = typeof(SingleMicrosoftTestPlatformRunnerCoverageTests).Assembly.Location;
            await runner.DiscoverTestsAsync(testAssembly);

            // Enable coverage mode
            runner.SetCoverageMode(true);
            File.Exists(coverageFilePath).Should().BeFalse("Coverage file should be deleted on first enable");

            // Create a coverage file to verify no-op doesn't delete it
            await File.WriteAllTextAsync(coverageFilePath, "test-data");

            // Try to enable again - should do nothing (no server disposal, no file deletion)
            runner.SetCoverageMode(true);
            File.Exists(coverageFilePath).Should().BeTrue("Coverage file should NOT be deleted when mode already enabled");
            (await File.ReadAllTextAsync(coverageFilePath)).Should().Be("test-data", "File content should be unchanged");

            // Verify servers are still functional (not disposed)
            var result = await runner.DiscoverTestsAsync(testAssembly);
            result.Should().BeTrue("Servers should still be functional after no-op");

            // Disable coverage mode
            runner.SetCoverageMode(false);

            // Try to disable again - should do nothing (no server disposal)
            runner.SetCoverageMode(false);

            // Verify servers are still functional
            result = await runner.DiscoverTestsAsync(testAssembly);
            result.Should().BeTrue("Servers should still be functional after no-op disable");
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact(Skip = "Behaviour delta: upstream MSTest test-host is MTP-native so runner.DiscoverTestsAsync(testAssembly) succeeds against the test assembly itself; stryker-netx tests use xUnit which is not MTP-compatible, so discovery returns False. Investigation sprint TBD.")]
    public async Task SetCoverageMode_ShouldRestartServers_WhenTogglingBetweenModes()
    {
        var runnerId = 603;

        using var runner = new SingleMicrosoftTestPlatformRunner(
            runnerId,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        var testAssembly = typeof(SingleMicrosoftTestPlatformRunnerCoverageTests).Assembly.Location;

        // Initial discovery without coverage
        var result1 = await runner.DiscoverTestsAsync(testAssembly);
        result1.Should().BeTrue("Initial discovery should succeed");

        // Enable coverage - should restart servers
        runner.SetCoverageMode(true);
        var result2 = await runner.DiscoverTestsAsync(testAssembly);
        result2.Should().BeTrue("Discovery after enabling coverage should succeed (server restarted)");

        // Disable coverage - should restart servers again
        runner.SetCoverageMode(false);
        var result3 = await runner.DiscoverTestsAsync(testAssembly);
        result3.Should().BeTrue("Discovery after disabling coverage should succeed (server restarted)");
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmpty_WhenFileDoesNotExist()
    {
        using var runner = new SingleMicrosoftTestPlatformRunner(
            500,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        var result = runner.ReadCoverageData();

        result.CoveredMutants.Should().BeEmpty();
        result.StaticMutants.Should().BeEmpty();
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmpty_WhenFileIsEmpty()
    {
        var runnerId = 501;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, string.Empty);

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().BeEmpty();
            result.StaticMutants.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmpty_WhenFileContainsWhitespace()
    {
        var runnerId = 502;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, "   \n\t  ");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().BeEmpty();
            result.StaticMutants.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldParseCoveredMutants()
    {
        var runnerId = 503;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, "1,2,3");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().HaveCount(3);
            result.CoveredMutants.Should().Contain(1);
            result.CoveredMutants.Should().Contain(2);
            result.CoveredMutants.Should().Contain(3);
            result.StaticMutants.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldParseCoveredAndStaticMutants()
    {
        var runnerId = 504;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, "1,2,3;10,20");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().HaveCount(3);
            result.CoveredMutants.Should().Contain(1);
            result.CoveredMutants.Should().Contain(2);
            result.CoveredMutants.Should().Contain(3);

            result.StaticMutants.Should().HaveCount(2);
            result.StaticMutants.Should().Contain(10);
            result.StaticMutants.Should().Contain(20);
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleSingleMutant()
    {
        var runnerId = 505;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, "42");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().HaveCount(1);
            result.CoveredMutants.Should().Contain(42);
            result.StaticMutants.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldReturnEmptyCovered_WhenOnlyStaticMutantsPresent()
    {
        var runnerId = 506;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, ";5,6,7");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().BeEmpty();
            result.StaticMutants.Should().HaveCount(3);
            result.StaticMutants.Should().Contain(5);
            result.StaticMutants.Should().Contain(6);
            result.StaticMutants.Should().Contain(7);
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact]
    public void ReadCoverageData_ShouldHandleTrailingSemicolon()
    {
        var runnerId = 507;
        var coverageFilePath = Path.Combine(Path.GetTempPath(), $"stryker-coverage-{runnerId}.txt");

        try
        {
            File.WriteAllText(coverageFilePath, "1,2,3;");

            using var runner = new SingleMicrosoftTestPlatformRunner(
                runnerId,
                _testsByAssembly,
                _testDescriptions,
                _testSet,
                _discoveryLock,
                NullLogger.Instance);

            var result = runner.ReadCoverageData();

            result.CoveredMutants.Should().HaveCount(3);
            result.StaticMutants.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(coverageFilePath))
            {
                File.Delete(coverageFilePath);
            }
        }
    }

    [Fact(Skip = "Behaviour delta: upstream MSTest test-host is MTP-native so runner.DiscoverTestsAsync(testAssembly) populates _assemblyServers; stryker-netx tests use xUnit which is not MTP-compatible, so discovery doesn't populate the dictionary. Investigation sprint TBD.")]
    public async Task ResetServerAsync_ShouldDisposeAndClearAllServers()
    {
        using var runner = new SingleMicrosoftTestPlatformRunner(
            0,
            _testsByAssembly,
            _testDescriptions,
            _testSet,
            _discoveryLock,
            NullLogger.Instance);

        // Populate _assemblyServers by discovering tests against the real test assembly
        var testAssembly = typeof(SingleMicrosoftTestPlatformRunnerCoverageTests).Assembly.Location;
        await runner.DiscoverTestsAsync(testAssembly);

        var serversField = typeof(SingleMicrosoftTestPlatformRunner)
            .GetField("_assemblyServers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var serversBefore = (Dictionary<string, AssemblyTestServer>)serversField.GetValue(runner)!;
        serversBefore.Should().NotBeEmpty("servers should be populated after discovery");

        await runner.ResetServerAsync();

        var serversAfter = (Dictionary<string, AssemblyTestServer>)serversField.GetValue(runner)!;
        serversAfter.Should().BeEmpty("all servers should be disposed and removed after reset");
    }
}
