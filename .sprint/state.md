---
current_sprint: "21"
sprint_goal: "Automated E2E in CI: Stryker.E2E.Tests Projekt mit ~10 Subprozess-basierten Tests gegen Sample.slnx → v2.8.0"
branch: "feature/21-automated-e2e"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 21 — Automated E2E in CI

**GitHub-Issue:** [#22](https://github.com/pgm1980/stryker-netx/issues/22)
**Base-Tag:** `v2.7.0` (Sprint 20 closed)
**Final-Tag:** `v2.8.0`
**Reference:** Sprint 21 Maxential 15 thoughts (1 branch project-location + 1 revision build-strategy) + ToT 13-candidate ranking → 10 tests selected (4 fast + 6 cached-slow).

## Architecture decisions

- D1-A: separate `tests/Stryker.E2E.Tests/` project
- D2-D: Hybrid build-fixture + direct-binary spawn (`dotnet exec Stryker.CLI.dll`)
- D3: `[Collection("E2E-Sequential")]` + assembly `DisableTestParallelization=true`
- D4: Minimal `MutationReport` record (System.Text.Json) for report parsing
- D5: `StrykerRunCacheFixture` (3 profile-runs cached, shared across multiple Facts)
