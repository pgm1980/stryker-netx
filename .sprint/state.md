---
current_sprint: "17"
sprint_goal: "v2.4.0: final long-tail rest. Ship RoslynSemanticDiagnosticsFilter (speculative-binding API) + GenericConstraintLoosen interface-pair extension. Defer JsonReport full AOT-trim to v3.0 per ADR-024."
branch: "feature/17-v2.4-final-long-tail"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 17 — v2.4.0 final long-tail rest

**GitHub-Issue:** [#17](https://github.com/pgm1980/stryker-netx/issues/17)
**Base-Tag:** `v2.3.0` (Sprint 16 closed)
**Final-Tag:** `v2.4.0`
**Reference:** Sprint 16 deferred-list + Sprint 17 Maxential (11 thoughts, 1 branch jsonreport-aot-scope, E3 chosen).

## Implementation order

- [x] **17.1** — `RoslynSemanticDiagnosticsFilter` using SemanticModel.GetSpeculativeSymbolInfo
- [x] **17.2** — Wire into `EquivalentMutantFilterPipeline.Default`
- [x] **17.3** — `GenericConstraintLoosenMutator` extension with BCL-interface-pair table
- [x] **17.4** — ADR-024 (JsonReport full AOT-trim → v3.0 deferral)
- [x] **17.5** — Build/Test/E2E/Semgrep green
- [x] **17.6** — README + MIGRATION update (5 filters, ADR-024 link)
- [x] **17.7** — Lessons doc + commit + tag v2.4.0 + release + merge

## Sprint-17-DoD

- [x] RoslynSemanticDiagnosticsFilter written + wired
- [x] GenericConstraintLoosenMutator extended with BCL pair table
- [x] ADR-024 written
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100%
- [x] Semgrep clean
- [x] README + MIGRATION updated (5 filters)
- [x] Lessons doc
- [x] Tag `v2.4.0`
- [x] GitHub release
- [x] Branch merged into main
- [x] Issue #17 closed
- [x] housekeeping_done=true
