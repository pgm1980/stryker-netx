using System;
using System.Linq;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Reporters;
using Stryker.Core.Reporters.Html;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Progress;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 56 (v2.42.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ReporterFactoryTests : TestBase
{
    private readonly Mock<IGitInfoProvider> _branchProviderMock = new(MockBehavior.Loose);

    [Theory]
    [InlineData(Reporter.Json, typeof(JsonReporter))]
    [InlineData(Reporter.Html, typeof(HtmlReporter))]
    [InlineData(Reporter.Progress, typeof(ProgressReporter))]
    [InlineData(Reporter.Dots, typeof(ConsoleDotProgressReporter))]
    [InlineData(Reporter.ClearText, typeof(ClearTextReporter))]
    public void ReporterFactory_CreatesRequestedReporters(Reporter option, Type reporter)
    {
        var target = new ReporterFactory();
        var options = new StrykerOptions { Reporters = [option] };

        var result = target.Create(options, _branchProviderMock.Object);
        var broadcastReporter = result.Should().BeOfType<BroadcastReporter>().Which;
        broadcastReporter.Reporters.Should().ContainSingle().Which.Should().BeOfType(reporter);
    }

    [Fact]
    public void ReporterFactory_CreatesAllReporters()
    {
        var target = new ReporterFactory();
        var options = new StrykerOptions { Reporters = [Reporter.All] };

        var result = (BroadcastReporter)target.Create(options, _branchProviderMock.Object);

        var broadcastReporter = result.Should().BeOfType<BroadcastReporter>().Which;
        broadcastReporter.Reporters.Should().Contain(r => r is JsonReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is ConsoleDotProgressReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is ClearTextReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is ClearTextTreeReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is ProgressReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is DashboardReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is MarkdownSummaryReporter);
        broadcastReporter.Reporters.Should().Contain(r => r is BaselineReporter);

        result.Reporters.Count().Should().Be(10);
    }
}
