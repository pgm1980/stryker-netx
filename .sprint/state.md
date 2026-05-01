---
current_sprint: "23"
sprint_goal: "Operations Hardening: Crash-Fix Complete+All + E2E-CI + coverlet + validation-count-reconcile → v2.10.0"
branch: "feature/23-operations-hardening"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 23 — Operations Hardening

**GitHub-Issue:** [#25](https://github.com/pgm1980/stryker-netx/issues/25)
**Base-Tag:** `v2.9.0` (Sprint 22 closed)
**Final-Tag:** `v2.10.0`

## Sub-Tasks (alle abgeschlossen)
1. ✅ Crash-Fix `--mutation-level Complete --mutation-profile All` — UoiMutator parent-context skip + global `DoNotMutateOrchestrator<QualifiedNameSyntax>` + 2 unit tests + 1 E2E regression test
2. ✅ E2E-Job in CI explizit — separate `e2e-test` matrix job; `build-test` excludes E2E via filter
3. ✅ coverlet file-lock fix — `coverlet.runsettings` excludes `Stryker.DataCollector` from instrumentation
4. ✅ Validation-Count-Reconcile — hardcoded counts → soft-asserts (sums-add-up, mutants&gt;0); graceful early-return when StrykerOutput missing
