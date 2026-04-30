---
current_sprint: "13"
sprint_goal: "Spec-gap closure: 8 new mutators (ArgumentPropagation, MemberVariable, MethodBodyReplacement, SwitchArmDeletion, TaskWhenAll<->WhenAny, ConfigureAwait swap, DateTime AddDays sign-flip, AsSpan<->AsMemory) -> v2.0.1"
branch: "feature/13-v2-spec-gap-closure"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 13 — Spec-gap closure (v2.0.1)

**GitHub-Issue:** [#13](https://github.com/pgm1980/stryker-netx/issues/13)
**Base-Tag:** `v2.0.0` (Sprint 12 closed)
**Final-Tag:** `v2.0.1`
**Reference inputs:** `_input/mutation_framework_comparison.md` §4.1 + §4.2 + §4.4 + Phase A doc-reconciliation patch (commit 4ae08bd)

## Aktueller Phase-Stand

**Phase A — completed in commit 4ae08bd (doc reconciliation):**
- [x] **A.1** — README profile-count typo (+10/+4 statt +9/+5)
- [x] **A.2** — README known-limitations + semantic-deviation entries
- [x] **A.3** — MIGRATION roadmap split into open-by-section + documented-deviations
- [x] **A.4** — MEMORY entries Sprint 11/12 corrected

**Phase B — implementation order ascending in risk:**
- [x] **13.1** — `ConfigureAwaitMutator` — Stronger | All
- [x] **13.2** — `DateTimeAddSignMutator` — Stronger | All
- [x] **13.3** — `SwitchArmDeletionMutator` — Stronger | All
- [x] **13.4** — `MemberVariableMutator` (type-aware) — Stronger | All
- [x] **13.5** — `ArgumentPropagationMutator` (type-aware) — All only
- [x] **13.6** — `TaskWhenAllToWhenAnyMutator` — Stronger | All
- [x] **13.7** — `AsSpanAsMemoryMutator` — All only
- [x] **13.8** — `MethodBodyReplacementMutator` (type-aware) — All only
- [x] **13.9** — Wire all 8 into `DefaultMutatorList`
- [x] **13.10** — README + MIGRATION final tally update (48 mutators total)
- [x] **13.11** — Build/Test/E2E/Semgrep green
- [x] **13.12** — Sprint-close + lessons + tag `v2.0.1` + GitHub release + merge to main

## Sprint-13-DoD

- [x] 8 new mutator files
- [x] All carry `[MutationProfileMembership]`
- [x] All wired into `DefaultMutatorList`
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100% under default profile
- [x] Semgrep clean
- [x] README + MIGRATION updated (26 + 15 + 7 = 48)
- [x] Lessons doc
- [x] Tag `v2.0.1`
- [x] GitHub release published
- [x] Branch merged into main
- [x] Issue #13 closed
- [x] housekeeping_done=true
