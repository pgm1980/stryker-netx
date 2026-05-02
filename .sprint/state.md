---
current_sprint: "77"
sprint_goal: "MutantFilters batch C (IgnoreBlock + ExcludeFromCodeCoverage, 12 green) → v2.63.0"
branch: "feature/77-mutantfilters-batch-c"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 77 — MutantFilters batch C (12 grün, verified-unported)

## Outcome
- IgnoreBlockMutantFilterTests (4 facts) — Block mutant detection in source
- ExcludeFromCodeCoverageFilterTests (4 facts + 1 theory ×3 = 7 facts) — [ExcludeFromCodeCoverage] attribute matching on method/property/class
- Total: 12 green, 0 skip
- Dogfood-project: 672 + 16 skip = 688
- 1 build-fix-cycle (CA1859 file-level pragma — upstream tests deliberately exercise IMutantFilter contract)
- VsTestHelperTests skipped: belongs in Stryker.TestRunner.VsTest.Tests project (production class lives in Stryker.TestRunner.VsTest assembly), not Dogfood.

## Lessons (NEW)
- **CA1859 file-level pragma for interface-contract tests**: upstream tests using `IMutantFilter target = new ConcreteFilter()` deliberately verify the interface contract. CA1859 demands concrete type for performance — but that defeats the test's purpose. File-level `#pragma warning disable CA1859` ... `#pragma warning restore CA1859` is the right fix.
