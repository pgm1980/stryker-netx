---
current_sprint: "144"
sprint_goal: "v3.2.0-dev Phase 2 — type-position-aware mutation control: CAE-aware lift für MB.Name + MA-in-CAE-WhenNotNull-subtree (UoiMutator walk-up bis enclosing CAE). KEIN Tag — multi-sprint refactor, Phase 3 outstanding."
branch: "feature/144-engine-refactor-part2-uoi-cae-lift"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 144 in progress (v3.2.0-dev Phase 2)

## Sprint 144 — ADR-027 Phase 2

Phase 1 (Sprint 143) hat MA.Name pivot eingeführt. Phase 2 erweitert um CAE-aware
lifting für `MB.Name` (`data?.Length`) und MA-in-CAE-WhenNotNull-subtree
(`data?.X.Length`) — beide Cases würden ohne lift zu Roslyn-Binder-NRE führen
weil `ConditionalAccessExpression.WhenNotNull` binding-led (Start mit `.` oder `[`)
sein muss. Lift hebt den Pivot bis zur outermost-CAE der enclosing
`?.`-Kette, sodass der Postfix/Prefix-Operator das gesamte CAE umschließt
(`data?.Length` → `data?.Length++` als PostfixUnary(CAE)).

### Maxential decision (13 Schritte, 2 Branches)
- **Variant A (gewählt)**: Mutator-zentrische CAE-walk-up Logic im UoiMutator. Exakt wie ADR-027 Phase 2 spezifiziert.
- **Variant C (verworfen)**: Engine-side automatic Lift — zu invasiv für Phase 2; eher Phase-3-/Phase-4-Topic.

### Phase-1-Gap entdeckt
MA-in-CAE-WhenNotNull-subtree (`data?.X.Length`) war Phase 1 schon broken
(PostfixUnary(MA) im CAE.WhenNotNull-slot ist nicht binding-led). Sample.Library
hat kein solches pattern, daher Phase-1-e2e-Test nicht broken. Real-World-Code
(Calculator-Tester, ~1300 LOC) hat solche patterns. Phase 2 fixt beide MA-und-MB
Cases via einheitlichen CAE-walk-up.

### Implementation
1. **UoiMutator**: smart pivot mit CAE-walk-up. Initial pivot via MA.Name OR
   MB.Name parent. Dann `LiftPastConditionalAccess` while-loop bis pivot nicht
   mehr in einer CAE.WhenNotNull-Subtree liegt.
2. **UoiMutator.IsSafeToWrap**: Phase-1's MB.Name-skip entfernen.
3. **MemberAccessNameSlotOrchestrator.CanHandle**: Predicate von MA.Name auf
   MA.Name OR MB.Name aufweiten.
4. **CsharpMutantOrchestrator**: Phase-1's `DoNotMutateOrchestrator<SimpleName>(MB.Name)`
   Guard ENTFERNEN.
5. **ConditionalExpressionOrchestrator**: UNVERÄNDERT — existierende
   `MutationControl.MemberAccess`-Mechanik trägt Phase 2 von selbst.

### Verifikation (geplant)
- Lokal `--mutation-profile All --mutation-level Complete` mit `data?.Length`-Pattern: kein Crash, kein file-compile-poisoning.
- Lokal mit `data?.X.Length`-Pattern: kein Crash.
- Solution-wide tests grün.
- Semgrep clean.

### KEIN Tag
Phase 3 (TypeSyntax-Engine für SpanReadOnly re-enable) outstanding. v3.2.0 Tag
erst nach Phase 3.
