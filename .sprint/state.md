---
current_sprint: "185"
sprint_goal: "Äquivalenz-Filter-Cluster (externer 360°-Test): EQF-001 (High, RoslynSemanticDiagnosticsEquivalenceFilter verwirft Methodengruppen-Mutanten — StringMethod immer, Linq bei datei-level using), EQF-003 (Med, ConservativeDefaultsEqualityFilter flaggt killbare unsigned-Null-Vergleiche), EQF-002 (Low, IdentityArithmeticFilter+IdempotentBooleanFilter inert). Alle in src/Stryker.Core/Mutants/Filters/. TDD je Fix, Serena-first, ADR-059. Ship: PR → Squash → Tag v3.3.10 → Release → Closing."
branch: "feature/185-equivalence-filters"
started_at: "2026-06-15"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 185 (Äquivalenz-Filter-Cluster, v3.3.10)

> Erster Sprint des externen-360°-Test-Fix-Blocks (185–187, Variante B risiko-isoliert,
> via MAXential+ToT). Quelle: `_bug_reporting/BUG_REPORT.md` + `UPSTREAM_ISSUES.md`;
> Repros: `_bug_reporting/testing/` (byte-genau gesichert). Roadmap: 185 Filter, 186 Quick-Wins
> (MAT-001/SOL-001/RUN-001 → v3.3.11), 187 INJ-001 SOLO (→ v3.3.12).

## Fix-Liste — ERLEDIGT 2026-06-15

| Fix | Befund | Sev | Ort | Status |
|-----|--------|-----|-----|--------|
| 1 | EQF-001 | High | RoslynSemanticDiagnosticsEquivalenceFilter — CandidateSymbols-Gate (beide Pfade); Methodengruppe = Kandidaten vorhanden = valide. MAXential+ToT (A1 0.93). | ✅ |
| 2 | EQF-003 | Med | ConservativeDefaultsEqualityFilter — positions-normalisiert (FlipComparison), nur 2/8 Kombis äquiv. Über Report hinaus (Operand-Position). | ✅ |
| 3 | EQF-002a | Low | IdentityArithmeticFilter — struktur-basierte right-identity; 0-x/1/x-Fallen ausgeschlossen. Über Report hinaus. | ✅ |
| 4 | EQF-002b | Low | IdempotentBooleanFilter — konzeptuell inert, ehrlich dokumentiert (MAXential+ToT 0.92, kein spekulativer Fix). | ✅ |

## Erfolgsmaße — ERGEBNIS
- Je Fix Red→Green ✅ · Build 0/0 (TWAE) ✅ · Stryker.Core.Tests 567/567 ✅ (1 bekannter Log-Capture-Flake im Parallel-Lauf, isoliert grün + Doppellauf grün) · Filter-Tests 57/57 ✅ · Semgrep 0 ✅
- **E2E-Probe (bug01, lokale CLI mit Fix): 8 Methodengruppen-Mutanten getestet (Score 75 %), 0 als equivalent ge-Ignored** — vorher (v3.3.9) alle gefiltert. Linq `xs.Max` Killed = Blast-Radius end-to-end belegt (Unit-Harness deckt den Linq-Fall nicht ab).
- 3 bug-pinnende Tests (die das defekte Verhalten festschrieben) korrigiert.

## Notizen
- EQF-003 + EQF-002a: Operand-Position-Nuance über den Report hinaus entdeckt (naiver Report-Fix hätte neuen Bug eingeführt).
- IdempotentBooleanFilter: kein realer äquivalenter Boolean-Mutant existiert → spekulativer Fix wäre unfalsifizierbar (TDD-widrig); Double-Negation-Pfad korrekt, nur ungenutzt.
- Offen für Closing: housekeeping_done, memory_updated (Serena + Claude-Memory nach Ship).
