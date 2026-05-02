using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. SinceMutantFilter tests use IDiffProvider +
/// IGitInfoProvider mock chain with rich path-glob matrix (339 LOC). Defer to dedicated
/// since-feature deep-port sprint.</summary>
public class SinceMutantFilterTests
{
    [Fact(Skip = "Heavy IDiffProvider + IGitInfoProvider mock chain + path-glob matrix — defer.")]
    public void ShouldFilterUnchangedMutants() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldKeepMutantsInChangedFiles() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldHandleAddedFiles() { /* placeholder */ }
}
