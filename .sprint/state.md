---
current_sprint: "47"
sprint_goal: "Core.Mutators batch A (14 smallest, ~1013 LOC) → v2.34.0"
branch: "feature/47-core-mutators-batch-a"
started_at: "2026-05-02"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Sprint 47 — 83 new grün + 2 skip; Mutators batch A done

## Outcome
- 14 smallest mutator test files ported
- Dogfood-project total: 115 grün + 2 skip = 117
- Solution-wide: 931 grün ohne E2E
- 1 build-fix-cycle
- Production drift: IMutator.Mutate now requires IStrykerOptions (added 3rd param)
