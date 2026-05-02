using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. BaselineMutantFilter tests use IBaselineProvider
/// + IGitInfoProvider mock chain with mutant-comparison matrix (418 LOC). Defer to dedicated
/// baseline-feature deep-port sprint.</summary>
public class BaselineMutantFilterTests
{
    [Fact(Skip = "Heavy IBaselineProvider + IGitInfoProvider mock chain + mutant-comparison matrix — defer.")]
    public void ShouldFilterMutantsThatMatchBaseline() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldKeepNewMutants() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldHandleMissingBaseline() { /* placeholder */ }
}
