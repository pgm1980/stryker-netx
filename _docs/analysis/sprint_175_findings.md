# 360°-Analyse — Sprint 175: Initialisation/Utilities/Solutions/Configuration (Findings-Register)

> **Programm:** Sprints 173–178, Findings-only (Issue #276). Status-Schema und Severity
> wie Sprints 173/174: `VERDACHT` / `BESTÄTIGT` / `ENTKRÄFTET` / `NOTIZ`; P0–P3.
> Finding-Präfix dieses Sprints: **H-NN**.
>
> **Pflicht-Schwerpunkte (Carry-over aus 173/174 + offene Issues):**
> 1. **#273-Wurzel:** TimeoutValueCalculator + InitialTestProcess — Wall-Clock-Abhängigkeit
>    der Timeout-Berechnung (Flaky `InitialTestProcess_ShouldCalculateTestTimeout`).
> 2. **TypeBasedStrategy (174-Vormerkung):** Orchestrator-Registry verlässt sich auf
>    „more specific first"-Registrierungsordnung — Bucket-/FindHandler-Semantik verifizieren.
> 3. **TextSpanHelper (174-Vormerkung):** `Reduce`/`RemoveOverlap` — Korrektheit der
>    Span-Arithmetik, auf der `IsComponentExcluded` steht.
> 4. **MutateInput-Default-Garantie:** CsharpMutationProcess.IsFileInMutateScope:187 nennt
>    den include-leeren Fall „Sicherheitsnetz, Validate ergänzt default `**/*`" — Vertrag prüfen.
> 5. **ADR-025-Auto-Bump:** MutationProfileInput/MutationLevelInput — Profil bumpt Level;
>    Gating-Matrix gegen Mutator-Level (173-Erkenntnis: Advanced/Complete erst ab Bump testbar).
> 6. **MSBuild-Schicht:** Restore-Pflicht (Memory: obj/project.assets.json referenzierter
>    Projekte), GetSourceGenerators/GetParseOptions/GetResources (aus CompilingProcess-Sicht),
>    InputFileResolver-Restrisiken nach ADR-052 (#270-Saga: FindMutableAnalyses, Orphans,
>    transitive ProjectReferences, 130ms-Burst-Timing).

## Abdeckungs-Protokoll

| Batch | Dateien | Status |
|-------|---------|--------|
| 1 | TimeoutValueCalculator.cs, InitialTestProcess.cs, InitialTestRun.cs, TypeBasedStrategy.cs, TextSpanHelper.cs, ITypeHandler.cs | ✅ gelesen |
| 2 | InputFileResolver.cs (komplett, 1063 Zeilen — ADR-039/-042/-052-Areal) | ✅ gelesen |

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| H-01 | BESTÄTIGT (Schwerpunkt 1 aufgelöst) | P3 | TimeoutValueCalculator.cs + InitialTestProcess.cs | **#273-Wurzel ist rein test-seitig:** Der Calculator ist deterministische Arithmetik (kein Wall-Clock-Zugriff); `Math.Max(testSessionTime − aggregatedTestTimes, 0)` fängt parallele Runner (Summe > Wall-Zeit) korrekt ab; Timeout-Überschätzung bei Parallel-Suiten geht in die sichere Richtung. Die Flakiness entsteht in der TEST-Assertion gegen reale Stopwatch-Zeit um `Task.Delay(10)`. Fix-Richtung: Zeit-Abstraktion injizieren oder Test-Toleranz weiten (Kommentar auf #273 ergänzt) |
| H-02 | NOTIZ | P3 | InitialTestProcess.cs:16 | `TimeoutValueCalculator` als mutable Property am Prozess ist redundant — der Rückgabewert `InitialTestRun` trägt dieselbe Instanz und ist der konsumierte Pfad (MutationTestProcess nutzt `Input.InitialTestRun.TimeoutValueCalculator`). Bei Mehrprojekt-Läufen Last-Write-wins auf der Property — latent verwirrend, aktuell folgenlos |
| H-03 | NOTIZ (Schwerpunkt 2 aufgelöst) | P3 | Utilities/Helpers/TypeBasedStrategy.cs | **ADR-027-Ordnungs-Annahme bestätigt:** Innerhalb eines ManagedType-Buckets entscheidet Registrierungsreihenfolge (`FirstOrDefault(CanHandle)`), Typ-Auflösung walkt exakter-Typ→BaseType-Kette ✓. Nebenbefunde: (a) Doc-Kommentar behauptet „keeping a cache" — es existiert KEIN Memo-Cache, FindHandler walkt je besuchtem Syntax-Knoten (O(Tiefe) Dictionary-Hits — bei Projektgröße messbar, aber unkritisch); (b) Interface-ManagedTypes wären unerreichbar (BaseType-Walk) — im Roslyn-Syntax-Domain folgenlos, latente Constraint; (c) totes `item != null` im Schleifenkopf |
| H-04 | ENTKRÄFTET (Schwerpunkt 3 aufgelöst — Code sound) | P3 | Utilities/Helpers/TextSpanHelper.cs | **174-Vormerkung aufgelöst: Reduce/RemoveOverlap sind korrekt.** Das `other != default`-Idiom ist in beiden Pfaden benign: in Reduce kann ein echtes `[0,0)`-Match nur Empty-Spans betreffen (finaler `Where(!IsEmpty)`-Filter), in RemoveOverlap matcht `OverlapsWith` (strikt) nie ein Empty-Span → default ≡ not-found. foreach-modify-break-Pattern korrekt (Abbruch vor MoveNext). O(n³)-Restart-Schleifen bei kleinen Pattern-Mengen akzeptabel. `IsComponentExcluded`-Semantik („excluded ⇔ Include-Spans vollständig von Excludes überdeckt") steht damit auf solidem Fundament |
| **H-05** | **BESTÄTIGT (Code-Lektüre; Upstream-Erbe)** | **P3** | InputFileResolver.FindProjectFile:847 | **Extension-Validierung ist ein No-op:** `FileSystem.Path.HasExtension(".csproj")` prüft den LITERAL-String „.csproj" (hat immer eine Extension → true) statt `path`. Bedingung kollabiert zu `File.Exists(path)` → JEDE existierende Datei (.sln, .cs, beliebig) wird als Projektdatei akzeptiert und scheitert erst spät/undurchsichtig in der MSBuild-Analyse statt mit sauberer InputException. Intent: `Path.GetExtension(path) is ".csproj" or ".fsproj"`. Trivialer Fix, guter First-Issue-Kandidat |
| H-06 | VERDACHT | P3 | InputFileResolver.SourceProjectInfos:178 | targetProjectMode-Erkennung vergleicht **rohe** `options.TestProjects`-Strings (potenziell relativ) gegen den ABSOLUTEN normalisierten Working-Dir-Projektpfad — bei relativen `--test-projects`-Angaben matcht nie → Working-Dir-Testprojekt wird nicht als solches erkannt → Filter zeigt auf ein Testprojekt → ADR-039-Layer-2 wirft die irreführende „matches only test project(s)"-Exception statt den Filter zu verwerfen. Sollte gegen `testProjectFileNames` (bereits via FindProjectFile aufgelöst) vergleichen |
| H-07 | NOTIZ | P3 | InputFileResolver.ScanAssemblyReferences:774 + SelectMutableProject:239 | ADR-039-Fix-3 (GetFullPath + OrdinalIgnoreCase) wurde nur auf ScanProjectReferences angewandt — die Assembly-Ref-Seite (774) vergleicht ohne GetFullPath-Normalisierung, SelectMutableProject (239) sogar case-SENSITIV (Ordinal) auf Windows-Pfaden. Praktisch konsistent, weil beide Seiten aus derselben Workspace-Quelle stammen — latente Asymmetrie zur dokumentierten Fix-Begründung |
| H-08 | NOTIZ | P3 | InputFileResolver.BuildSourceProjectInfo:808–825 | F#-Pfad: LogError verspricht „No mutants will be generated" (Weiterlauf-Semantik), direkt danach wirft der Language-Switch `NotSupportedException("Language not supported: Fsharp")` — Log und Verhalten widersprechen sich (Upstream hatte einen FsharpBuilder, netx nicht). Ehrlicher: InputException mit klarer Nicht-Unterstützungs-Meldung |
| H-09 | NOTIZ | P3 | InputFileResolver (diverse) | Kleinkram: `SelectAnalysis` wirft bei leerer Liste mit leerem Projektnamen im Text (674/677); TFM-Vergleich case-sensitiv (690, „NET10.0" vs „net10.0" → unnötiger Fallback+WARN); `LoadProjectAnalyses` nutzt `.GetAwaiter().GetResult()` (CLI-kontextlos benign, G-37-Klasse). Positiv: Catch-all um Projekt-Analyse (567) ist genau die #277-Robustheitsschicht — Analysis-Layer macht es vor; SequentialEnumerableQueue (Dedup + yield-Konsum) sauber |

## Detail-Einträge
