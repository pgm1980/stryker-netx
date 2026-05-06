---
current_sprint: "150"
sprint_goal: "Bug #8 P1 (Multi-Project Test-Setup-UX): neue --all-projects Flag damit Test-Projekt mit mehreren Source-References ALLE referenced Source-Projekte mutiert. Calculator-Tester Bug-Report 4. ADR-031. v3.2.5. SCHLIESST Bug-Report 4 vollständig."
branch: "feature/150-bug8-all-projects"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 150 in progress (Bug #8 --all-projects)

## Bug-Report 4 Bug #8 — der Auftrag

Calculator-Tester Bug-Report 4: Test-Projekt mit mehreren `<ProjectReference>` zu Source-Projekten (Clean Architecture: Domain + Infrastructure + App) bricht mit `"Test project contains more than one project reference. Please set the project option…"`. Sprint-141 hat `--solution` als Alternative im Hinweis ergänzt, aber `--solution` setzt eine Solution-Datei voraus + scannt ALLE Solution-Projekte. User-Forderung: per-Test-Project-Flag das nur die referenzierten Source-Projekte mutiert.

## Sprint 150 — Option B1 (--all-projects Flag, 11-Schritte Maxential)

**Decision:** B1 (--all-projects flag, NoValue, long-only). Sauber abgrenzt: 6 modified files + 5 unit tests + Initialisation-Layer-Branch. Kein Engine-Eingriff, kein Breaking-Change.
**Verworfen:** B2 (Multi-`--project` mit MultipleValue) wegen Breaking-Change auf SourceProjectName-API + Filter-Matching-Refactor durch ganze Pipeline.

## Sprint-150-Phasen

- **Phase A** ✅ Maxential 11 Schritte mit 2 ToT-Branches (B1 vs B2, B1 gewählt)
- **Phase D1** ✅ AllProjectsInput + IStrykerInputs + IStrykerOptions wired
- **Phase D2** ✅ StrykerOptions.IsAllProjectsMode + ValidateAll wiring (incl. MA0051 Refactor BuildThresholds)
- **Phase D3** ✅ CommandLineConfigReader --all-projects registration (long-only)
- **Phase D4** ✅ InputFileResolver.ResolveMultiReferenceCase Helper (incl. MA0051 Method-Body Refactor) + verbesserte Fehlermeldung
- **Phase D5** ✅ 7 neue Tests (5 AllProjectsInputTests + 2 CLI-Plumbing) + ConfigBuilderTests Mock-Update
- **Phase F** ✅ Solution-wide build (0 W / 0 E), 2035 Unit-Tests grün, Semgrep clean
- **Phase G** ✅ ADR-031 + 0.16.0 history-row geschrieben
- **Phase H** PR + merge + tag v3.2.5

## Sprint-Plan für Bug-Report-4

- **Sprint 147** ✅ closed: Bug #9 P0 (v3.2.2 ADR-028)
- **Sprint 148** ✅ closed: Bug #4 P1 (v3.2.3 ADR-029)
- **Sprint 149** ✅ closed: Bug #6 P1 (v3.2.4 ADR-030)
- **Sprint 150 (jetzt)**: Bug #8 P1 (`--all-projects`)

**Mit Sprint 150 ist Bug-Report 4 vollständig geschlossen.** Alle 4 Bugs (#4, #6, #8, #9) sind via ADRs 028–031 architektonisch fixed.
