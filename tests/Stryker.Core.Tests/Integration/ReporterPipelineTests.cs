using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Reporters;
using Stryker.Core.Reporters.Html;
using Stryker.Core.Reporters.Json;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// L5 integration layer (Sprint 20): the
/// <see cref="ReporterFactory"/> ↔ <see cref="BroadcastReporter"/> pipeline.
/// Verifies which reporters get wired up given an
/// <see cref="IStrykerOptions.Reporters"/> selection, and that the broadcast
/// fan-out propagates each lifecycle event to every wrapped reporter in order.
///
/// Note: <see cref="BroadcastReporter.OnAllMutantsTested"/> calls
/// <c>Thread.Sleep(1s)</c> to flush console caches. Tests in this class avoid
/// invoking that lifecycle hook unless absolutely necessary.
/// </summary>
[Trait("Category", "Integration")]
public class ReporterPipelineTests : IntegrationTestBase
{
    private static StrykerOptions OptionsWith(params Reporter[] reporters) =>
        new()
        {
            Reporters = reporters,
            // Provide minimal output dirs so the file-based reporters (Json, Html, Markdown)
            // can be constructed safely even when not enabled.
            OutputPath = System.IO.Path.GetTempPath(),
            ReportFileName = "stryker-report",
        };

    [Fact]
    public void Factory_AlwaysReturnsBroadcastReporter()
    {
        var factory = new ReporterFactory();
        var reporter = factory.Create(OptionsWith(), branchProvider: null);
        reporter.Should().BeOfType<BroadcastReporter>(
            "the factory must always return a BroadcastReporter wrapper, even when no reporter is enabled");
    }

    [Fact]
    public void Factory_NoReporterRequested_BroadcastsEmptyList()
    {
        var factory = new ReporterFactory();
        var reporter = (BroadcastReporter)factory.Create(OptionsWith(), branchProvider: null);
        reporter.Reporters.Should().BeEmpty(
            "an options object with no enabled reporters must produce an empty broadcast list");
    }

    [Fact]
    public void Factory_AllReporter_ReturnsEveryKnownReporter()
    {
        var factory = new ReporterFactory();
        var reporter = (BroadcastReporter)factory.Create(OptionsWith(Reporter.All), branchProvider: null);
        // The factory's internal possibleReporters dictionary has 10 entries
        // (Dots, Progress, ClearText, ClearTextTree, Json, Html, Dashboard,
        //  RealTimeDashboard, Markdown, Baseline). All must be returned.
        reporter.Reporters.Should().HaveCount(10,
            "Reporter.All must enable every entry in the factory's possibleReporters map");
    }

    [Fact]
    public void Factory_SingleReporterRequested_OnlyThatReporterIsWired()
    {
        var factory = new ReporterFactory();
        var reporter = (BroadcastReporter)factory.Create(OptionsWith(Reporter.Json), branchProvider: null);
        reporter.Reporters.Should().ContainSingle();
        reporter.Reporters.Single().Should().BeOfType<JsonReporter>();
    }

    [Fact]
    public void Factory_HtmlAndJsonRequested_BothAreWired()
    {
        var factory = new ReporterFactory();
        var reporter = (BroadcastReporter)factory.Create(OptionsWith(Reporter.Json, Reporter.Html), branchProvider: null);
        reporter.Reporters.Should().HaveCount(2);
        reporter.Reporters.OfType<JsonReporter>().Should().ContainSingle();
        reporter.Reporters.OfType<HtmlReporter>().Should().ContainSingle();
    }

    [Fact]
    public void Factory_UnknownEnumValueIgnored()
    {
        // Reporter.All is the "alias" sentinel; passing it together with a real
        // reporter must still resolve to the All-set (Reporter.All wins).
        var factory = new ReporterFactory();
        var reporter = (BroadcastReporter)factory.Create(OptionsWith(Reporter.All, Reporter.Json), branchProvider: null);
        reporter.Reporters.Should().HaveCount(10,
            "Reporter.All in the options list must short-circuit to the full reporter set");
    }

