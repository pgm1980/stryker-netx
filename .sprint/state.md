---
current_sprint: "70"
sprint_goal: "Options batch F (5 verified-unported Inputs, 26 green) → v2.56.0"
branch: "feature/70-options-batch-f"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 70 — Options batch F (26 grün, verified-unported)

## Outcome
- OpenReportInputTests (4 facts) ported
- LanguageVersionInputTests (4 facts) ported
- MutationLevelInputTests (3 facts + 1 theory ×2 = 5 facts) ported
- OptimizationModeInputTests (1 fact + 1 theory ×5 + 1 fact = 7 facts) ported
- MsBuildPathInputTests (2 facts + 2 theories ×2/×2 = 6 facts) ported
- Total: 26 green, 0 skip
- Dogfood-project: 553 + 14 skip = 567
- 1 build-fix-cycle (CA2263 generic Enum.GetValues<T>() preferred over typeof(T))
