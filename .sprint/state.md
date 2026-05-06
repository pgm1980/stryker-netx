---
current_sprint: "142"
sprint_goal: "Hotfix v3.1.2 — Bug #9 (--mutation-profile All InvalidCastException) via UoiMutator pre-check + SpanReadOnly disable + global DoNotMutate"
branch: "feature/142-bug9-all-profile-crash"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 142 (active, hotfix v3.1.2)

## Goal
Calculator-tester Bug-Report-2 (`_bug_reporting/bug_report_2_stryker_netx.md`) Bug #9: `--mutation-profile All` crasht mit `InvalidCastException(ParenthesizedExpression → TypeSyntax/SimpleNameSyntax)`. Hotfix v3.1.2 — pure bug-fix, additive disable, kein Behavior-Change für Defaults/Stronger-Profile.

## Diagnose-Trail (Sprint 142)

**Lokale Repro:** SpanTester.cs mit `data.Length > 0 ? data[0] : 0` zu Sample.Library hinzugefügt → CLI mit `--mutation-profile All` aufgerufen → InvalidCastException reproduziert.

**Bisect-Result:**
- UoiMutator allein triggert SimpleNameSyntax-Variante des Crashs (auf `data.Length`)
- SpanReadOnlySpanDeclarationMutator allein triggert nicht im local-Repro, aber per Bug-Report-Stack-Trace (TypeSyntax-Variante) auf Calculator-tester-codebase

**Root cause (verified):**
- ConditionalInstrumentationEngine wrappt Mutationen in `ParenthesizedExpressionSyntax` (`(MutantControl.IsActive(N) ? mutated : original)`)
- Mutators die in TypeSyntax-/NameSyntax-Slots emittieren produzieren ParenthesizedExpression in einem Slot wo Roslyn's typed visitor strict NameSyntax/TypeSyntax erwartet → `InvalidCastException` zur Mutation-Time
- Sprint 23 hat das gleiche Pattern für QualifiedNameSyntax schon adressiert (UoiMutator parent-skip + global DoNotMutate). Sprint 142 erweitert auf SimpleNameSyntax (.Name slot) + SpanReadOnly's TypeSyntax slot.

## Maxential-Decision (5 Thoughts)

**Final: A+B+SpanReadOnly-disable** (Sprint 23 precedent):
- A: UoiMutator.IsSafeToWrap() erweitern um MemberAccess.Name + MemberBinding.Name zu skippen
- B: Global DoNotMutateOrchestrator<SimpleNameSyntax> mit predicate (parent.Name == t)
- SpanReadOnly: aus All-Profile rausnehmen via `[MutationProfileMembership(MutationProfile.None)]`. Re-enable bedingt engine-fix.

Verworfen: (C) Engine-rewrite (zu invasiv für hotfix), (A-only) (fehlt future-proofing), (D-keep-Span) (mutator emittiert ausschließlich in TypeSyntax → kompletter pre-check würde Mutator deaktivieren, daher direkter via Profile.None).

## Implementation

### Code-Changes
- `src/Stryker.Core/Mutators/UoiMutator.cs`: IsSafeToWrap() erweitert mit MemberAccess.Name + MemberBinding.Name skip
- `src/Stryker.Core/Mutators/SpanReadOnlySpanDeclarationMutator.cs`: `[MutationProfileMembership(MutationProfile.None)]` + Doc-Comment mit Sprint-142-Note + Re-enable-Bedingungen
- `src/Stryker.Core/Mutants/CsharpMutantOrchestrator.cs`: neue `DoNotMutateOrchestrator<SimpleNameSyntax>(predicate=...)` registriert; Sprint-23-Comment-Block kombiniert mit Sprint-142

### Doc-Changes
- `_docs/architecture spec/architecture_specification.md`: ADR-026 hinzugefügt (Profile/Level conjunctive-incompat-Pattern, alternatives evaluated, future re-enable conditions); Änderungshistorie 0.8.0 entry

### Test-Changes
- `tests/Stryker.Core.Tests/Mutators/UoiMutatorTests.cs`: 3 neue Regression-Tests (RightHandOfMemberAccess, StillMutates_LocalIdentifier, RightHandOfMemberBinding)
- `tests/Stryker.Core.Tests/Mutators/SpanReadOnlySpanDeclarationMutatorTests.cs`: Profile_IsAllOnly → Profile_IsNone_AsOfSprint142
- `tests/Stryker.Core.Tests/Properties/MutatorReflectionProperties.cs`: IntentionallyDisabledMutators-Whitelist + Tests entsprechend angepasst

## Verification

- Lokale Repro: Crash war `InvalidCastException(ParenthesizedExpression → SimpleNameSyntax)` auf `data.Length`-Pattern. Nach Fix: 61 mutants created, score 65.22 %, kein Crash.
- Solution-wide Tests (excl. E2E): **2003 grün, 27 legitimate skips, 0 fail** (1184 dogfood + 391 core + 80 CLI + 142 MTP + 57 VsTest + 128 RegexMutators + 17 Sample + 15 Solutions + 10 Architecture)
- Semgrep on changed files: 0 findings

## Tag-Strategy
**v3.1.2 (Patch)** — pure bug fix + disable. Kein behaviour-change für die meisten User. Sprint-Tag-Convention: nach merge-on-main + ff-only-pull → tag → push → release.yml auto-trigger.

## Known follow-ups (nicht Sprint 142)
- ConditionalInstrumentationEngine type-position-aware machen (separate ADR, multi-sprint, kann SpanReadOnlySpanDeclarationMutator wieder enable)
- Bug #4 (Tester-Sicht: `--version` braucht Argument) — Sprint 141 hat additiv `--tool-version` gemacht; Tester erwartet anderen Approach. Sprint 143 könnte das nochmal aufgreifen.
- Bug #6 (Tester-Sicht: `--reporters` plural unbekannt) — Sprint 138 hat Doku gefixt; Tester erwartete Alias-Akzeptanz. Sprint 143 könnte das additiv ergänzen.