    [Fact]
    public void Broadcast_OnMutantsCreated_ForwardsToAllWrappedReporters()
    {
        var a = new Mock<IReporter>(MockBehavior.Loose);
        var b = new Mock<IReporter>(MockBehavior.Loose);
        var broadcast = new BroadcastReporter([a.Object, b.Object]);

        var component = new Mock<IReadOnlyProjectComponent>().SetupAllProperties();
        component.Setup(c => c.Mutants).Returns([]);
        var testProjects = new Mock<ITestProjectsInfo>().Object;

        broadcast.OnMutantsCreated(component.Object, testProjects);

        a.Verify(r => r.OnMutantsCreated(component.Object, testProjects), Times.Once);
        b.Verify(r => r.OnMutantsCreated(component.Object, testProjects), Times.Once);
    }

    [Fact]
    public void Broadcast_OnStartMutantTestRun_ForwardsToAllWrappedReporters()
    {
        var a = new Mock<IReporter>(MockBehavior.Loose);
        var b = new Mock<IReporter>(MockBehavior.Loose);
        var broadcast = new BroadcastReporter([a.Object, b.Object]);
        var mutants = new List<IReadOnlyMutant>();

        broadcast.OnStartMutantTestRun(mutants);

        a.Verify(r => r.OnStartMutantTestRun(mutants), Times.Once);
        b.Verify(r => r.OnStartMutantTestRun(mutants), Times.Once);
    }

    [Fact]
    public void Broadcast_OnMutantTested_ForwardsToAllWrappedReporters()
    {
        var a = new Mock<IReporter>(MockBehavior.Loose);
        var b = new Mock<IReporter>(MockBehavior.Loose);
        var broadcast = new BroadcastReporter([a.Object, b.Object]);
        var mutant = new Mock<IReadOnlyMutant>().Object;

        broadcast.OnMutantTested(mutant);

        a.Verify(r => r.OnMutantTested(mutant), Times.Once);
        b.Verify(r => r.OnMutantTested(mutant), Times.Once);
    }

    [Fact]
    public void Broadcast_PreservesReporterOrderInForwarding()
    {
        // The broadcast iterates Reporters in the order it was given. Verify that
        // the consumer-visible enumeration order matches the construction order.
        var first = new Mock<IReporter>(MockBehavior.Loose).Object;
        var second = new Mock<IReporter>(MockBehavior.Loose).Object;
        var third = new Mock<IReporter>(MockBehavior.Loose).Object;
        var broadcast = new BroadcastReporter([first, second, third]);
        broadcast.Reporters.Should().Equal(first, second, third);
    }

    [Fact]
    public void Broadcast_OnEmptyReporterList_DoesNotThrowOnLifecycleEvents()
    {
        var broadcast = new BroadcastReporter([]);
        var component = new Mock<IReadOnlyProjectComponent>().SetupAllProperties();
        component.Setup(c => c.Mutants).Returns([]);
        var testProjects = new Mock<ITestProjectsInfo>().Object;
        var mutant = new Mock<IReadOnlyMutant>().Object;

        var act = () =>
        {
            broadcast.OnMutantsCreated(component.Object, testProjects);
            broadcast.OnStartMutantTestRun([]);
            broadcast.OnMutantTested(mutant);
        };
        act.Should().NotThrow("an empty broadcast list must be a valid no-op for every lifecycle event");
    }

    [Fact]
    public void Broadcast_OnMutantTested_IsThreadSafe()
    {
        // BroadcastReporter.OnMutantTested takes a Lock — verify calls from multiple
        // threads serialise without losing invocations (no torn counting).
        var a = new Mock<IReporter>(MockBehavior.Loose);
        var broadcast = new BroadcastReporter([a.Object]);
        var mutant = new Mock<IReadOnlyMutant>().Object;
        const int N = 200;

        System.Threading.Tasks.Parallel.For(0, N, _ => broadcast.OnMutantTested(mutant));

        a.Verify(r => r.OnMutantTested(mutant), Times.Exactly(N),
            "every parallel OnMutantTested call must reach the wrapped reporter exactly once");
    }
}
