# 360°-Analyse — Sprint 174: Mutations-Pipeline (Findings-Register)

> **Programm:** Sprints 173–178, Findings-only (Issue #276). Status-Schema und Severity
> wie Sprint 173 (`sprint_173_findings.md`): `VERDACHT` / `BESTÄTIGT` / `ENTKRÄFTET` /
> `NOTIZ`; P0–P3.
>
> **Pflicht-Schwerpunkte (aus Sprint-173-Register):**
> 1. **F-14:** `BaseFunctionOrchestrator.cs:117–142` — AddEndingReturn läuft im
>    No-Mutations-Pfad, im Mutations-Pfad hängt alles an `context.InjectMutations(...,
>    !returnType.IsVoid())`; Probe bewies CS0161-CEs für Block-Mutanten. Wirkkette klären.
> 2. **F-10:** Hosting von Deklarations-Level-Mutationen (MethodBodyReplacement 1/1 CE,
>    GenericConstraint) — wie platziert/instrumentiert der Placer Member-Replacements?
> 3. **#277-Folge:** `CsharpMutantOrchestrator.GenerateMutationsForNode:223` ohne
>    try/catch — Robustheits-Frage systematisch (auch Filter-Pipeline, Visitor).
> 4. **F-08:** Equivalence-Pipeline — Erweiterbarkeit Richtung Operator-/Flow-Typfehler.
> 5. **F-25:** Fängt ConservativeDefaultsEquality No-op-Returns (`return 0;`→`return 0;`)?

## Abdeckungs-Protokoll

| Batch | Dateien | Status |
|-------|---------|--------|
| 1 | MutationContext.cs, Instrumentation/EndingReturnEngine.cs (+ BaseFunctionOrchestrator aus 173 vorgelesen) | ✅ gelesen |

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| **G-01** | **BESTÄTIGT (Mechanik komplett)** | **P2** | Instrumentation/EndingReturnEngine.cs:17–26 + CsharpNodeOrchestrators/BaseFunctionOrchestrator.cs:140 | **F-14-Auflösung:** AddEndingReturn würde den CS0161-Schaden des if/else-Wraps heilen (nach Wrap ist `Statements.Last()` ein IfStatement, nicht mehr Return → Early-Exit Zeile 19 griffe nicht; Zeile 23 fände das Return im else-Zweig) — aber der Mutations-Pfad ruft die Engine NIE auf: Zeile 140 wendet nur `InjectOutParametersInitialization` auf das Inject-Ergebnis an; AddEndingReturn existiert ausschließlich im No-Mutations-Pfad (Zeile 130–135). Fix-Ort eindeutig: `SwitchToThisBodies(targetNode, MutantPlacer.AddEndingReturn(newBody, returnType), null)` im Mutations-Pfad. Erklärt Block-CS0161 (Probe 2/2) und vermutlich Reporter-D-CS0161-Anteil |
| G-02 | NOTIZ | P3 | MutationContext.cs | Sauber geschichtetes Kontext-Design (Stack-basierte MutationStore, Block/Member-Kind-Kontexte, Comment-Filter-Vererbung) — keine Auffälligkeit im Kontext-Handling selbst |

## Detail-Einträge
