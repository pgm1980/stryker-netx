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

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| H-01 | BESTÄTIGT (Schwerpunkt 1 aufgelöst) | P3 | TimeoutValueCalculator.cs + InitialTestProcess.cs | **#273-Wurzel ist rein test-seitig:** Der Calculator ist deterministische Arithmetik (kein Wall-Clock-Zugriff); `Math.Max(testSessionTime − aggregatedTestTimes, 0)` fängt parallele Runner (Summe > Wall-Zeit) korrekt ab; Timeout-Überschätzung bei Parallel-Suiten geht in die sichere Richtung. Die Flakiness entsteht in der TEST-Assertion gegen reale Stopwatch-Zeit um `Task.Delay(10)`. Fix-Richtung: Zeit-Abstraktion injizieren oder Test-Toleranz weiten (Kommentar auf #273 ergänzt) |
| H-02 | NOTIZ | P3 | InitialTestProcess.cs:16 | `TimeoutValueCalculator` als mutable Property am Prozess ist redundant — der Rückgabewert `InitialTestRun` trägt dieselbe Instanz und ist der konsumierte Pfad (MutationTestProcess nutzt `Input.InitialTestRun.TimeoutValueCalculator`). Bei Mehrprojekt-Läufen Last-Write-wins auf der Property — latent verwirrend, aktuell folgenlos |
| H-03 | NOTIZ (Schwerpunkt 2 aufgelöst) | P3 | Utilities/Helpers/TypeBasedStrategy.cs | **ADR-027-Ordnungs-Annahme bestätigt:** Innerhalb eines ManagedType-Buckets entscheidet Registrierungsreihenfolge (`FirstOrDefault(CanHandle)`), Typ-Auflösung walkt exakter-Typ→BaseType-Kette ✓. Nebenbefunde: (a) Doc-Kommentar behauptet „keeping a cache" — es existiert KEIN Memo-Cache, FindHandler walkt je besuchtem Syntax-Knoten (O(Tiefe) Dictionary-Hits — bei Projektgröße messbar, aber unkritisch); (b) Interface-ManagedTypes wären unerreichbar (BaseType-Walk) — im Roslyn-Syntax-Domain folgenlos, latente Constraint; (c) totes `item != null` im Schleifenkopf |
| H-04 | ENTKRÄFTET (Schwerpunkt 3 aufgelöst — Code sound) | P3 | Utilities/Helpers/TextSpanHelper.cs | **174-Vormerkung aufgelöst: Reduce/RemoveOverlap sind korrekt.** Das `other != default`-Idiom ist in beiden Pfaden benign: in Reduce kann ein echtes `[0,0)`-Match nur Empty-Spans betreffen (finaler `Where(!IsEmpty)`-Filter), in RemoveOverlap matcht `OverlapsWith` (strikt) nie ein Empty-Span → default ≡ not-found. foreach-modify-break-Pattern korrekt (Abbruch vor MoveNext). O(n³)-Restart-Schleifen bei kleinen Pattern-Mengen akzeptabel. `IsComponentExcluded`-Semantik („excluded ⇔ Include-Spans vollständig von Excludes überdeckt") steht damit auf solidem Fundament |

## Detail-Einträge
