# 360°-Analyse — Sprint 177: Reporters/Baseline/CLI/Abstractions/Helpers (Findings-Register)

> **Programm:** Sprints 173–178, Findings-only (Issue #276). Status-Schema und Severity
> wie Vorsprints: `VERDACHT` / `BESTÄTIGT` / `ENTKRÄFTET` / `NOTIZ`; P0–P3.
> Finding-Präfix dieses Sprints: **J-NN**. Letzter Lese-Sprint vor der 178er-Synthese.
>
> **Pflicht-Schwerpunkte (Carry-over):**
> 1. **I-02-Aufrufer-Watch (#294):** Rufen Reporter `ITestIdentifiers.Contains` (invertiert!)?
>    Json-Serialisierung von CoveringTests/KilledBy prüfen.
> 2. **G-37-Baseline-Kette:** Provider-HTTP/Auth (.Result-Muster), Fremd-JSON-Deserialisierung
>    (Enum.Parse-Robustheit der gesamten Kette, nicht nur des Filters).
> 3. **H-27:** GitDiffProvider — `master`-Default-Verhalten bei main-Repos (Fehlerpfad/Meldung).
> 4. **RoslynHelper KOMPLETT** (bisher nur Auszüge): IsAStringExpression (#277-Wurzel),
>    BuildDefaultExpression (G-25-Fix-Ort), ScanChildStatements, IsVoid-Null-Toleranz.
> 5. **SyntaxSlotValidator KOMPLETT:** ADR-028/-032-Herz — Slot-Prüf-Mechanik.
> 6. **G-15-Reporter-Anschluss:** Wie stellen Reporter CompileError/Ignored/Pending dar
>    („reporters can classify"-Behauptung aus OrchestrationHelpers)?
> 7. **CLI:** Program/MSBuildLocator (H-17-Areal), Exit-Code-Pfade, Config-Parsing-Robustheit,
>    NugetFeedClient-Netzwerkverhalten.

## Executive Summary (Sprint-Abschluss 2026-06-11)

**Scope: 157 Dateien (+2 Clients-Nachtrag); ~95 logik-tragende voll gelesen, ≈62 vertragsfreie
Trivia klassifiziert.** **15 Findings (J-01…J-15), 2 neue Issues (#299, #300).**

**Alle 7 Pflicht-Schwerpunkte aufgelöst:**
- **I-02-Watch → J-01 (entlastend):** Kein Reporter ruft das invertierte `Contains` — #294 bleibt ruhend ✓
- **G-37-Kette → J-07/J-14:** Provider-Deserialisierung schema-tolerant; `.Wait()`-Muster kartiert; Broadcast-Lock entschärft die DashboardClient-Batch-Race (Invariante!)
- **H-27 → J-04 (#299, verschärft):** Substring-Branch-Matching → stiller falscher Diff-Base; master-Default-Fehlpfad mit Jargon-Meldung
- **RoslynHelper → J-02:** ScanChildStatements blind für Catch/Finally (G-01-Nebenecke); ContainsNodeThatVerifies vorbildlich scope-korrekt
- **SyntaxSlotValidator → J-03:** Force-Traverse fängt typed-list-Casts (Bug-9 ✓), Property-Access-Shapes = dokumentiertes Rest-Fenster
- **G-15-Reporter-Anschluss → J-01:** „Pending" erreicht den Final-Report als schema-fremder Status; CE/Ignored sauber dargestellt
- **CLI → J-10/J-11/J-15:** NoTestProjects→Exit 0 (CI-grün-Illusion), selbst-neutralisierter Config-Check, -V-Dualsemantik

**Top-Funde:**
- **J-04/P2 (#299):** `--since-target main` kann „maintenance" treffen — stille falsche Mutantenselektion
- **J-06/P2 (#300):** Realtime-HTML-Races (Writer-Liste/Queue) + AggregateException bricht Reporter-Kette → Json/Baseline-Report-Verlust
- **Positiv-Bilanz:** 3 Cross-Layer-Rettungen verifiziert (Broadcast-Lock, Provider-Robustheit, FileConfigReader-Guards); MsBuildHelper/ProcessUtil/NugetFeedClient solide

## Abdeckungs-Protokoll

| Batch | Dateien | Status |
|-------|---------|--------|
| 1 | Helpers komplett: RoslynHelper (Voll-Lektüre), SyntaxSlotValidator, MsBuildHelper, ProcessUtil (4) | ✅ |
| 2 | JSON-Reporter-Kern: JsonMutant, JsonReport, JsonReporter, JsonReportSerialization + I-02-Aufrufer-Grep über Reporters/ | ✅ |
| 3 | HtmlReporter, DashboardReporter, DashboardClient (+IDashboardClient — Scope-Nachtrag Clients/), GitInfoProvider, GitDiffProvider, DiskBaselineProvider | ✅ |
| 4 | Azure-/S3-/Dashboard-Provider + Factory, BaselineMutantHelper, BaselineReporter, BroadcastReporter, ReporterFactory, FilteredMutantsLogger, ProgressBarReporter, SseServer, RealTimeMutantHandler | ✅ |
| 5 | CLI-Kern: Program, StrykerCli (komplett), StrykerNugetFeedClient, ConfigBuilder, FileConfigReader, LoggingInitializer; Core-Infrastructure/ServiceCollectionExtensions | ✅ |
| 6 | Reporter-Rest: MarkdownSummary, ClearTextTree, ClearText, ConsoleDot, Progress-Quartett, CrossPlatformBrowserOpener, SourceFile+Converter-Cluster, Location/Position | ✅ |
| 7 | CLI-Rest: CommandLineConfigReader (komplett), FileConfigGenerator, FileBasedInput, SseEvent, JsonTestFile; Azure-/S3-Tails, DiffResult; Abstractions-Verträge: ITestIdentifiers, Mutation, MutantStatus, MutationTestingRequirements, OptimizationModes, MutationProfile, ITestRunner, TestDescription | ✅ |
| — | **Abdeckungs-Ehrlichkeit:** ~95 logik-tragende Dateien voll gelesen; Residuum ≈62 vertragsfreie Trivia (Ein-Zeilen-Interfaces, Enum-Listen, DTO-Records, Converter-Klone des gelesenen Musters, generierte SerializerContexts) — klassifiziert, nicht zeilenweise gelesen. Bugs leben in Logik; Trivia-Restposten ggf. Stichprobe in 178 | ◐ klassifiziert |

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| J-01 | BESTÄTIGT (Schwerpunkt 1 ✓ entlastend) | P3 | Reporters/ (Grep) + Json/SourceFiles/JsonMutant.cs | **I-02-Watch: kein Reporter ruft `ITestIdentifiers.Contains`** — Serialisierung läuft über `GetIdentifiers()` → #294 bleibt ruhend ✓. Nebenbefunde: `CoveredBy/KilledBy` für EveryTest = LEERE Liste (semantisch „alle decken" → Report zeigt nichts); `TestsCompleted`/`Duration` deklariert, nie befüllt; `Status = ResultStatus.ToString()` serialisiert auch **„Pending"** — kein gültiger mutation-report-Schema-Status (erreicht den Final-Report im Failed-to-test-Pfad, G-09-Anschluss) |
| J-02 | BESTÄTIGT (Schwerpunkt 4 ✓) | P3 | Helpers/RoslynHelper.cs:238–258 | **ScanChildStatements ist blind für Catch-/Finally-Körper** — TryStatement fällt in den Default-Case, der nur Statement-KINDER scannt; CatchClause/FinallyClause sind keine Statements → Returns dort unsichtbar. Konsumiert von EndingReturnEngine („Block ohne Return → kein Inject") → Nebenecke der G-01-CS0161-Klasse. Positiv: `ContainsNodeThatVerifies` ist mit Lambda-Scope-Ausschlüssen + Block-Skip semantisch KORREKT designt (Ternary-Duplikation von Block-Lambdas ist scope-sicher) |
| J-03 | NOTIZ (Schwerpunkt 5 ✓) | P3 | Helpers/SyntaxSlotValidator.cs | ADR-028-Herz gelesen: Force-Traverse (`DescendantNodesAndSelf().ToList()`) + Catch fängt Slot-Mismatches, die bei **typed-list-Casts** während der Enumeration werfen (die historische Bug-9-Klasse ✓) — Mismatches, die erst bei typisiertem **Property-Zugriff** werfen, passieren die Validierung (Rest-Fenster; konsistent damit, dass ADR-032 später Zusatznetze brauchte). Ehrlich dokumentierte Pragmatik |
| **J-04** | **BESTÄTIGT (Code-Lektüre)** | **P2** | Baseline/Providers/GitInfoProvider.GetTargetCommit:87–105 | **Branch-Matching per Substring-`Contains`:** `--since-target main` matcht auch „maintenance"/„domain-feature" (Upstream/Canonical/Friendly, ERSTE Enumeration gewinnt) → **stiller falscher Diff-Base-Commit** → Since-Filter testet falsche Mutantenmenge (Under-/Over-Testing). Dazu H-27-Bestätigung: master-Default auf main-Repo → InputException „No branch or tag or commit found with given target master… Please provide a different GitDiffTarget" (interner Jargon statt `--since-target`-Hinweis). Fix: exaktes Segment-Matching + main/master-Fallback-Kette |
| J-05 | NOTIZ | P3 | GitInfoProvider.RepositoryPath:19 + GetTargetCommit:123 | `Repository.Discover(...).Split(".git")[0]` — „.git" im ELTERN-Pfadnamen („C:\my.gitops\repo") splittet falsch → kaputte Diff-Pfade; Commit-Lookup nur für 40-Zeichen-SHAs (Kurz-SHAs fallen durch zur generischen Fehlermeldung) |
| **J-06** | **BESTÄTIGT (Code-Lektüre)** | **P2** | Html/RealTime/SseServer.cs + RealTimeMutantHandler.cs | **Realtime-HTML-Races:** `_writers` (plain List) wird vom Listener-Task (Add bei Client-Connect) UND von Mutant-Threads (foreach in SendEvent, Remove) unsynchronisiert geteilt — Browser-Connect während Event-Send → „Collection was modified"/Korruption; `_delayedEventQueue` (Queue) analog (Enqueue Mutant-Thread vs. Dequeue-Loop im ClientConnected-Handler). Dazu: Listener-Loop-Fault (ObjectDisposed beim Close-Race) ist unobserved → Realtime stirbt STILL; `CloseSseEndpoint` Task.WaitAll über Writer-Flushes wirft bei disconnected Clients AggregateException → bricht die BroadcastReporter-Kette → nachfolgende Reporter (Json! Baseline!) werden ÜBERSPRUNGEN |
| J-07 | ENTKRÄFTET-Anteil + NOTIZ | P3 | Clients/DashboardClient.cs + BroadcastReporter.cs:45–51 | Vermutete `_batch`-List-Race in PublishMutantBatch ENTSCHÄRFT: BroadcastReporter serialisiert OnMutantTested per `lock` → Single-Writer by construction (Invariante dokumentieren — direkter Reporter-Einsatz ohne Broadcast würde racen). Rest: `.Wait()/.Result`-Kette (G-37-Klasse) blockiert Executor-Threads pro HTTP-Call; URL-Bau ohne Encoding (Branch-Namen mit `#`/`?` brechen); Fehlerpfade sauber gefangen+geloggt ✓ |
| J-08 | BESTÄTIGT | P3 (Perf) | Baseline/Utils/BaselineMutantHelper.cs:21–33 | `GetMutantSourceCode` ruft `CSharpSyntaxTree.ParseText(source)` PRO Baseline-Mutant — Datei mit N Mutanten wird N-mal voll geparst (O(Mutanten × Parse) je Datei im Baseline-Modus). Fix: Tree pro Datei cachen (ein Parse, N Span-Lookups) |
| J-09 | NOTIZ | P3 | ReporterFactory.cs:36–45 | `--reporter all` aktiviert ALLE Reporter inkl. Baseline + RealTimeDashboard bedingungslos: ohne Dashboard-Key wirft schon die Validierung (sauber); MIT Key, aber ohne Baseline-Setup wirft erst `BaselineReporter.OnAllMutantsTested` → `GetCurrentBranchName` (Repository null bei Since=false) → späte InputException NACH dem Testlauf. „All"-Semantik (Baseline/RealTime ausnehmen wie Upstream?) klären |
| J-10 | NOTIZ | P3 | CLI/Program.cs:40–44 + StrykerCli.HandleStrykerRunResult | `NoTestProjectsException` → **Exit 0** — CI ohne gefundene Testprojekte wird still grün (Illusion); Score-NaN → ebenfalls Success. Bewusste Upstream-Parität? Als Verhaltensvertrag dokumentieren oder Exit-Code spendieren |
| J-11 | NOTIZ | P3 | CLI/ConfigBuilder.cs:53 | `File.Exists(defaultConfigFileName)` prüft den NACKTEN Namen (CWD-relativ) statt `defaultConfigFilePath` — nur dadurch korrekt, dass basePath ≡ CWD gesetzt wird (Zeile 26). Selbst-neutralisierter Bug; bricht, sobald basePath je entkoppelt wird |
| J-12 | NOTIZ | P3 | Helpers/ProcessUtil/ProcessExecutor.cs + MsBuildHelper.cs | ProcessExecutor: `WaitForExit(timeout)`-Overload wartet nicht auf Async-Output-Drain (Klassiker → Tail der Build-Ausgabe kann fehlen); `Process.Start` null/Fehlstart → irreführende „long runtime"-OCE-Meldung. MsBuildHelper: ADR-010-Kommandobau für beide Pfade korrekt (`dotnet msbuild` vs. exe; `-c` vs. `/property:`); QuotesIfNeeded-len<3-Mikro (Duplikat zu InitialBuildProcess) |
| J-13 | NOTIZ | P3 | BroadcastReporter.cs:57 + Diverse | `Thread.Sleep(1s)` als Console-Flush-Heuristik vor Final-Reports (Upstream-Erbe). Formatting-Reporter (ClearText/Tree/Markdown/Dots/Progress) sauber (ADR-041-Kompaktspalten ✓); CrossPlatformBrowserOpener: WSL-Pfad ungequotet + ReadToEnd-Newline in PowerShell-Arg (Mikro); SourceFile-Duplikat-Detektion mit WARN ✓ |
| J-14 | NOTIZ | P3 | Baseline-Provider-Quartett + FileConfigReader/LoggingInitializer | Provider robust (S3 NotFound→null, Azure Auth-Fail geloggt, Disk sauber); G-37-Kette: Fremd-JSON-Deserialisierung provider-seitig schema-tolerant (init-Defaults), das Enum.Parse-Risiko bleibt im Filter (bereits registriert). FileConfigReader: Key-Präsenz-Guards korrekt (leere Strings überschreiben nicht); LoggingInitializer: Output-Default + .gitignore-Anlage ✓; NugetFeedClient netzwerk-gehärtet (Catch-all → 0.0.0) ✓ |
| J-15 | NOTIZ | P3 | AzureFileShareBaselineProvider.UploadFileContent:170–178 + CommandLineConfigReader:218 | (a) Azure-Chunk-Upload: Chunk-Fehler wird geloggt, Schleife läuft WEITER → Remote-Baseline mit Loch (korrupte Datei statt Abbruch+Cleanup); (b) `-V`-Doppelbelegung: bare `-V` = Tool-Version (Sprint-148-Konvention), `-V <wert>` = Verbosity-Kurzflag — funktional, aber verwirrende Dualsemantik; (c) `--with-baseline <committish>` schreibt in SinceTarget (Upstream-Design, dokumentationswürdig) |

## Detail-Einträge
