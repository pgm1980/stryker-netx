---
current_sprint: "151"
sprint_goal: "Bug #9 v3.2.5 systemic audit (Bug-Report 5): Sprint-147 Validator extension von Injection-Phase auf Orchestration-Phase, projektweites Cast-Site-Audit + Listing als Patch-Note. ADR-032. v3.2.6."
branch: "feature/151-bug9-systemic-cast-audit"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 151 in progress (Bug #9 systemic audit)

## Bug-Report 5 — der Auftrag

Calculator-Tester Bug-Report 5: in v3.2.5 reproduziert sich der Cast-Crash auf Calculator.Infrastructure als NEUER Cast `ParenthesizedExpressionSyntax → IdentifierNameSyntax`. User-Forderung verschärft: "projektweite Suche nach allen Casts in Mutator-Code-Pfaden + Listing als Patch-Note + **systemischer** Eingriff (Symptom-Behandlung wäre nicht akzeptabel)".

**Architektonischer Trugschluss von Sprint 147 korrigiert:** ADR-028 Validator deckt nur Injection-Phase, nicht Orchestration-Phase. Der ursprüngliche Sprint-147-Punkt (e) "Audit aller Mutators" wurde nicht durchgeführt — Trugschluss "Validator als Safety-Net macht Audit unnötig". Sprint 151 holt das nach.

## Sprint 151 — Branch S3 Hybrid (Maxential 5-Schritte mit 3 ToT-Branches)

**Decision:** S3 Hybrid (per-child validation + final safety-net):
- Per-Site fix für 4 unsafe Orchestrator `ReplaceNodes`-Sites (Base + 3 derived)
- `OrchestrationHelpers.ReplaceChildrenValidated`: per-child `SyntaxSlotValidator.TryReplaceWithValidation` + bulk try/catch
- Konsistent mit Sprint-147-Defense-in-Depth-Architektur
- Audit-Listing als Patch-Note in ADR-032 (8 safe + 4 unsafe sites projektweit)

## Sprint-151-Phasen

- **Phase A** ✅ Maxential 5-step (3 ToT branches) — S3 Hybrid chosen
- **Phase B** ✅ Audit aller Cast-Patterns projektweit (Mutators + Orchestrators + Initialisation)
- **Phase C** ✅ OrchestrationHelpers.ReplaceChildrenValidated implementiert (116 LOC + 2 LoggerMessage)
- **Phase D** ✅ 4 Orchestrator-Sites umgeroutet (Base NodeSpecific + 3 derived: Conditional/Invocation/ExpressionBodiedProperty). Andere 2 Sites (StaticField/StaticConstructor) safe-by-construction.
- **Phase E** ✅ 12 neue Tests (10 OrchestrationSlotValidationTests + 2 OrchestrationHelpersTests)
- **Phase F** ✅ Solution-wide build (0 W / 0 E), 2047 Tests grün, Semgrep clean
- **Phase G** ✅ ADR-032 + Audit-Listing als Patch-Note + 0.17.0 history-row
- **Phase H** PR + merge + tag v3.2.6

## Bug-Report 5 Status

- Bug #9 (verschärft) ✓ closing mit ADR-032 (Sprint 151 / v3.2.6) — Audit durchgeführt, 4 unsafe Sites gefixt, 8 safe Sites dokumentiert.
- Bugs #4, #6, #8 ✓ unverändert closed mit Bug-Report-4-Sprint-Sweep (147-150).
