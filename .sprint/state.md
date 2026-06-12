---
current_sprint: "183"
sprint_goal: "Fix-Sprint 5/6 (Config & Nebenläufigkeit, Fahrplan sprint_178_synthesis.md): #290/H-17 (--configuration/--target-framework erreichen die MSBuildWorkspace nie — Factory-Lambda füttert Provider-Ctor mit Optionen), #291/H-18 (Multi-TFM-Fallback parst erstes Listen-Segment statt InputException auf der Rohliste), #296/I-09 (Multi-Projekt-Init-Races: sequenzialisieren + Instanz-Ersetzungs-/Clear-Fixes), #299/J-04 (GitInfoProvider exaktes Branch-Segment-Matching statt Substring-Contains + master-Default-Fallback + saubere Meldung), #300/J-06 (SseServer/RealTimeMutantHandler Synchronisation + Listener-Guard + best-effort CloseSse — Broadcast-Kette darf nicht brechen). TDD je Fix; Serena-first für Analyse UND Implementierung; Serena-Memory vor/nach Sprint. Erfolgsmaße: P-5-Probe (-c Release → Injektion in bin\\Release), P-6-Probe (Multi-TFM-Lauf Exit 0). Ship: PR → Squash → Tag v3.3.8 → Release → Closing."
branch: "feature/183-config-concurrency"
started_at: "2026-06-12"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 183 (Config & Nebenläufigkeit)

## Fix-Liste

| Fix | Issue | Ort | Status |
|-----|-------|-----|--------|
| 1 | #299 | GitInfoProvider.GetTargetCommit — exaktes Segment-Matching + master-Fallback + Meldung | ☐ |
| 2 | #300 | SseServer (_writers) + RealTimeMutantHandler (_delayedEventQueue) — Sync, Listener-Guard, best-effort Close | ☐ |
| 3 | #296 | Initial-Tests sequenzialisieren + VsTests-Instanz-Ersetzung + projektscharfes ClearInitialResult | ☐ |
| 4 | #291 | RoslynProjectAnalysis.TargetFramework — erstes Listen-Segment + Meldungs-Hinweis | ☐ |
| 5 | #290 | DI-Factory füttert MSBuildWorkspaceProvider mit Configuration/Platform/TargetFramework | ☐ |

## Erfolgsmaße
- P-5-Probe: `-c Release` → Debug-Log zeigt Injektion in `bin\Release\...` (Baseline: bin\Debug trotz Release-Build)
- P-6-Probe: ProbeLib mit `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>` → Exit 0 (Baseline: Exit 1 InputException)
- Je Fix Red→Green; Build 0/0; Vollsuite grün; Semgrep 0

## Notizen
- #296: einfachster struktureller Fix = foreach statt Task.WhenAll (Initial-Läufe dominieren die Gesamtzeit nicht); zusätzlich defensive Dictionary-Fixes
- #299: J-05 (Kurz-SHA-Lookup) nur mitnehmen, wenn trivial — sonst #302
- #290: Provider-Lebenszyklus beachten (Factory pro Resolve-Lauf); Wechselwirkung mit #291 (Workspace lädt erstes TFM)
- Proben am Ende gegen lokale Release-CLI aus ProbeLib.Tests/ (Class1-Pattern); Probe-Quelle: ProbeLib.csproj temporär auf Multi-TFM für P-6
