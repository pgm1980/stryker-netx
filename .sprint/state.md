---
current_sprint: "173"
sprint_goal: "360°-Analyse A — Mutatoren-Katalog (Findings-only). Alle 55 src/Stryker.Core/Mutators/*.cs + 20 src/Stryker.RegexMutators/*.cs einzeln vollständig lesen; Bug-Klassen-Checkliste aus der Projekt-Historie (Emission-Typdisziplin ADR-047/049, Slot-Kompatibilität ADR-027/028/032, Guard-Lücken unsigned/nullable/const/checked/expression-trees/ref/async, Äquivalenz-Unfälle, Profile-Membership, Kultur/Format, PIT/cargo/mutmut-Treue); Register _docs/analysis/sprint_173_findings.md batch-weise; Verdacht→Verifikation→Issue (P0 sofort an User); KEINE Fixes (User-Direktive Findings-only, Fix-Backlog konsolidiert in Sprint 178). Teil des 6-Sprint-Programms 173-178 (Issue #276). Kein Tag."
branch: "feature/173-analysis-a-mutator-catalogue"
started_at: "2026-06-11"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 173 (360°-Analyse A: Mutatoren-Katalog)

## Programm-Kontext

User-Direktive nach Sprint 172: 360°-Analyse aller Quelltext-Dateien in 4–6
Sprints, Fable-5-Tiefenlektüre. Approved: Findings-only / #270-Fix vorgezogen
(erledigt, v3.3.3) / src/ tief + tests/ als Orakel. Programm-Issue #276.
Nightly-Dogfood läuft seit v3.3.3 als 9-10/11-Sicherheitsnetz unter dem Programm.

## Scope Sprint 173

| Block | Dateien | LOC |
|-------|---------|-----|
| Core/Mutators | 55 | ~4.265 |
| RegexMutators | 20 | ~711 |

## Status

- [x] Branch + Issue #276 + Register-Infrastruktur
- [ ] Batch-Lektüre Core/Mutators (55)
- [ ] Batch-Lektüre RegexMutators (20)
- [ ] Verifikation der Verdachtsfälle, Issues, Register-PR, Close
