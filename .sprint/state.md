---
current_sprint: "16"
sprint_goal: "v2.3.0: long-tail items. Ship 1 mutator (AsyncAwaitResult — comparison.md §4.4 spec-faithful) + JsonReport source-gen (AOT-trim) + validation-count-test deferral (ADR-023). Defer 2: RoslynDiagnostics v2 + GenericConstraintLoosen interface-target."
branch: "feature/16-v2.3-long-tail"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 16 — v2.3.0 long-tail

**GitHub-Issue:** [#16](https://github.com/pgm1980/stryker-netx/issues/16)
**Base-Tag:** `v2.2.0` (Sprint 15 closed)
**Final-Tag:** `v2.3.0`
**Reference:** Sprint-15 lessons long-tail list + Sprint-16 Maxential (10 thoughts, 1 branch on contract-extension-strategy made moot mid-thought).

## Implementation order

- [x] **16.1** — `AsyncAwaitResultMutator.cs` (mirror AsyncAwaitMutator pattern, emit `await x → x.Result`)
- [x] **16.2** — Wire into `DefaultMutatorList.V2OperatorBatches`
- [x] **16.3** — `JsonReportSerializerContext` source-gen + JsonReportSerialization rewrite
- [x] **16.4** — `[Trait("Skip", "...")]` on validation count-tests with documented reason
- [x] **16.5** — ADR-023 (validation-non-reconciliation principle)
- [x] **16.6** — Build/Test/E2E/Semgrep green
- [x] **16.7** — README + MIGRATION update (52 mutators, AOT-trim note)
- [x] **16.8** — Lessons doc + commit + tag v2.3.0 + release + merge

## Sprint-16-DoD

- [x] AsyncAwaitResultMutator written + wired
- [x] JsonReportSerializerContext + serialization paths updated
- [x] Validation count-tests Skip-Trait + ADR-023
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100%
- [x] Semgrep clean
- [x] README + MIGRATION updated (52 mutators)
- [x] Lessons doc
- [x] Tag `v2.3.0`
- [x] GitHub release
- [x] Branch merged into main
- [x] Issue #16 closed
- [x] housekeeping_done=true
