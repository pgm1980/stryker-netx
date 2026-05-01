---
current_sprint: "14"
sprint_goal: "v2.1.0: 3 new mutators (ConstantReplacement, GenericConstraintLoosen, SpanReadOnlySpanDeclaration) + 1 new equivalence filter (RoslynDiagnostics) + doc clarification on already-implemented coverage-driven skip; HotSwap deferred to v2.2.0"
branch: "feature/14-v2.1-filter-pipeline"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 14 — v2.1.0 Filter pipeline + operator completion

**GitHub-Issue:** [#14](https://github.com/pgm1980/stryker-netx/issues/14)
**Base-Tag:** `v2.0.1` (Sprint 13 closed)
**Final-Tag:** `v2.1.0`
**Reference inputs:** `_input/mutation_framework_comparison.md` §4.1 (CRCR) + §4.2 + §4.3 (mutmut Roslyn-diag) + §4.4 (Span/Memory) + Sprint 13 lessons + Sprint-14 Maxential session (15 thoughts, B2 branch chosen)

## Implementation order (ascending in risk)

- [x] **14.1** — `ConstantReplacementMutator` (PIT CRCR `c → 0, 1, -1, -c`) — Stronger | All
- [x] **14.2** — `GenericConstraintLoosenMutator` (per-clause loosening) — Stronger | All
- [x] **14.3** — `SpanReadOnlySpanDeclarationMutator` (declaration-site swap) — All only
- [x] **14.4** — `RoslynDiagnosticsEquivalenceFilter` (new IEquivalentMutantFilter, syntax-error pre-filter)
- [x] **14.5** — Wire 3 mutators into `DefaultMutatorList.V2OperatorBatches`
- [x] **14.6** — Wire filter into `EquivalentMutantFilterPipeline.Default`
- [x] **14.7** — README + MIGRATION update (51-mutator state, coverage-already-impl note, v2.2 HotSwap roadmap)
- [x] **14.8** — ADR-019 (v2.2 HotSwap focused-release decision)
- [x] **14.9** — Build/Test/E2E/Semgrep green
- [x] **14.10** — Sprint-close + lessons + tag `v2.1.0` + GitHub release + merge to main

## Sprint-14-DoD

- [x] 3 new mutator files
- [x] 1 new filter file
- [x] All 3 mutators carry `[MutationProfileMembership]`
- [x] All wired into `DefaultMutatorList`
- [x] Filter wired into `EquivalentMutantFilterPipeline.Default`
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100% under default profile
- [x] Semgrep clean
- [x] README + MIGRATION updated (26 + 17 + 8 = 51)
- [x] ADR-019 v2.2 HotSwap deferral
- [x] Lessons doc
- [x] Tag `v2.1.0`
- [x] GitHub release published
- [x] Branch merged into main
- [x] Issue #14 closed
- [x] housekeeping_done=true
