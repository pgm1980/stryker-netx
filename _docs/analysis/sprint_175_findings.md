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

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|

## Detail-Einträge
