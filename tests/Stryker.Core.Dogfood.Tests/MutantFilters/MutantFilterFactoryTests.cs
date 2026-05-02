using System;
using System.Linq;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Baseline;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.DiffProviders;
using Stryker.Core.MutantFilters;
using Stryker.Core.Reporters.Json;
using Stryker.TestHelpers;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 86 (v2.72.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MutantFilterFactoryTests : TestBase
{
    [Fact]
    public void MutantFilterFactory_Creates_of_type_BroadcastFilter()
    {
        var options = new StrykerOptions { Since = true };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);
        var branchProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProvider = new Mock<IBaselineProvider>(MockBehavior.Loose);

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProvider.Object, branchProviderMock.Object);

        result.Should().BeOfType<BroadcastMutantFilter>();
    }

    [Fact]
    public void Create_Throws_ArgumentNullException_When_Stryker_Options_Is_Null()
    {
        var act = () => MutantFilterFactory.Create(null!, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MutantFilterFactory_Creates_Standard_Mutant_Filters()
    {
        var options = new StrykerOptions { Since = false };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);
        var branchProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProvider = new Mock<IBaselineProvider>(MockBehavior.Loose);

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProvider.Object, branchProviderMock.Object);

        var resultAsBroadcastFilter = result as BroadcastMutantFilter;
        resultAsBroadcastFilter!.MutantFilters.Count().Should().Be(5);
    }

    [Fact]
    public void MutantFilterFactory_Creates_DiffMutantFilter_When_Since_Enabled()
    {
        var options = new StrykerOptions { Since = true };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);
        var branchProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProvider = new Mock<IBaselineProvider>(MockBehavior.Loose);

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProvider.Object, branchProviderMock.Object);

        var resultAsBroadcastFilter = result as BroadcastMutantFilter;
        resultAsBroadcastFilter!.MutantFilters.Count().Should().Be(6);
        resultAsBroadcastFilter.MutantFilters.Count(x => x.GetType() == typeof(SinceMutantFilter)).Should().Be(1);
    }

    [Fact]
    public void MutantFilterFactory_Creates_ExcludeLinqExpressionFilter_When_ExcludedLinqExpressions_IsNotEmpty()
    {
        var options = new StrykerOptions
        {
            ExcludedLinqExpressions = [LinqExpression.Any],
        };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);
        var branchProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProvider = new Mock<IBaselineProvider>(MockBehavior.Loose);

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProvider.Object, branchProviderMock.Object);

        var resultAsBroadcastFilter = result.Should().BeOfType<BroadcastMutantFilter>().Subject;
        resultAsBroadcastFilter.MutantFilters.Count().Should().Be(6);
        resultAsBroadcastFilter.MutantFilters.Count(x => x.GetType() == typeof(ExcludeLinqExpressionFilter)).Should().Be(1);
    }

    [Fact]
    public void MutantFilterFactory_Creates_DashboardMutantFilter_And_DiffMutantFilter_WithBaseline_Enabled()
    {
        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "foo" };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);
        var gitInfoProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProviderMock = new Mock<IBaselineProvider>(MockBehavior.Loose);

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProviderMock.Object, gitInfoProviderMock.Object);

        var resultAsBroadcastFilter = result as BroadcastMutantFilter;
        resultAsBroadcastFilter!.MutantFilters.Count().Should().Be(7);
        resultAsBroadcastFilter.MutantFilters.Count(x => x.GetType() == typeof(BaselineMutantFilter)).Should().Be(1);
        resultAsBroadcastFilter.MutantFilters.Count(x => x.GetType() == typeof(SinceMutantFilter)).Should().Be(1);
    }

    [Fact]
    public void MutantFilterFactory_Creates_BlockMutantFilter_Last()
    {
        var options = new StrykerOptions
        {
            WithBaseline = true,
            ExcludedLinqExpressions = [LinqExpression.Distinct],
        };
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Strict);
        var gitInfoProviderMock = new Mock<IGitInfoProvider>(MockBehavior.Strict);
        var baselineProviderMock = new Mock<IBaselineProvider>(MockBehavior.Strict);
        const string Branch = "branch";
        gitInfoProviderMock.Setup(m => m.GetCurrentBranchName()).Returns(Branch);
        baselineProviderMock.Setup(m => m.Load($"baseline/{Branch}")).ReturnsAsync(new JsonReport());
        diffProviderMock.Setup(m => m.ScanDiff()).Returns(new DiffResult());
        diffProviderMock.Setup(m => m.Tests).Returns(new TestSet());

        var result = MutantFilterFactory.Create(options, null!, diffProviderMock.Object, baselineProviderMock.Object, gitInfoProviderMock.Object);
        var broadcastFilterResult = result as BroadcastMutantFilter;

        broadcastFilterResult!.MutantFilters.Last().Should().BeOfType<IgnoreBlockMutantFilter>();
    }
}
