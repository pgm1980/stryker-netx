using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 31 (v2.18.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/DefaultRunnerFactoryTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `new object()` discoveryLock → `new Lock()` (Sprint 2 .NET 10 modernisation).
/// </summary>
public class DefaultRunnerFactoryTests
{
    [Fact]
    public void CreateRunner_ShouldReturnSingleMicrosoftTestPlatformRunner()
    {
        var factory = new DefaultRunnerFactory();
        var testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        var testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        var testSet = new TestSet();
        var discoveryLock = new Lock();
        var logger = NullLogger.Instance;

        var runner = factory.CreateRunner(1, testsByAssembly, testDescriptions, testSet, discoveryLock, logger);

        runner.Should().NotBeNull();
        runner.Should().BeOfType<SingleMicrosoftTestPlatformRunner>();
    }

    [Fact]
    public void CreateRunner_ShouldPassAllParametersToRunner()
    {
        var factory = new DefaultRunnerFactory();
        var testNode = new TestNode("test1", "testMethod1", "test", "pending");
        var testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal)
        {
            ["test.dll"] = [testNode],
        };
        var testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal)
        {
            ["test1"] = new MtpTestDescription(testNode),
        };
        var testSet = new TestSet();
        testSet.RegisterTest(testDescriptions["test1"].Description);
        var discoveryLock = new Lock();
        var logger = NullLogger.Instance;
        const int ExpectedId = 42;

        var runner = factory.CreateRunner(ExpectedId, testsByAssembly, testDescriptions, testSet, discoveryLock, logger);

        runner.Should().NotBeNull();
    }

    [Fact]
    public void CreateRunner_ShouldCreateMultipleIndependentRunners()
    {
        var factory = new DefaultRunnerFactory();
        var testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        var testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        var testSet = new TestSet();
        var discoveryLock = new Lock();
        var logger = NullLogger.Instance;

        var runner1 = factory.CreateRunner(1, testsByAssembly, testDescriptions, testSet, discoveryLock, logger);
        var runner2 = factory.CreateRunner(2, testsByAssembly, testDescriptions, testSet, discoveryLock, logger);

        runner1.Should().NotBeNull();
        runner2.Should().NotBeNull();
        runner1.Should().NotBeSameAs(runner2);
    }

    [Fact]
    public void CreateRunner_ShouldHandleEmptyCollections()
    {
        var factory = new DefaultRunnerFactory();
        var testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        var testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        var testSet = new TestSet();
        var discoveryLock = new Lock();
        var logger = NullLogger.Instance;

        var runner = factory.CreateRunner(0, testsByAssembly, testDescriptions, testSet, discoveryLock, logger);

        runner.Should().NotBeNull();
    }

    [Fact]
    public void CreateRunner_ShouldAcceptDifferentLoggerInstances()
    {
        var factory = new DefaultRunnerFactory();
        var testsByAssembly = new Dictionary<string, List<TestNode>>(StringComparer.Ordinal);
        var testDescriptions = new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal);
        var testSet = new TestSet();
        var discoveryLock = new Lock();

        var mockLogger = new Mock<ILogger>();
        var runner1 = factory.CreateRunner(1, testsByAssembly, testDescriptions, testSet, discoveryLock, mockLogger.Object);
        var runner2 = factory.CreateRunner(2, testsByAssembly, testDescriptions, testSet, discoveryLock, NullLogger.Instance);

        runner1.Should().NotBeNull();
        runner2.Should().NotBeNull();
    }
}
