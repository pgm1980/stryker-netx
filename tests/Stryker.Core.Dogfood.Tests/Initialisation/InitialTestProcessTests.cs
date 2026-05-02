using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Testing;
using Stryker.Configuration.Options;
using Stryker.Core.Initialisation;
using Stryker.TestHelpers;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 81 (v2.67.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: TestLoggerFactory.CreateLogger needs ApplicationLogging.LoggerFactory seeded.</summary>
public class InitialTestProcessTests : TestBase
{
    private readonly InitialTestProcess _target;
    private readonly StrykerOptions _options;

    public InitialTestProcessTests()
    {
        _target = new InitialTestProcess(TestLoggerFactory.CreateLogger<InitialTestProcess>());
        _options = new StrykerOptions
        {
            AdditionalTimeout = 0,
        };
    }

    [Fact]
    public async Task InitialTestProcess_ShouldNotThrowIfAFewTestsFail()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        var test1 = "test1";
        var testList = new List<string>(10) { test1 };
        for (var i = testList.Count; i < testList.Capacity; i++)
        {
            testList.Add("test" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        var ranTests = new TestIdentifierList(testList);
        var failedTests = new TestIdentifierList(test1);
        var testRunResult = Task.FromResult(new TestRunResult([], ranTests, failedTests,
            TestIdentifierList.NoTest(), string.Empty, [], TimeSpan.Zero) as ITestRunResult);
        testRunnerMock.Setup(x => x.InitialTestAsync(It.IsAny<IProjectAndTests>())).Returns(testRunResult);
        testRunnerMock.Setup(x => x.DiscoverTestsAsync(It.IsAny<string>())).Returns(Task.FromResult(true));
        testRunnerMock.Setup(x => x.GetTests(It.IsAny<IProjectAndTests>())).Returns(new TestSet());

        await _target.InitialTestAsync(_options, null!, testRunnerMock.Object);

        testRunnerMock.Verify(p => p.InitialTestAsync(It.IsAny<IProjectAndTests>()), Times.Once);
    }

    [Fact]
    public async Task InitialTestProcess_ShouldCalculateTestTimeout()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        testRunnerMock.Setup(x => x.InitialTestAsync(It.IsAny<IProjectAndTests>()))
            .Returns(async () =>
            {
                await Task.Delay(10).ConfigureAwait(false);
                return new TestRunResult(true) as ITestRunResult;
            });
        testRunnerMock.Setup(x => x.DiscoverTestsAsync(It.IsAny<string>())).Returns(Task.FromResult(true));
        testRunnerMock.Setup(x => x.GetTests(It.IsAny<IProjectAndTests>())).Returns(new TestSet());

        var result = await _target.InitialTestAsync(_options, null!, testRunnerMock.Object);

        result.TimeoutValueCalculator.DefaultTimeout.Should().BeInRange(1, 200,
            "This test contains a Thread.Sleep to simulate time passing as this test is testing that a stopwatch is used correctly to measure time.\nIf this test is failing for unclear reasons, perhaps the computer running the test is too slow causing the time estimation to be off");
    }
}
