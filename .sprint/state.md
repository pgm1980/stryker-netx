---
current_sprint: "15"
sprint_goal: "v2.2.0: walk back ADR-016 (HotSwap engine) — recherche surfaced wrong mental model in v2.0.0 architecture; cleanup HotSwap surface, ADR-021 + ADR-022, soft deprecation. No new mutators."
branch: "feature/15-v2.2-hotswap-walkback"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 15 — v2.2.0 HotSwap walk-back

**GitHub-Issue:** [#15](https://github.com/pgm1980/stryker-netx/issues/15)
**Base-Tag:** `v2.1.0` (Sprint 14 closed)
**Final-Tag:** `v2.2.0`
**Reference:** Sprint 15 Maxential session (14 thoughts, 3-way branch C2 chosen). Recherche revealed `CsharpMutationProcess.CompileMutations` does single-pass compile of all mutations — ADR-016's "5–10× perf boost" assumption was wrong.

## Implementation order

- [x] **15.1** — Write ADR-021 (walk back ADR-016, full recherche trail, YAGNI rationale)
- [x] **15.2** — Write ADR-022 (incremental mutation testing as future direction, Status: Proposed)
- [x] **15.3** — `[Obsolete]` annotations on `MutationEngine` enum, `IMutationEngine` interface, `IStrykerOptions.MutationEngine` property, `MutationEngineInput` class
- [x] **15.4** — Delete `HotSwapEngine.cs` + `RecompileEngine.cs`
- [x] **15.5** — CLI shim: `--engine recompile|hotswap` accepted with deprecation warning
- [x] **15.6** — Build/Test/E2E/Semgrep green
- [x] **15.7** — README "Mutation Engines" section rewrite + Sprint history table
- [x] **15.8** — MIGRATION "Documented removals (v2.2)" section
- [x] **15.9** — Lessons doc + commit + tag `v2.2.0` + release + merge

## Sprint-15-DoD

- [x] ADR-021 written
- [x] ADR-022 written (Proposed)
- [x] Engines deleted
- [x] `[Obsolete]` annotations applied
- [x] CLI deprecation warning
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100%
- [x] Semgrep clean
- [x] README + MIGRATION updated
- [x] Lessons doc
- [x] Tag `v2.2.0`
- [x] GitHub release
- [x] Branch merged into main
- [x] Issue #15 closed
- [x] housekeeping_done=true
