---
current_sprint: "183"
sprint_goal: "Fix-Sprint 5/6 (Config & Nebenläufigkeit, Fahrplan sprint_178_synthesis.md): #290/H-17 (--configuration/--target-framework erreichen die MSBuildWorkspace nie — Factory-Lambda füttert Provider-Ctor mit Optionen), #291/H-18 (Multi-TFM-Fallback parst erstes Listen-Segment statt InputException auf der Rohliste), #296/I-09 (Multi-Projekt-Init-Races: sequenzialisieren + Instanz-Ersetzungs-/Clear-Fixes), #299/J-04 (GitInfoProvider exaktes Branch-Segment-Matching statt Substring-Contains + master-Default-Fallback + saubere Meldung), #300/J-06 (SseServer/RealTimeMutantHandler Synchronisation + Listener-Guard + best-effort CloseSse — Broadcast-Kette darf nicht brechen). TDD je Fix; Serena-first für Analyse UND Implementierung; Serena-Memory vor/nach Sprint. Erfolgsmaße: P-5-Probe (-c Release → Injektion in bin\\Release), P-6-Probe (Multi-TFM-Lauf Exit 0). Ship: PR → Squash → Tag v3.3.8 → Release → Closing."
branch: "feature/183-config-concurrency"
started_at: "2026-06-12"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 183 (Config & Nebenläufigkeit)

## Fix-Liste

| Fix | Issue | Ort | Status |
|-----|-------|-----|--------|
| 1 | #299 | Exaktes Segment-Matching + master→main-Fallback (WARN) + --since-target-Meldung + Kurz-SHA (J-05) | ☑ |
| 2 | #300 | Writer-Lock + Snapshot-Send, Listener-Shutdown = Loop-Ende, best-effort Close, ConcurrentQueue | ☑ |
| 3 | #296 | Initial-Tests sequenziell (Overlap-Detektor-Red) + VsTests get-only/in-place | ☑ |
| 4 | #291 | FirstTargetFrameworkFrom (erstes Listen-Segment, public + getestet) | ☑ |
| 5 | #290 | ForProperties-Self-Factory + ConfiguredWorkspace einmal pro Lauf im Resolver | ☑ |

## Erfolgsmaße — ERGEBNIS 2026-06-12
- **P-5 ✓**: `--configuration Release` → „Injected the mutated assembly file into …\bin\Release\net10.0\ProbeLib.dll" (Baseline: bin\Debug), EXIT=0
- **P-6 ✓**: ProbeLib `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>` ohne --target-framework → EXIT=0, Score 83,33 % (Baseline: Exit 1 InputException). Probe deckte SCHICHT 2 auf: nach dem TFM-Listen-Fix starb der Lauf an „Language not supported: Undefined" (Outer-Evaluation ohne per-Framework-Properties) → Loader pinnt die Evaluation aufs erste Listen-TFM
- Je Fix Red→Green ✓ · Build 0/0 ✓ · Vollsuite grün (10 Projekte, E2E 18/18) ✓ · Semgrep 0/9 ✓ · Core-Suiten nach Layer-2-Fix erneut grün (536 + 1237) ✓

## Notizen
- #296: einfachster struktureller Fix = foreach statt Task.WhenAll (Initial-Läufe dominieren die Gesamtzeit nicht); zusätzlich defensive Dictionary-Fixes
- #299: J-05 (Kurz-SHA-Lookup) mitgenommen
- #290: Provider-Lebenszyklus beachtet (ConfiguredWorkspace einmal pro transientem Resolver)
- ProbeLib.csproj nach P-6 zurückgesetzt auf <TargetFramework>net10.0</TargetFramework> ✓

## Ship-Protokoll
- PR #313 squash-merged (ace6848); Issues #290/#291/#296/#299/#300 geschlossen + Evidenz-Kommentare
- Tag v3.3.8 auf Merge-Commit; Release-Run 27422801439 **success** (kein NU190x)
- Serena project_status_and_roadmap (183 ✅, 184 NÄCHSTER/letzter Fahrplan-Sprint) + Claude-Memory aktualisiert
- Kern-Lehre dokumentiert: Live-Probe deckte #291-Schicht-2 auf (Outer-Evaluation ohne per-Framework-Properties), die Unit-Tests nicht sahen
