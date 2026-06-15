---
current_sprint: "188"
sprint_goal: "INJ-001 Teil B endgültig: GenericConstraint(+Loosen)-Mutatoren ENTFERNT (semantisches Modell-Limit — Constraint-Mutationen erzeugen nie killbare Mutanten, IL-Probe byte-identisch). MAXential+ToT+Live-Probe. ADR-062 (supersedes ADR-061 Teil B). Katalog 52→50. Ship: PR → Squash → Tag v3.3.13 → Release → Closing."
branch: "feature/188-remove-constraint-mutators"
started_at: "2026-06-15"
housekeeping_done: false
memory_updated: false
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 188 (INJ-001 Teil B, v3.3.13) — Constraint-Mutatoren entfernt

> Nachzügler zum externen-360°-Test-Fix-Block (185–187). INJ-001 Teil B war in Sprint 187
> ins #279-Epic vertagt; User wies an, es endgültig zu lösen. Vertiefte Analyse ergab:
> Constraint-Mutationen sind semantisch kein killbarer-Mutant-Fall → ENTFERNEN statt fixen.

## Ergebnis 2026-06-15

| Schritt | Ergebnis |
|---------|----------|
| Analyse | MAXential (18 Gedanken, Branches rebuild/reframe verworfen) + ToT (entfernen 0.92) + IL-Live-Probe |
| IL-Probe | Method-Body byte-identisch mit/ohne `where T:class` (4==4 Bytes) → Constraint erzeugt kein IL → kein killbarer Mutant möglich |
| Entscheidung | **User: ENTFERNEN** (statt deaktivieren/rebuild/status-quo) |
| Umsetzung | GenericConstraintMutator + GenericConstraintLoosenMutator + 2 Test-Dateien gelöscht (git rm); 2 Orchestrator-Registrierungen + 2 Fremd-crefs (AsSpanAsMemory/TaskWhenAllToWhenAny) + 1 RoslynHelper-Kommentar bereinigt |
| Katalog | 52 → 50 (Defaults 26, Stronger 18→17, All-only 8→7) |
| ADR | **ADR-062** (supersedes ADR-061 Teil B) |

## Verifikation — ERGEBNIS
- Build 0/0 (TWAE) ✅ · Stryker.Core.Tests 562/562 ✅ (+ bekannter Flaky CommentParser-Log-Race isoliert grün; 11 entfernte Constraint-Tests = 574→563)
- Dogfood-Orchestrierung 1239/1248 (9 skip, 0 Fehler) ✅ — keine erwartete Mutanten-Zählung ändert sich (Mutatoren trugen nie erfolgreiche Mutanten bei)
- IL-Live-Probe ✅ · Semgrep 0 (4 geänderte Dateien) ✅ · MutatorReflectionProperties dynamisch → bleibt grün

## Architektur-Erkenntnis (ADR-062)
- Generic Constraints sind REIN COMPILE-TIME (erzeugen kein IL) → Constraint-Mutation kann nur CompileError (Fähigkeit genutzt) oder äquivalenter Survivor (nicht genutzt) sein, NIE killbar (adversarial geprüft, kein Gegenbeispiel — auch nicht Overload-Resolution, CS0111).
- „Injizierbar machen" (Per-Mutant-Rebuild) wäre Kategorienfehler: bricht ADR-021-Performance-USP + verfälscht Score durch falsche Survivor (ToT 0.10, strikt dominiert).
- Mutatoren zudem REDUNDANT: constraint-relevante Body-Logik ist bereits von Operator-Mutatoren abgedeckt → Entfernen verliert keine Coverage.
- Revidiert Sprint-187-Bewertung (entfernen 0.55 → 0.92), weil IL-Beleg + Redundanz die semantische Tiefe festigten.

## Offen für Closing
- housekeeping_done, memory_updated (Serena + Claude-Memory nach Ship).
- INJ-001 ENDGÜLTIG geschlossen (Teil A Sprint 187, Teil B Sprint 188). #279-Epic von Constraint-Altlast befreit.
