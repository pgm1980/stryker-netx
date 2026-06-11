# 360°-Analyse — Sprint 176: Test-Runner-Kette (Findings-Register)

> **Programm:** Sprints 173–178, Findings-only (Issue #276). Status-Schema und Severity
> wie Vorsprints: `VERDACHT` / `BESTÄTIGT` / `ENTKRÄFTET` / `NOTIZ`; P0–P3.
> Finding-Präfix dieses Sprints: **I-NN**.
>
> **Pflicht-Schwerpunkte (Carry-over + offene Issues):**
> 1. **#274 Root-Cause:** „Failed to start test server" für das MTP-Dogfood-Modul (ubuntu).
>    Hypothesen-Hierarchie aus dem Issue: (a) Config-Fehler — xunit-Testprojekt OHNE
>    `<UseMicrosoftTestingPlatformRunner>` + vendored `test-runner: mtp`-Key; (b) Exe-vs-DLL-Start
>    (`dotnet exec` nötig?); (c) IPC-Handshake. Schnellcheck Config+csproj VOR der Tiefenlektüre.
> 2. **G-23 (174-Vormerkung):** MTP-File-Control-Staleness — MutantControl re-readet nur bei
>    `LastWriteTimeUtc.Ticks`-Änderung; jetzt die SCHREIB-Seite: wie/wie oft schreibt der
>    MTP-Runner das Mutant-File? Tick-Granularitäts-Kollision real?
> 3. **CoverageAnalyser-Enumeration (G-32-Vormerkung):** `runner.CaptureCoverage` — lazy
>    IEnumerable? CoverageAnalyser enumeriert 3× (Sum/Aggregate/Any).
> 4. **H-13b:** stale „MTP not supported"-Warnung in InitialisationProcess trotz
>    `--test-runner mtp` — Einordnung + Fix-Richtung.
> 5. **#273-Rest:** TestRunResult.Duration-Aggregation (Summe vs. Wall) runner-seitig.
> 6. **G-15-Anschluss:** sessionTimedOut-/forceSingle-Pfade, VsTest-Timeout-Discard-Semantik.

## Abdeckungs-Protokoll

| Batch | Dateien | Status |
|-------|---------|--------|
| 0 | #274-Schnellcheck: stryker-config.json (MTP-Modul) + Stryker.TestRunner.MicrosoftTestPlatform.Tests.csproj | ✅ Root-Cause bestätigt |
| 1 | Stryker.TestRunner komplett (5/5): TestIdentifierList.cs, TestSet.cs, WrappedIdentifierEnumeration.cs, TestRunResult.cs, CoverageRunResult.cs (+ Aufrufer-Kartierung via Grep src/tests) | ✅ gelesen |

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| **I-01** | **BESTÄTIGT (Schwerpunkt 1 / #274 Root-Cause)** | **P2** | src/Stryker.TestRunner.MicrosoftTestPlatform/stryker-config.json + tests/….Tests.csproj | **#274 ist ein Config-Fehler, kein Code-Bug:** Dogfood-Config setzt `"test-runner": "mtp"`, aber das Test-Projekt ist klassisches xunit (Microsoft.NET.Test.Sdk + xunit.runner.visualstudio, CLAUDE.md-Stack) — KEIN Microsoft.Testing.Platform-Paket, kein MTP-Entry-Point → DLL kann nicht als MTP-Server starten → „Failed to start test server" ist die korrekte Folge. Key stammt aus Upstream-Vendoring (dort war das Testprojekt MTP-basiert). Shovel-ready Einzeiler (Key entfernen → VSTest-Default → Nightly 11/11); auf #274 kommentiert. Sekundär: Startstrecke sollte „Ziel ist kein MTP-Projekt" sprechend diagnostizieren (wird bei AssemblyTestServer-Lektüre präzisiert) |
| **I-02** | **BESTÄTIGT (Code-Lektüre; ruhend)** | **P2** | TestRunner/Tests/TestIdentifierList.cs:44 | **`Contains` ist INVERTIERT:** `IsEveryTest \|\| _identifiers?.Contains(testId) is false` — liefert true ⟺ Id NICHT in der Menge (korrekt wäre `is true`; Upstream-Pendant prüft positiv → Port-Regression). Blast-Radius HEUTE null: kein produktiver Aufrufer in src (AnalyzeTestRun/Analyser nutzen ContainsAny/Intersect/IsIncludedIn, die intern set-basiert arbeiten), keine Test-Abdeckung — aber WrappedIdentifierEnumeration.ContainsAny/IsIncludedIn delegieren auf `other.Contains` (ebenfalls ohne Live-Receiver-Pfad) und JEDER künftige Aufrufer (Reporter-Ports!) erbt invertierte Semantik. Schlafende Landmine auf öffentlicher API |
| I-03 | NOTIZ | P3 | TestRunner/Tests/WrappedIdentifierEnumeration.cs | Cluster: (a) `MergeList` behandelt `GetIdentifiers() is null` als Every-Marker — TestIdentifierList liefert aber NIE null (EveryTest → `[]`) → `Merge(wrapped, EveryTest-TIL)` kollabierte Every zu dessen leerer Id-Liste (kein Live-Pfad: Wrapped-Receiver-Merge wird nur runner-intern mit konkreten Sets genutzt); (b) `_identifiers` unvalidiert (null → NRE in Count/Contains) und potenziell lazy (Mehrfach-Enumeration je Count/Contains-Aufruf); (c) `Excluding` wirft NotSupported (ehrlich) |
| I-04 | VERDACHT (VsTest-Batch verifizieren) | P3 | TestRunner/Results/TestRunResult.cs:31 | Rich-Ctor filtert `TestDescriptions` via `executedTests.GetIdentifiers().Contains(...)` — für EveryTest liefert GetIdentifiers `[]` → Descriptions LEER trotz „alle ausgeführt". Konsumenten: ProjectMutator.EnrichTestProjectsWithTestInfo (Initial-Run!). Prüfen, ob der VsTest-Runner dem Initial-Result je EveryTest übergibt |
| I-05 | NOTIZ | P3 | TestRunner/Results/CoverageRunResult.cs:28–35 | Leaked-Zuweisung ÜBERSCHREIBT (=, nicht \|=) einen ggf. zuvor gesetzten Static-Flag — statisch+geleakt mit Exact-Confidence wird zu NeedEarlyActivation OHNE Static. Gemildert: generierungszeitliches `IsStaticValue` (G-32-Notiz) hält die Static-Behandlung; `Merge` nutzt korrekt \|= |
| I-06 | NOTIZ | P3 | TestIdentifierList.Excluding:85 + CoverageAnalyser:52–54/133 | `Excluding` wirft auf EveryTest — CoverageAnalyser ist nur BY CONSTRUCTION sicher (EveryTest entsteht exakt dann, wenn failedTests leer ist → Excluding nimmt den IsEmpty-Frühausstieg). Fragiles Invarianten-Paar über Dateigrenzen; Kommentar-würdig |

## Detail-Einträge
