---
current_sprint: "11"
sprint_goal: "PIT-2 + cargo-mutants Operator Batch: ConstructorNullMutator + MatchGuardMutator + WithExpressionMutator + NakedReceiverMutator -> v2.0.0-rc.1"
branch: "feature/11-v2-pit2-cargo-batch"
started_at: "2026-04-30"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 11 — PIT-2 + cargo-mutants Operator Batch

**GitHub-Issue:** [#11](https://github.com/pgm1980/stryker-netx/issues/11)
**Base-Tag:** `v2.0.0-preview.5` (Sprint 10 closed)
**Final-Tag:** `v2.0.0-rc.1`
**Reference inputs:** `_input/mutation_framework_comparison.md` §4.1 (PIT-2) + §4.2 (cargo-mutants)

## Aktueller Phase-Stand

- [x] **11.1** — `ConstructorNullMutator` (`new Foo(...) → null`) — PIT CONSTRUCTOR_CALLS
- [x] **11.2** — `MatchGuardMutator` (`when expr → when true/false`) — cargo-mutants C4
- [x] **11.3** — `WithExpressionMutator` (record-with field deletion) — cargo-mutants C5
- [x] **11.4** — `NakedReceiverMutator` (`a.M(b) → a`) — PIT EXP_NAKED_RECEIVER
- [x] **11.5** — Wire 4 mutators into `DefaultMutatorList`
- [x] **11.6** — Build/Test/E2E green
- [x] **11.7** — Sprint-close + lessons + tag `v2.0.0-rc.1`

## Sprint-11-DoD

- [x] 4 new mutator files in `src/Stryker.Core/Mutators/`
- [x] All 4 carry `[MutationProfileMembership]` (Stronger|All or All-only)
- [x] All 4 wired into `DefaultMutatorList`
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100 % under default profile (new operators silent by design)
- [x] Semgrep clean
- [x] Lessons doc
- [x] Tag `v2.0.0-rc.1`
- [x] Issue #11 closed
- [x] housekeeping_done=true

## Deferred to v2.0.x

- **CRCR full matrix** (overlap with InlineConstants — minimal marginal value)
- **Coverage-driven mutation skip (D3)** (cross-cutting, not operator-shaped)
- **D1 Roslyn Diagnostics filter** (needs compilation-pass diagnostic extraction)
