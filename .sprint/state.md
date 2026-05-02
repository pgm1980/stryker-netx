---
current_sprint: "73"
sprint_goal: "Options batch I (DashboardApiKey + BaselineProvider Inputs, 20 green) → v2.59.0"
branch: "feature/73-options-batch-i"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 73 — Options batch I (20 grün, verified-unported)

## Outcome
- DashboardApiKeyInputTests (5 facts) — env var get/set/restore pattern
- BaselineProviderInputTests (3 facts + 6 theories ×2/×2/×2/×2/×2/×2 + 2 MemberData ×2 = 15) ported
- Total: 20 green, 0 skip
- Dogfood-project: 625 + 16 skip = 641
- 1 build-fix-cycle (CS8625 SuppliedInput=null → null! + CA1825/MA0005 new Reporter[] {} → Array.Empty<Reporter>())
- ReportOpenerTests skipped — duplicate of OpenReportInputTests (Sprint 70)

## Lessons
- **MemberData for IEnumerable<T> theory data**: xUnit [Theory] [InlineData(new Reporter[] { Reporter.Dashboard })] errors with CA1825 — use `[MemberData(nameof(...))]` + `IEnumerable<object[]>` returning `[System.Array.Empty<Reporter>(), ...]` instead
