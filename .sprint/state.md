---
current_sprint: "80"
sprint_goal: "Block A: TestProjectTests + InjectedHelperTests (26 green) → v2.66.0"
branch: "feature/80-projectcomponents-testproject-injected"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 80 — TestProjectTests + InjectedHelperTests (26 grün)

## Outcome
- TestProjectTests (3 facts) — TestProject equality + preprocessor symbols via DefineConstants
- InjectedHelperTests (1 theory ×14 + 1 theory ×9 = 23 facts) — CodeInjection helpers compile across CSharp2..14, Default/Latest/LatestMajor/Preview, with/without nullable
- 3 TestResources (ExampleTestFileA/B/PreprocessorSymbols) added
- Total: 26 green
- Dogfood-project: 707 + 17 skip = 724
- 1 build-fix-cycle (S2971 Where().Count() → Count(predicate); CA1305 GetMessage() → GetMessage(InvariantCulture))
