# Status & Roadmap (Stand 2026-06-12, nach Sprint 180 / v3.3.5)

> Diese Memory ist der EINZIGE Serena-Ort für volatilen Stand — bei Sprint-Close nur HIER
> aktualisieren. Führend bleibt `.sprint/state.md`; bei Widerspruch gilt state.md.

## 360°-Analyse-Programm (Sprints 173–178) — abgeschlossen
- ~430 Dateien Volltext, **139 Findings** (F/G/H/I/J-Register unter `_docs/analysis/`),
  12 Live-Proben, Synthese + priorisierter Backlog: `_docs/analysis/sprint_178_synthesis.md`
- 5 Befund-Cluster: CE-Noise-Default-Profil, Crash/Hang-Robustheit, Races, Config-Reichweite, Score-Integrität
- Probe-Infrastruktur: Scratch-Projekt `%TEMP%/stryker-probe-174` (ProbeLib + ProbeLib.Tests; Class1 =
  Pattern/Switch/Static-Shapes, Class2 = C#14-out-Lambda); Läufe gegen lokale CLI via
  `dotnet run -c Release --project src/Stryker.CLI/Stryker.CLI.csproj -- --reporter json --mutate "**/ClassN.cs"`
  — **aus `ProbeLib.Tests/` starten** (vom Root findet Stryker kein csproj); Auswertung `python dump_report.py`

## Fix-Fahrplan 179–184 (aus der Synthese)
| Sprint | Thema | Status |
|--------|-------|--------|
| 179 | Quick-Wins + P1 | ✅ **v3.3.4** — #282 P1, #294, #295, #283, #292, #273, #274, H-05/J-11; ADR-053. G-01 ENTKRÄFTET |
| 180 | CE-Noise I | ✅ **v3.3.5 shipped 2026-06-12** — #278 (IsPatternExpressionMutator skippt designation-bindende Patterns; `ContainsBindingDesignation`), #284a (neuer `PatternSyntaxOrchestrator` + `IsInsideVariableBindingIsPattern`-Check in ExpressionSpecificOrchestrator → Block-Lift), #284b (ContainsDeclarations + SingleVariableDesignation), #285a (`MutantPlacer.CanHostValueMarker`: target-typed nie / konstant+mutationsfrei nie / User-Code immer), #285b (Property-Pfad auf MustInjectCoverageLogic gegated). ADR-054. **Probe-1: CE 62,5 % → 0 %**, 0× „mutant −1". Release-Gate-Hotfix: transitiver Pin **MessagePack 2.5.301** (GHSA-hv8m-jj95-wg3x in 2.5.198 via StreamJsonRpc 2.24.84; NU1903 killte ersten Release-Run; Tag/Release per Reparatur-Sequenz auf Hotfix-Merge d0756a4 umgesetzt) |
| 181 | Score-Integrität | **IN PROGRESS (Branch feature/181-score-integrity, gestartet 2026-06-12)**: #286/G-15 (Geister-Mutanten: Drop-Pfad ReplaceChildrenValidated muss Mutant-IDs aus Subtree-Annotationen sammeln → CompileError statt False-Survivor/NoCoverage), G-17 (generischer No-op-Filter #0 via SyntaxFactory.AreEquivalent — IdentityArithmeticFilter ist shape-beschränkt, IsMutantDuplicate dedupt nur gegen andere Mutanten), G-19 (equivalence-Filter droppt via continue OHNE Spur → Ignored-Mutant mit ResultStatusReason "equivalent: <FilterId>", Upstream-Parität), J-01 (JsonMutant.Status serialisiert schema-fremdes „Pending" im Failed-to-test-Pfad) |
| 182 | Robustheit | #277-Schicht um mutator.Mutate, #288 Loop-Bound, #297 Hang-Epic, Parse-Guards |
| 183 | Config & Nebenläufigkeit | #290 Workspace-Props, #291 Multi-TFM, #296 Init-Races, #299 Branch-Matching, #300 SSE |
| 184 | Backlog-Rest | #279-Checkliste (Emissions-Disziplin), #280, #287 (+BenchmarkDotNet), H-25, #302-Rest |

## Offene Issues (Fix-Backlog)
P1-Klasse: #277 (RegexMutator-Crash + fehlende Robustheitsschicht).
P2: #279 (Epic), #280, #286–#288, #290–#291, #296–#297, #299–#300. Sammelliste: #302.
Geschlossen in 179: #273, #274, #282, #283, #292, #294, #295. In 180: #278, #284, #285.

## Sprint-180-Erkenntnisse (für Folge-Sprints relevant)
- **Injektions-Mechanik** (verifiziert): Expression-Stores AGGREGIEREN über geschachtelte
  Expressions (ein Store je Kette); `MutationStore.Leave()` degradiert Pending-Mutationen
  auf das nächsthöhere Store-Level — Level-Routing MUSS zur Store-Zeit via
  `FindControl(Block)` passieren, nicht beim Bubbling. Block-Inject dupliziert den ganzen
  umgebenden Block (self-contained Kopien, eigene Scopes).
- `ConstantPatternSyntaxOrchestrator` blockt Inline-Injection (BlockInjection-Counter);
  Pattern-interne Mutationen tauchen am nächsten injektionsfähigen Expression-Slot auf.
- `ContainsNodeThatVerifies` schließt Lambda-/LocalFunction-Bodies aus → Designation-Checks
  erben Scope-Korrektheit automatisch.
- `MutantContext.TrackValue` setzt thread-static depth → markiert auch TRANSITIV aufgerufene
  Mutanten als static-covered. Marker-Skips dürfen Invocation-/MemberAccess-Initializer NIE
  erfassen (Pin-Test ShouldPlaceStaticMarkerOnInitializerExecutingUserCode).
- Probe-1-Restklasse: RelationalPattern-Konstanten in designation-FREIEN Patterns
  kompilieren als Ternary am is-Ausdruck weiter (kein Fix nötig).
- Release-Gate-Klasse (2× passiert, Sprint 170 + 180): neue NuGet-Advisory auf transitiver
  Dep bricht Locked-Mode-Restore im Release-Workflow (NuGetAudit + TWAE), lokaler Build
  merkt nichts (Audit-Cache). Bei Release-Failure ZUERST NU190x prüfen; Fix-Muster:
  zentraler Transitive-Pin in Directory.Packages.props (CentralPackageTransitivePinningEnabled
  ist aktiv) + `dotnet restore --force-evaluate` + Locked-Mode-Verifikation; Tag/Release
  per CLAUDE.md-Reparatur-Sequenz auf den Hotfix-Merge umsetzen.
