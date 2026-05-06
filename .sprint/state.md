---
current_sprint: "147"
sprint_goal: "Bug #9 P0 Architektur-Refactor (Bug-Report-4-Trigger): Validation-Layer im RoslynHelper.InjectMutation + Mutator-Audit + Regression-Tests aus Calculator-Tester-Forderungen. KEIN Hotfix mehr — defense-in-depth zentrale Lösung."
branch: "feature/147-bug9-validation-layer"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 147 in progress (Bug #9 Architektur-Refactor)

## Bug-Report 4 — der Auftrag

Calculator-Tester reproduziert Bug #9 nach v3.2.1 als `NullReferenceException` (statt vorher `InvalidCastException`). Gleicher Stack-Trace-Pfad: `NodeSpecificOrchestrator.OrchestrateChildrenMutation` Z.84/85. Der Hotfix in v3.2.1 (Skip-Liste-Erweiterung) hat die Patterns für meine Sample-Tests gefixt, aber Calculator's Real-World-Code triggert ANDERE Code-Pfade die NRE werfen.

**User-Auftrag (4 Forderungen unter P0 Bug #9):**
- a) Ursachen-Analyse statt Stack-Trace-Cosmetik
- b) Pattern-Match statt `as` für Nicht-Type-Fall (designentscheidung statt crash)
- c) **Validierungs-Layer vor der Mutation** — zentrale Stelle in der Pipeline
- d) Regression-Tests mit Calculator's Code-Schnipseln + meinen
- e) Audit aller Mutators im All-Set

Plus P1 Bugs #4 (`--version`), #6 (`--reporters`), #8 (Multi-Project) — separate Sprints 148-150.

## Sprint 147 — Branch C Hybrid (Maxential + ToT 13 Schritte, 3 Branches)

**Decision:** Layer 1 (per-Mutator Skip-Liste — Performance) + Layer 2 (Try/Catch Safety-Net im RoslynHelper.InjectMutation — Generic Validator). Defense-in-depth.

**ToT-Branches evaluiert:**
- Branch A (Try/Catch Safety Net): zentral, future-proof. Performance-cost. ✅ Teil von C.
- Branch B (Reflection Slot-Type-Lookup): theoretisch sauber, praktisch fragil. Verworfen.
- Branch C (Hybrid): A + per-Mutator-Audit + erweiterte Skip-Listen. **GEWÄHLT**.

## Sprint-147-Phasen

- **Phase A** ✅ Code-Analyse via Serena (NodeSpecificOrchestrator.OrchestrateChildrenMutation Z.82-84 + RoslynHelper.InjectMutation Z.120-128)
- **Phase B** Implementation `SyntaxSlotValidator` + integration in RoslynHelper.InjectMutation
- **Phase C** Audit aller All-only Mutators systematisch via Serena
- **Phase D** Erweitere Skip-Listen wo Audit Patterns findet
- **Phase E** Regression-Tests (Calculator's Patterns + eigene)
- **Phase F** Solution-wide build + tests + semgrep
- **Phase G** ADR-028 schreiben (validation-layer architecture)
- **Phase H** Commit + PR + merge + Tag v3.2.2

## Sprint-Plan für Bug-Report-4

- **Sprint 147 (jetzt)**: Bug #9 P0
- **Sprint 148**: Bug #4 P1 (`--version` Flag)
- **Sprint 149**: Bug #6 P1 (`--reporters` plural)
- **Sprint 150**: Bug #8 P1 (Multi-Project UX)

Tag-Strategy dynamisch — entweder einzelne v3.2.x patches oder Sammelrelease v3.3.0.
