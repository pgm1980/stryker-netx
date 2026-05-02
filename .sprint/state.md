---
current_sprint: "96"
sprint_goal: "ConcurrencyInput (2) + S3BaselineProvider (4) → 6 skips → 6 real via EnableAllLogLevels helper → v2.82.0"
branch: "feature/96-concurrencyinput-loggermessage-helper"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: true
documentation_updated: false
---
# Sprint 96 — LoggerMessage IsEnabled root-cause fix (defer-skip aufarbeitung)

## Outcome — 6 skips → 6 real
- ConcurrencyInputTests: 2 skips → 2 green (WhenGivenValueIs..., WhenGiven1ShouldPrintWarning)
- S3BaselineProviderTests: 4 skips → 4 green (Load_Returns_Null_When_Object_Not_Found, Load_Returns_Null_On_S3_Error, Save_Uploads_Report, Save_Logs_Error_On_Failure)
- Net: +6 green, -6 skip
- Dogfood-project: 916 + 93 skip = 1009

## Critical root-cause discovery
The "[LoggerMessage] source-gen drift (Sprint 72 lesson)" diagnosis was WRONG.
Real cause: `Mock<ILogger<T>>.IsEnabled` returns `false` by default → production guard
`if (logger.IsEnabled(LogLevel.X))` (CA1873) skips the Log call entirely → Verify finds nothing.

Fix: new `EnableAllLogLevels<T>(this Mock<ILogger<T>>)` helper in
`tests/Stryker.TestHelpers/LoggerMockExtensions.cs` — call once per logger mock before any
Verify(...) on log statements that are IsEnabled-guarded in production.

Also generalized `Verify` helper's `MatchesRenderedMessage` to support both classical
structured-logging state AND [LoggerMessage] source-gen value-type structs (both have
ToString() returning the formatted message — uniform match).

## Files changed
- `tests/Stryker.TestHelpers/LoggerMockExtensions.cs` (new EnableAllLogLevels + generalized Verify)
- `tests/Stryker.Core.Dogfood.Tests/Options/Inputs/ConcurrencyInputTests.cs` (2 unskipped)
- `tests/Stryker.Core.Dogfood.Tests/Baseline/Providers/S3BaselineProviderTests.cs` (4 unskipped)
