# Sprint 31 — MTP DefaultRunnerFactory + ResponseListener Tests: Lessons

**Sprint:** 31 (2026-05-02)
**Branch:** `feature/31-mtp-factory-listener-port`
**Base:** v2.17.0
**Final Tag:** `v2.18.0`

## Outcome
- DefaultRunnerFactoryTests: 5/5 grün
- ResponseListenerTests: 6/6 grün
- 11 new tests, MTP-project total now 13 grün
- Solution-wide: 510 tests grün excl E2E
- 1-shot port (no build-fix-cycle needed) — predicted by accumulating mechanical-fix-knowledge

## Lessons
- **`new object()` discoveryLock pattern** in 5 spots — alle ersetzt durch `new Lock()` (Sprint 2 baseline).
- **`StringComparer.Ordinal` für Dictionary-Constructors** — alle 5 instances expanded.
- **Reflection target `ResponseListener.Complete()` private method** — funktioniert direkt im Port (kein API-drift).
- **`using var cts = new CancellationTokenSource()`** statt manueller `cts.Dispose()` (CA2000).

## Roadmap unchanged: Sprint 32 = MicrosoftTestPlatformRunnerPoolTests (319 LOC).
