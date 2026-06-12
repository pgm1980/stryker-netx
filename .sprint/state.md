---
current_sprint: "184"
sprint_goal: "Fix-Sprint 6/6 — letzter Fahrplan-Sprint (Backlog-Rest, sprint_178_synthesis.md): #287/G-22 (RegisterCoverage HashSet-Drop-in + BenchmarkDotNet-Delta; G-24 toter Handler), #280/F-01 (Mutator.Number statt Linq-Fehlkategorisierung der Konstanten-Mutatoren, Type-Assertions), #279-Batch-1 (UOI numerischer Gate + get-only-Props, ROR Ordnungs-Matrix nur numerisch/IComparable, ConstructorNull IsReferenceType-Gate, TypeDrivenReturn Async-Guard, Doc-F-30; Block/AsSpanAsMemory/F-08 bleiben offen), H-25 (Threshold-Quervalidierung Effektivwerte), I-11 (Guard statt mutantId=-1). TDD je Fix; Serena-first; Memory vor/nach Sprint. Erfolgsmaße: 56 %-CE-Probe (profile All) deutlich rückläufig; Benchmark-Delta dokumentiert. Ship: PR → Squash → Tag v3.3.9 → Release → Closing."
branch: "feature/184-backlog-rest"
started_at: "2026-06-12"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 184 (Backlog-Rest, Fahrplan-Abschluss)

## Fix-Liste

| Fix | Befund | Ort | Status |
|-----|--------|-----|--------|
| 1 | #287/G-22+G-24 | MutantControl.RegisterCoverage → HashSet; toter ProcessExit-Handler raus; Benchmark | ☐ |
| 2 | #280/F-01 | Mutator-Enum + Number; InlineConstants/ConstantReplacement umkategorisieren; Type-Assertions | ☐ |
| 3 | #279/F-07 | UoiMutator: numerischer Typ-Gate (GetTypeInfo) + get-only-Property-Check | ☐ |
| 4 | #279/F-06 | RorMatrixMutator: Ordnungs-Replacements nur numerisch/IComparable; ==/!=-Swaps bleiben | ☐ |
| 5 | #279/F-35 | ConstructorNullMutator: IsReferenceType-Gate (Doc-Versprechen einlösen) | ☐ |
| 6 | #279/F-23 | TypeDrivenReturnMutator: Async-Guard | ☐ |
| 7 | #279/F-30 | Doc-Korrektur „classified as killed" → CompileError/Rollback (3 Dateien) | ☐ |
| 8 | H-25 | Threshold-Quervalidierung gegen Effektivwerte | ☐ |
| 9 | I-11 | MTP-Multi-Mutant-Guard statt stillem mutantId=-1 | ☐ |

## Erfolgsmaße — ERGEBNIS 2026-06-12
- **CE-Probe (Class5, 173er-Shapes, Profil All): 56 % → 29,4 %** (5 CE von 17). Die Probe deckte zwei F-07-Gate-Löcher auf (UOI auf Namespace-/Methodengruppen-Identifier — aufgelöste Nicht-Wert-Symbole), die nachgeschärft wurden; verbleibende CEs sind ausschließlich die bewusst offenen Klassen des #279-Epics (Block-Removal F-09/F-14, AsyncAwait) ✓
- **BenchmarkDotNet (Release, 20 Hits/Mutant): 10k Mutanten 23.262 µs → 2.076 µs (11,2×, Ratio 0.09); 1k: 375 → 203 µs** ✓
- Je Fix Red→Green ✓ · Build 0/0 ✓ · Semgrep 0/11 ✓ · Core-Suiten grün (549 + 1248; ein nicht-reproduzierbarer Einzelflake in Doppelläufen, isoliert 4× grün)

## Notizen
- #279 bleibt nach Batch 1 OFFEN (Block/F-14, AsSpanAsMemory/B.1+B.2, F-08-Filter-Erweiterung) — Checkboxen im Issue abhaken
- Blaupausen im Repo: NullCoalescingExpressionMutator (FlowState), ArgumentPropagation (ClassifyConversion), MemberVariable (Symbol-Gate)
- MutantControl ist C#-2-beschränkt per Doku — HashSet ist .NET 3.5+: Kommentar-Lage prüfen
- #302-Quickies nur bei Restzeit (G-30, G-05, H-10, G-34)
