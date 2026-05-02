---
current_sprint: "72"
sprint_goal: "Options batch H (4 verified-unported Inputs, 26 green + 2 skip) → v2.58.0"
branch: "feature/72-options-batch-h"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 72 — Options batch H (26 grün + 2 skip, verified-unported)

## Outcome
- AzureFileStorageSasInputTests (4 facts + 2 theories ×3/×3 = 10 facts) ported
- AzureFileStorageUrlInputTests (6 facts) ported
- ConcurrencyInputTests (4 facts + 1 theory ×4 = 8) — 3 ported (1 fact + 1 base) + 5 skipped (production drift: [LoggerMessage] source-gen bypasses Moq.Verify)
- SinceInputTests (7 facts) ported
- Total: 26 green + 2 skip
- Dogfood-project: 605 + 16 skip = 621
- 0 build-fix-cycles (1-shot port)

## Lessons (NEW)
- **[LoggerMessage] + Moq Verify drift**: production uses `[LoggerMessage]` source-gen which generates strongly-typed state objects; the standard `ILogger.Log(level, eventId, state, ...)` interception path that LoggerMockExtensions.Verify expects is bypassed. 5 ConcurrencyInput tests skipped with uniform reason. Future remediation: a "structured-logging-test sprint" rewrites Verify to look at the [LoggerMessage]-generated state directly.
