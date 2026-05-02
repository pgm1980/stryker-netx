---
current_sprint: "102"
sprint_goal: "GitInfoProviderTests full upstream port (3 placeholder skips → 13 real green) → v2.88.0"
branch: "feature/102-gitinfoprovider-full-port"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 102 — GitInfoProviderTests full upstream port

## Outcome — 3 skips → 13 real green
Sprint 93 placeholder (3 [Fact(Skip)]) replaced with full upstream port (475 LOC).
- Net: +13 green, -3 skip, +10 new tests
- Dogfood-project: 962 + 73 skip = 1035

## Production matches upstream (LibGit2Sharp IRepository) → pure port
- MSTest → xUnit, Shouldly → FluentAssertions
- Mock&lt;IRepository&gt; + Mock&lt;BranchCollection&gt; + Mock&lt;Branch&gt; pattern preserved 1:1
- IDE0028 file-level pragma (List initializer cast to IEnumerable for GetEnumerator —
  collection-expression breaks target-type inference on intermediate cast)
- New lesson: FluentAssertions `.Should().Be(commitMock.Object)` does deep equality on
  Castle proxies and considers two identical-looking proxy instances different. Use
  `.Should().BeSameAs(commitMock.Object)` for reference equality (replaces Shouldly's
  reference-comparing `ShouldBe(mockObject)`).

## Files
- `tests/Stryker.Core.Dogfood.Tests/DashboardCompare/GitInfoProviderTests.cs` (full port, 13 tests)
