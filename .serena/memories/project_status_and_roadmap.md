# Status & Roadmap (Stand 2026-06-15, nach Sprint 187 / v3.3.12)

> **FAHRPLAN 179–184 KOMPLETT — das 360°-PROGRAMM (Sprints 173–184) IST ABGESCHLOSSEN.**
> 12 Sprints: 6 Analyse (~430 Dateien, 139 Findings, 12 Proben), 6 Fix-Sprints mit
> 6 Releases (v3.3.4–v3.3.9). ~20 von 23 Programm-Issues geschlossen.
> Offener kuratierter Rest: #279-Folgebatches (Block/F-14, AsSpanAsMemory/B.1+B.2,
> AsyncAwait, F-08-Filter), #302-Restposten (G-30, G-05, H-10, H-11, H-15, H-20, J-08,
> J-15a, G-34, H-13b, J-10, Invarianten-Kommentare).

## 🆕 Externer-360°-Test-Fix-Block (Sprints 185–187) — ✅ ABGESCHLOSSEN (Stand 2026-06-15)
Ein externes Testmanagement-Projekt fand per Black/White-Box gegen v3.3.9 **7 Bugs** (Berichte `_bug_reporting/BUG_REPORT.md` + `UPSTREAM_ISSUES.md`; Repros byte-genau unter `_bug_reporting/testing/`). Roadmap via MAXential+ToT (Variante B, risiko-isoliert):
- **Sprint 185 (✅ v3.3.10 shipped 2026-06-15)** — Äquivalenz-Filter-Cluster, Merge d4d6c3b (#317), ADR-059. EQF-001 (High): `RoslynSemanticDiagnosticsEquivalenceFilter` verwarf Methodengruppen (StringMethod immer, Linq bei datei-level using) als equivalent — Fix `&& CandidateSymbols.IsDefaultOrEmpty` (beide Pfade; MAXential+ToT A1 0.93). **E2E bug01: 8 Mutanten getestet statt ge-Ignored, Linq xs.Max Killed** (Blast-Radius; Unit-Harness deckt Linq-Fall nicht ab). EQF-003 (Med): `ConservativeDefaultsEqualityFilter` positions-normalisiert (`FlipComparison`; nur 2/8 unsigned-Kombis äquiv — Operand-Position ÜBER Report hinaus). EQF-002 (Low): `IdentityArithmeticFilter` struktur-basierte right-identity (`0-x`/`1/x`-Fallen ausgeschlossen, über Report hinaus); `IdempotentBooleanFilter` ehrlich als konzeptuell inert dokumentiert (MAXential+ToT 0.92, kein spekulativer Fix — kein realer äquivalenter Boolean-Mutant existiert). 3 bug-pinnende Tests umgeschrieben. Build 0/0, Core 567/567, Semgrep 0.
- **Sprint 186 (✅ v3.3.11 shipped 2026-06-15)** — Quick-Wins, Merge eaf2e84 (#319), ADR-060. MAT-001 (Med): `MathMutator` Member-Pfad prüfte `ContainingType` des Receiver-Typsymbols System.Math (= null) statt des Symbols → `Math.X(...)` nie mutiert mit Semantic-Model; Fix `symbol?.ToString()`. Unit-Tests fuhren Null-Model-Fallback (maskierte Bug) → Red mit echtem Model; **E2E bug03: 2 Math-Mutanten (Member-Call gefixt)**. SOL-001 (Low): `IsSolutionContext` an cwd==SolutionDir gebunden → `--solution` aus fremdem cwd ignoriert; Fix `=> SolutionPath != null` (MAXential+ToT A 0.93; SolutionPath nur via --solution gesetzt). RUN-001 (Med): README-Kompat korrigiert (MTP+TUnit roadmapped, #3094/ADR-044). Build 0/0, Core 572/572, Semgrep 0.
- **Sprint 187 (✅ v3.3.12 shipped 2026-06-15)** — INJ-001 SOLO, Merge ef2aece (#321), ADR-061. Architektur-Analyse via MAXential+ToT + Live-Probe (bug02). **Teil A GEFIXT:** `MethodBodyReplacementMutator` zu `TypeAwareMutatorBase<BlockSyntax>` umgebaut (direkter Method-Body-Block, `Type=Mutator.Statement` gegen IgnoreBlockMutantFilter; der naive Block-Retarget scheiterte — von der Live-Probe aufgedeckt!); E2E bug02 'Echo' CompileError-Soft-Fail → KILLED. **Teil B VERTAGT (User-Entscheidung → #279-Epic):** GenericConstraint + GenericConstraintLoosen mutieren Signatur-Constraints, im Laufzeit-Schalt-Modell prinzipiell nicht injizierbar (Orchestrator besucht ConstraintClauses NIE); Roadmap-„Vorbild" Loosen FALSIFIZIERT (0 Mutanten); als nicht-injizierbar dokumentiert (XML-remarks), nicht deaktiviert. **🏁 FIX-BLOCK 185–187 ABGESCHLOSSEN: 6/7 externe Bugs behoben (EQF-001/003/002, MAT-001, SOL-001, RUN-001) + INJ-001 Teil A; INJ-001 Teil B als Modell-Limit dokumentiert.**

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
| 181 | Score-Integrität | ✅ **v3.3.6 shipped 2026-06-12** — G-17 (NoOpMutationFilter als Filter #0, AreEquivalent trivia-insensitiv), G-19 (gefilterte → Ignored „Equivalent mutant (filter: <Id>)", nie injiziert; Red per Stash-Roundtrip bewiesen), #286/G-15 (onMutationsDropped-Callback auf beiden ADR-032-Drop-Pfaden → MutationContext.FlagDroppedMutantsIn → CsharpMutantOrchestrator.FlagDroppedMutants; MutantPlacer.ExtractMutantIds liest MutationId-Annotationen; nur Pending→CompileError; 4 Call-Sites; generischer Geister-Detektor in OrchestrationSlotValidationTests), J-01 (Pending-Reste am Session-Drain in OnMutantsTested → Ignored + Reason). ADR-055. Merge c9c3e11 |
| 182 | Robustheit | ✅ **v3.3.7 shipped 2026-06-12** — #277a (RegexMutator is-Pattern; interpolierte Patterns übersprungen), #277b (SafelyMutate-Guard: Lazy-Materialisierung IM try, WARN+Skip, OCE propagiert), #288 (HasScanProgress-Referenzvergleich + MaxNreScanRounds=5 → CompilationException; RecoverFromEmitNullReference extrahiert), #297a (Pool: _initializationFailure + Signal im finally; RunThis nach MTP-Muster 1s/5min/30s-Log, Fail-fast), #297b (WaitEnd(TimeSpan), 5-Min-Default → Aborted-Pfad), #297c (ResponseListener.Fail mit TrySet-Semantik; FailAllListeners bei Disconnect mit IOException), H-19 (TryParse bool/int, File.Exists vor CreateFromFile, InputException für „no serializer"), G-37b (Baseline Enum.TryParse → unbekannter Status bleibt Pending). ADR-056, Merge 5c94121. **Crash-Probe: Exit 127 → 0** (Class4, Advanced; kein WARN nötig — Wurzel gefixt, Schicht per Unit-Test belegt) |
| 183 | Config & Nebenläufigkeit | ✅ **v3.3.8 shipped 2026-06-12** — #290 (ForProperties-Self-Factory + ConfiguredWorkspace einmal pro Lauf; P-5: Injektion bin\\Release ✓), #291 in ZWEI Schichten (FirstTargetFrameworkFrom + Loader pinnt Outer-Evaluation aufs erste TFM — Probe deckte Schicht 2 „Language not supported: Undefined" auf, Unit-Tests sahen sie nicht; P-6: Exit 1→0, Score 83,33 %), #296 (Initial-Tests sequenziell, Overlap-Detektor-Red; VsTests-Lost-Update entfernt), #299 (exaktes Segment-Matching, master→main-WARN-Fallback, --since-target-Meldung, Kurz-SHA J-05), #300 (Writer-Lock+Snapshot, Listener-Shutdown=Loop-Ende, CloseSse best-effort — AggregateException brach Reporter-Kette, ConcurrentQueue). ADR-057, Merge ace6848 |
| 184 | Backlog-Rest | ✅ **v3.3.9 shipped 2026-06-12 — FAHRPLAN-ABSCHLUSS** — #287/G-22 (HashSet-Drop-in, Benchmark 11,2× bei 10k; GetCoverageData materialisiert einmal für den Reflection-Vertrag; G-24-Handler raus), #280/F-01 (Mutator.Number additiv ans Ende; Type-Pins ergänzt; ignore-mutations-Meldungs-Pin aktualisiert), #279-Batch-1 (UOI-Typ/Schreibbarkeits/NICHT-WERT-SYMBOL-Gates — Namespace/Methodengruppe von der CE-Probe aufgedeckt; ROR-Ordnung nur geordnet, lifted Nullables entpackt; ConstructorNull-Struct-Gate; TypeDrivenReturn-Async-Guard; Doc-F-30 6 Dateien; **CE-Probe 56 %→29,4 %**, Rest = offene Epic-Klassen Block/AsyncAwait), H-25 (Threshold-Effektivwerte), I-11 (MTP-Singleton-Guard; 2 Bug-pinnende Upstream-Tests umgeschrieben). ADR-058, Merge 883ea4c |

## Offene Issues (Fix-Backlog nach Programm-Abschluss)
Offen: #279 (Epic — Batch 1 geshippt; Rest: Block/F-14, AsSpanAsMemory, AsyncAwait, F-08), #302 (P3-Sammelliste, teilabgehakt: H-05/J-11/H-25/I-11/G-24 ✓).
Geschlossen in 179: #273/#274/#282/#283/#292/#294/#295 · 180: #278/#284/#285 · 181: #286 · 182: #277/#288/#297 · 183: #290/#291/#296/#299/#300 · 184: #287/#280.

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
