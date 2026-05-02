---
current_sprint: "65"
sprint_goal: "Reporters batch D (DashboardReporter, 10 green) → v2.51.0"
branch: "feature/65-reporters-batch-d"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 65 — Reporters batch D DashboardReporter (10 grün)

## Outcome
- DashboardReporterTests (8 facts + 1 theory ×2 = 10) ported
- Dogfood-project: 466 + 14 skip = 480
- Solution-wide: 1282 + 32 skip ohne E2E
- 1 build-fix-cycle (TestBase inheritance for ApplicationLogging.LoggerFactory seeding — Sprint 49 lesson reapplied)
- Production drift: `JsonReport` upstream → `IJsonReport` ours; `Mock<IDashboardClient>` setup uses `IJsonReport`

## Lessons (NEW)
- Reporter tests using DashboardReporter / RegexMutator / FilteredMutantsLogger / etc. **MUST inherit TestBase** because their ctors call `ApplicationLogging.LoggerFactory.CreateLogger<T>()` (Sprint 49 lesson reaffirmed)
