using Xunit;

namespace Stryker.Core.Dogfood.Tests.DiffProviders;

/// <summary>Sprint 94 (v2.80.0) defer-doc placeholder. GitDiffProvider (691 LOC upstream) requires
/// LibGit2Sharp + real-or-mocked git repository with branch/commit fixtures. Defer to dedicated
/// git-integration deep-port sprint.</summary>
public class GitDiffProviderTests
{
    [Fact(Skip = "691 LOC + LibGit2Sharp integration — defer to git-integration deep-port sprint.")]
    public void ScanDiff_ShouldReturnChangedFiles() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ScanDiff_ShouldHandleAddedFiles() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ScanDiff_ShouldHandleDeletedFiles() { /* placeholder */ }
}
