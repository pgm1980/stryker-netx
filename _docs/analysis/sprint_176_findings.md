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

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|

## Detail-Einträge
