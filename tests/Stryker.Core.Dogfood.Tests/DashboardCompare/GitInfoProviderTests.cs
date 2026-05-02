using Xunit;

namespace Stryker.Core.Dogfood.Tests.DashboardCompare;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. GitInfoProvider tests require LibGit2Sharp
/// integration with real-or-mocked git repository (475 LOC). Defer to dedicated git-integration
/// deep-port sprint.</summary>
public class GitInfoProviderTests
{
    [Fact(Skip = "Requires LibGit2Sharp + real-or-mocked git repository — defer to git-integration deep-port sprint.")]
    public void GetCurrentBranchName_ShouldReturnBranch() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void GetTargetCommit_ShouldResolveBranchName() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void GetCurrentRepoPath_ShouldFindGitDir() { /* placeholder */ }
}
