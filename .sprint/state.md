---
current_sprint: "181"
sprint_goal: "Fix-Sprint 3/6 (Score-Integrität, Fahrplan sprint_178_synthesis.md): #286/G-15 (ADR-032-Geister-Mutanten — Drop-Pfad in ReplaceChildrenValidated sammelt Mutant-IDs aus Subtree-Annotationen und markiert CompileError statt stillem False-Survivor/NoCoverage), G-17 (generischer No-op-Filter #0 via SyntaxFactory.AreEquivalent gegen Replacement≡Original-False-Survivors), G-19 (equivalence-gefilterte Mutationen als Ignored mit ResultStatusReason statt spurlosem continue — Upstream-Parität), J-01 (JsonMutant serialisiert schema-fremdes „Pending" im Failed-to-test-Pfad). TDD je Fix; Serena-first für Code-Analyse UND Implementierung; Serena-Memory vor/nach Sprint aktuell. Ship: PR → Squash → Tag v3.3.6 → Release → Closing."
branch: "feature/181-score-integrity"
started_at: "2026-06-12"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 181 (Score-Integrität)

## Fix-Liste

| Fix | Befund | Ort | Status |
|-----|--------|-----|--------|
| 1 | G-17 | Neuer Filter #0: SyntaxFactory.AreEquivalent(Original, Replacement) in der Equivalence-Pipeline | ☐ |
| 2 | G-19 | CsharpMutantOrchestrator: gefilterte Mutationen → Ignored-Mutant mit Reason statt continue | ☐ |
| 3 | #286/G-15 | OrchestrationHelpers.ReplaceChildrenValidated: Drop-Pfad sammelt IDs → CompileError | ☐ |
| 4 | J-01 | JsonMutant: „Pending" darf den Report nicht erreichen (schema-gültiges Mapping) | ☐ |

## Erfolgsmaße
- Je Fix Red-Test → Green; Geister-Mutanten enden als CompileError im Report (Unit-Test über die 3-Schichten-Kette soweit testbar)
- No-op-Mutationen (Replacement ≡ Original) erzeugen keine Survivors mehr; gefilterte erscheinen als Ignored mit Reason
- Build 0/0, Vollsuite grün, Semgrep 0

## Notizen
- Reihenfolge bewusst: G-17 zuerst (Filter existiert), dann G-19 (macht Filter-Ergebnis sichtbar — Wechselwirkung: No-ops werden danach als Ignored REGISTRIERT statt verworfen)
- G-15-Plumbing: OrchestrationHelpers ist statisch — ID-Extraktion via MutantPlacer-Annotationen („MutationId") des verworfenen Subtrees; Status-Setzen braucht Weg zu den Mutant-Objekten (Aufrufer-Kontext analysieren)
- J-01: Failed-to-test-Pfad (G-09-Anschluss) prüfen — wo bleibt Pending stehen?
