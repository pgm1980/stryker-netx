---
current_sprint: "187"
sprint_goal: "INJ-001 SOLO (externer 360°-Test, architektur-schwer): MethodBodyReplacement + GenericConstraint declaration-level nicht injizierbar. Teil A (MethodBodyReplacement) gefixt; Teil B (Constraint-Mutatoren) vertagt (User-Entscheidung, #279-Epic). MAXential+ToT + Live-Probe. ADR-061. Ship: PR → Squash → Tag v3.3.12 → Release → Closing."
branch: "feature/187-inj-decl-injection"
started_at: "2026-06-15"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 187 (INJ-001 SOLO, v3.3.12) — ABSCHLUSS Fix-Block 185–187

> Dritter und LETZTER Sprint des externen-360°-Test-Fix-Blocks. 185 (Filter) ✅ v3.3.10,
> 186 (Quick-Wins) ✅ v3.3.11. INJ-001 = architektur-schwer → MAXential+ToT + Live-Probe (bug02).

## Fix — ERGEBNIS 2026-06-15

| Teil | Befund | Ergebnis |
|------|--------|----------|
| A | MethodBodyReplacementMutator (Body-Replacement) | ✅ GEFIXT — zu `TypeAwareMutatorBase<BlockSyntax>` umgebaut (direkter Method-Body-Block, nicht-async; `Type=Mutator.Statement` gegen IgnoreBlockMutantFilter). E2E bug02: 'Echo' CompileError-Soft-Fail → KILLED. |
| B | GenericConstraintMutator + GenericConstraintLoosenMutator (Constraint-Mutationen) | ⏸️ VERTAGT (User-Entscheidung, #279-Epic) — im Laufzeit-Schalt-Modell prinzipiell nicht injizierbar; als nicht-injizierbar dokumentiert (XML-remarks + ADR-061), NICHT deaktiviert. |

## Erfolgsmaße — ERGEBNIS
- Teil A Red→Green (Unit 6/6) ✅ · **E2E bug02: MethodBodyReplacement 'Echo' KILLED** (vorher CompileError-Soft-Fail „sourceNode is null") ✅
- Build 0/0 (TWAE) ✅ · Stryker.Core.Tests 574/574 ✅ · Dogfood-Orchestrierung 36/37 (1 skip) ✅ · Semgrep 0 ✅

## Architektur-Erkenntnis (ADR-061)
- Höchster Inject-Frame = Method-BODY; MethodDeclaration-OriginalNode in keinem Frame → Soft-Fail.
- Teil A: naiver Block-Retarget scheiterte (Live-Probe!) — BaseFunctionOrchestrator injiziert left-over nur via Expression-Body-Pfad; Lösung = BlockSyntax-Mutator wie BlockMutator.
- Teil B: Orchestrator besucht ConstraintClauses NIE; Constraint-Mutationen nicht per MutantControl schaltbar. Roadmap-„Vorbild" Loosen empirisch FALSIFIZIERT (0 Mutanten). 3-fach-Lehre: Live-Probe > Analyse.

## Notizen
- Offen für Closing: housekeeping_done, memory_updated (Serena + Claude-Memory nach Ship).
- Fix-Block 185–187 ABGESCHLOSSEN: 6/7 Bugs behoben + INJ-001 Teil A; Teil B als Modell-Limit dokumentiert.
