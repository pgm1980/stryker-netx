# Status & Roadmap (Stand 2026-06-12, nach Sprint 179 / v3.3.4)

> Diese Memory ist der EINZIGE Serena-Ort für volatilen Stand — bei Sprint-Close nur HIER
> aktualisieren. Führend bleibt `.sprint/state.md`; bei Widerspruch gilt state.md.

## 360°-Analyse-Programm (Sprints 173–178) — abgeschlossen
- ~430 Dateien Volltext, **139 Findings** (F/G/H/I/J-Register unter `_docs/analysis/`),
  12 Live-Proben, Synthese + priorisierter Backlog: `_docs/analysis/sprint_178_synthesis.md`
- 5 Befund-Cluster: CE-Noise-Default-Profil, Crash/Hang-Robustheit, Races, Config-Reichweite, Score-Integrität
- Probe-Infrastruktur: Scratch-Projekt `%TEMP%/stryker-probe-174` (ProbeLib + Tests; Class1 =
  Pattern/Switch/Static-Shapes, Class2 = C#14-out-Lambda); Läufe gegen lokale CLI via
  `dotnet run -c Release --project src/Stryker.CLI/Stryker.CLI.csproj -- --reporter json --mutate "**/ClassN.cs"`

## Fix-Fahrplan 179–184 (aus der Synthese)
| Sprint | Thema | Status |
|--------|-------|--------|
| 179 | Quick-Wins + P1 | ✅ **v3.3.4 shipped** — #282 P1, #294, #295, #283, #292, #273, #274, H-05/J-11; ADR-053. G-01 als Fehldiagnose ENTKRÄFTET (needReturn-Pfad liefert Ending-Return; Proben-Block-CEs = G-30-Rollback-Kollateral, echte Ursache #284/#278) |
| 180 | CE-Noise I | **IN PROGRESS (Branch feature/180-ce-noise-1, gestartet 2026-06-12)**: #284 (designation-aware Store-Level + ContainsDeclarations-Erweiterung), #278 (Designation-Skip), #285 (TrackValue-Shape-Skip + Property-Gate). Vorab: Dogfood-Manifest-Pin 3.3.2→3.3.4 (`.config/dotnet-tools.json`). Erfolgsmaß: Probe-1-CE-Rate 62,5 %→<30 %, keine „mutant −1"-Rollback-Runde |
| 181 | Score-Integrität | #286 Geister-Mutanten, G-17 AreEquivalent-Filter #0, G-19, J-01 |
| 182 | Robustheit | #277-Schicht um mutator.Mutate, #288 Loop-Bound, #297 Hang-Epic, Parse-Guards |
| 183 | Config & Nebenläufigkeit | #290 Workspace-Props, #291 Multi-TFM, #296 Init-Races, #299 Branch-Matching, #300 SSE |
| 184 | Backlog-Rest | #279-Checkliste (Emissions-Disziplin), #280, #287 (+BenchmarkDotNet), H-25, #302-Rest |

## Offene Issues (Fix-Backlog)
P1-Klasse: #277 (RegexMutator-Crash + fehlende Robustheitsschicht), #278 (P1-Kandidat).
P2: #279 (Epic), #280, #284–#288, #290–#291, #296–#297, #299–#300. Sammelliste: #302.
Geschlossen in 179: #273, #274, #282, #283, #292, #294, #295.

## Sprint-180-Symbol-Einstiege (Serena)
- #284: `RoslynHelper/ContainsDeclarations` (src/Stryker.Core/Helpers/RoslynHelper.cs) +
  Aufrufer `ExpressionSpecificOrchestrator/StoreMutations`, `InvocationExpressionOrchestrator/StoreMutations`;
  Pattern-Orchestrator: generischer `NodeSpecificOrchestrator<PatternSyntax,PatternSyntax>` in
  `CsharpMutantOrchestrator/BuildOrchestratorList`
- #278: `IsPatternExpressionMutator` (src/Stryker.Core/Mutators/)
- #285: `StaticInitializerMarkerEngine/PlaceValueMarker` (Instrumentation/) +
  `ExpressionBodiedPropertyOrchestrator/OrchestrateChildrenMutation` (ungegateter Property-Pfad)
