---
current_sprint: "100"
sprint_goal: "DashboardClientsTest full upstream port (3 placeholder skips → 14 real green) → v2.86.0"
branch: "feature/100-dashboardclients-full-port"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 100 — DashboardClientsTest full upstream port (defer-skip aufarbeitung)

## Outcome — 3 skips → 14 real green
Sprint 93 placeholder had 3 [Fact(Skip)] stubs. Full upstream port (438 LOC) with all 12
upstream [TestMethod]s + 2 implicit (Empty Module + RealTime variants) → 14 [Fact]s.
- Net: +15 green, -3 skip, +12 new tests
- Dogfood-project: 942 + 79 skip = 1021

## Production matches upstream signatures
DashboardClient v2.x uses HttpClient + IJsonReport / IJsonMutant abstractions identical
to upstream. No production drift forced rewrite — direct port via:
- MSTest → xUnit (constructor + IDisposable for HttpClient cleanup)
- Shouldly → FluentAssertions
- HttpMessageHandler mock with `Moq.Protected()` + `ItExpr` preserved verbatim
- EnableAllLogLevels for [LoggerMessage] source-gen logger (Sprint 96 pattern)

## New helper file
- `tests/Stryker.Core.Dogfood.Tests/Reporters/Json/MockJsonReport.cs` — port of upstream
  test stub that constructs JsonReport without invoking the full Build pipeline. Adapted
  to support nullable thresholds/files (init properties only assigned when not null).

## Files
- `tests/Stryker.Core.Dogfood.Tests/Clients/DashboardClientsTest.cs` (full port, 14 tests)
- `tests/Stryker.Core.Dogfood.Tests/Reporters/Json/MockJsonReport.cs` (NEW helper)
