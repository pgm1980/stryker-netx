---
current_sprint: "97"
sprint_goal: "defer-skip aufarbeitung — 9 skips → 9 real (StatusReporter+SseEvent+SourceProjectName+ArrayCreationMutator) → v2.83.0"
branch: "feature/97-defer-skip-aufarbeitung-batch1"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: true
documentation_updated: false
---
# Sprint 97 — defer-skip aufarbeitung batch 1

## Outcome — 9 skips → 9 real
- StatusReporterTests (3) — same EnableAllLogLevels root-cause as Sprint 96
- SseEventTest (3) — SSE-spec newline wrappers, test expectations adapted
- SourceProjectNameInputTests (1) — HelpText 2-space drift, test expectation adapted
- ArrayCreationMutatorTests (2) — IMutator.Mutate requires non-null IStrykerOptions, fixed via static DefaultOptions
- Net: +9 green, -9 skip
- Dogfood-project: 925 + 84 skip = 1009

## New helper
`Mock<ILogger<T>>.VerifyNoOtherLogCalls()` — variant of VerifyNoOtherCalls() that first marks
IsEnabled invocations as verified (infrastructure noise from [LoggerMessage] source-gen guards).
Use whenever a test calls EnableAllLogLevels AND VerifyNoOtherCalls.

## Files changed
- `tests/Stryker.TestHelpers/LoggerMockExtensions.cs` (new VerifyNoOtherLogCalls)
- `tests/Stryker.Core.Dogfood.Tests/Reporters/StatusReporterTests.cs` (3 unskipped + ctor EnableAll)
- `tests/Stryker.Core.Dogfood.Tests/Reporters/Html/RealTime/Events/SseEventTest.cs` (3 unskipped + adapted strings)
- `tests/Stryker.Core.Dogfood.Tests/Options/Inputs/SourceProjectNameInputTests.cs` (1 unskipped + adapted string)
- `tests/Stryker.Core.Dogfood.Tests/Mutators/ArrayCreationMutatorTests.cs` (2 unskipped + DefaultOptions)
