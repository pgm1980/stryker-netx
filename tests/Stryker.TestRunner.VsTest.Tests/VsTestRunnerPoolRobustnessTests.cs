using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 182 (issue #297a, 360°-Analyse I-07): runner construction ran fire-and-forget
/// (<c>_ = Task.Run(Parallel.For(...))</c>) — construction exceptions (vstest.console
/// missing, broken deployment) were unobserved, the pool stayed empty and every consumer
/// waited forever on the runner-available handle. The pool must surface the failure.
/// </summary>
public class VsTestRunnerPoolRobustnessTests : VsTestMockingHelper
{
    [Fact]
    public async Task RunnerPool_WhenAllRunnerBuildsFail_FailsFastInsteadOfHangingForever()
    {
        // reuse the mocked VsTest context from a healthy pool build
        _ = BuildVsTestRunnerPool(new StrykerOptions(), out var healthyPool);
        using var pool = new VsTestRunnerPool(healthyPool.Context, NullLogger.Instance,
            (_, _) => throw new InvalidOperationException("vstest deployment broken"));

        var initialTest = Task.Run(() => pool.InitialTestAsync(Mock.Of<IProjectAndTests>()));
        var firstFinished = await Task.WhenAny(initialTest, Task.Delay(TimeSpan.FromSeconds(15)));

        firstFinished.Should().BeSameAs(initialTest,
            "the pool must fail fast when no runner can ever become available");
        initialTest.Status.Should().Be(TaskStatus.Faulted,
            "the initialization failure must reach the caller instead of an endless wait");
        initialTest.Exception!.InnerException.Should().BeOfType<GeneralStrykerException>(
            "the failure must carry diagnosis context");
    }
}
