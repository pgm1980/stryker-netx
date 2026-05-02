using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Testing;
using Stryker.Core.Initialisation;
using Stryker.Core.Mutants;
using Stryker.Core.MutationTest;
using Stryker.TestHelpers;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutationTest;

/// <summary>Sprint 79 (v2.65.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MutationTestExecutorTests : TestBase
{
    [Fact]
    public async Task MutationTestExecutor_NoFailedTestShouldBeSurvived()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        var mutant = new Mutant { Id = 1 };
        testRunnerMock.Setup(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), It.IsAny<ITimeoutValueCalculator>(), It.IsAny<IReadOnlyList<IMutant>>(), null!))
            .Returns(Task.FromResult(new TestRunResult(true) as ITestRunResult));

        var loggerMock = new Mock<ILogger<MutationTestExecutor>>();
        var target = new MutationTestExecutor(loggerMock.Object)
        {
            TestRunner = testRunnerMock.Object,
        };

        await target.TestAsync(null!, [mutant], null!, null!);

        mutant.ResultStatus.Should().Be(MutantStatus.Survived);
        testRunnerMock.Verify(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), It.IsAny<ITimeoutValueCalculator>(), It.IsAny<IReadOnlyList<IMutant>>(), null!), Times.Once);
    }

    [Fact]
    public async Task MutationTestExecutor_FailedTestShouldBeKilled()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        var mutant = new Mutant { Id = 1, CoveringTests = TestIdentifierList.EveryTest() };
        testRunnerMock.Setup(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), null!, It.IsAny<IReadOnlyList<IMutant>>(), null!))
            .Returns(Task.FromResult(new TestRunResult(false) as ITestRunResult));

        var loggerMock = new Mock<ILogger<MutationTestExecutor>>();
        var target = new MutationTestExecutor(loggerMock.Object)
        {
            TestRunner = testRunnerMock.Object,
        };

        await target.TestAsync(null!, [mutant], null!, null!);

        mutant.ResultStatus.Should().Be(MutantStatus.Killed);
        testRunnerMock.Verify(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), null!, It.IsAny<IReadOnlyList<IMutant>>(), null!), Times.Once);
    }

    [Fact]
    public async Task MutationTestExecutor_TimeoutShouldBePassedToProcessTimeout()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        var mutant = new Mutant { Id = 1, CoveringTests = TestIdentifierList.EveryTest() };
        testRunnerMock.Setup(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), It.IsAny<ITimeoutValueCalculator>(), It.IsAny<IReadOnlyList<IMutant>>(), null!))
            .Returns(Task.FromResult(TestRunResult.TimedOut(new List<VsTestDescription>(), TestIdentifierList.NoTest(), TestIdentifierList.NoTest(), TestIdentifierList.EveryTest(), "", [], TimeSpan.Zero) as ITestRunResult));

        var loggerMock = new Mock<ILogger<MutationTestExecutor>>();
        var target = new MutationTestExecutor(loggerMock.Object)
        {
            TestRunner = testRunnerMock.Object,
        };

        var timeoutValueCalculator = new TimeoutValueCalculator(500);
        await target.TestAsync(null!, [mutant], timeoutValueCalculator, null!);

        mutant.ResultStatus.Should().Be(MutantStatus.Timeout);
        testRunnerMock.Verify(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), timeoutValueCalculator, It.IsAny<IReadOnlyList<IMutant>>(), null!), Times.Once);
    }

    [Fact]
    public async Task MutationTestExecutor_ShouldSwitchToSingleModeOnDubiousTimeouts()
    {
        var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
        var mutant1 = new Mutant { Id = 1, CoveringTests = TestIdentifierList.EveryTest() };
        var mutant2 = new Mutant { Id = 2, CoveringTests = TestIdentifierList.EveryTest() };
        testRunnerMock.Setup(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), It.IsAny<ITimeoutValueCalculator>(), It.IsAny<IReadOnlyList<IMutant>>(), null!))
            .Returns(Task.FromResult(TestRunResult.TimedOut(new List<VsTestDescription>(), TestIdentifierList.NoTest(), TestIdentifierList.NoTest(), TestIdentifierList.NoTest(), "", [], TimeSpan.Zero) as ITestRunResult));

        var loggerMock = new Mock<ILogger<MutationTestExecutor>>();
        var target = new MutationTestExecutor(loggerMock.Object)
        {
            TestRunner = testRunnerMock.Object,
        };

        var timeoutValueCalculator = new TimeoutValueCalculator(500);
        await target.TestAsync(null!, [mutant1, mutant2], timeoutValueCalculator, null!);

        mutant1.ResultStatus.Should().Be(MutantStatus.Timeout);
        mutant2.ResultStatus.Should().Be(MutantStatus.Timeout);
        testRunnerMock.Verify(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), timeoutValueCalculator, It.IsAny<IReadOnlyList<IMutant>>(), null!), Times.Exactly(3));
    }
}
