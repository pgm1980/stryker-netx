---
current_sprint: "175"
sprint_goal: "360°-Analyse C — Initialisation/Utilities/Solutions/Configuration (Findings-only). 98 Dateien: Initialisation (20, inkl. InputFileResolver/ADR-052-Areal, TimeoutValueCalculator/#273), Utilities (15, inkl. TypeBasedStrategy, TextSpanHelper, MSBuild-Schicht), Solutions (3), Configuration (60, inkl. StrykerOptions + 46 Options/Inputs). Pflicht-Schwerpunkte: #273-Flaky-Wurzel (TimeoutValueCalculator/InitialTestProcess), TypeBasedStrategy-Bucket-Ordnung (174-Vormerkung), TextSpanHelper Reduce/RemoveOverlap (174-Vormerkung), MutateInput-Default-Garantie (IsFileInMutateScope-Sicherheitsnetz), ADR-025-Auto-Bump (MutationProfile/Level), MSBuild-Restore-Pflicht-Memory. Register _docs/analysis/sprint_175_findings.md batch-weise committet. Teil des Programms 173–178 (Issue #276). Kein Tag (Findings-only)."
branch: "feature/175-analysis-c-initialisation-config"
started_at: "2026-06-11"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 175 (360°-Analyse C: Initialisation/Utilities/Solutions/Configuration)

## Kontext

Sprint 174 (Analyse B) geschlossen: `e6e7a5b` auf main, 39 Findings, Issues #282 (P1) – #288.
Programm-Issue #276 trägt weiter. Methode unverändert: Volltext-Lektüre je Datei (360°-Anspruch),
Serena-Server steht dieser Session projektfremd nicht zur Verfügung (dokumentierter Fallback,
vgl. Sprint 170) — für Ganzdatei-Analyse ist Read ohnehin das Mittel der Wahl.

## Scope Sprint 175 (98 Dateien)

| Block | Dateien | Schwerpunkte |
|-------|---------|--------------|
| Initialisation/ | 20 | InputFileResolver (ADR-052/#270-Areal), TimeoutValueCalculator + InitialTestProcess (#273), InitialBuildProcess, NugetRestoreProcess, ProjectOrchestrator, CsharpProjectComponentsBuilder, FolderCompositeCache |
| Utilities/ | 15 | TypeBasedStrategy (Registry-Ordnung), TextSpanHelper (Reduce/RemoveOverlap), MSBuild-Schicht (Loader/Analysis/Extensions), EmbeddedResourcesGenerator, FilePathUtils, HeartbeatLogger |
| Solutions/ | 3 | SolutionFile/Provider (.slnx-Parsing) |
| Configuration/ | 60 | StrykerOptions/StrykerInputs, MutateInput (Default-`**/*`-Garantie), MutationProfile-/MutationLevelInput (ADR-025), FilePattern/ExclusionPattern, BasicIdProvider, 46 Inputs |

## Status

- [x] Branch + state.md + Register angelegt
- [ ] Batch-Lektüre (Pflicht-Schwerpunkte zuerst)
- [ ] Verifikations-/Probe-Phase, Issues, Register-PR, Close (kein Tag)
