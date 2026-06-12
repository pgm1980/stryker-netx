# Task Completion Checklist (Stand v3.3.4 / Fix-Ära)

Vor jedem „fertig":

## TDD-Disziplin (Fix-Sprints 179–184)
- [ ] Red-Test ZUERST, abgeleitet aus dem Register-Befund (`_docs/analysis/…`), mit der
      exakten Ziel-Exception/-Assertion — Red-Lauf nachweisen
- [ ] Bei „Fix wirkt nicht im Red" → STOPP: mögliche Fehldiagnose (Lehre G-01: needReturn-Pfad
      machte den geplanten Fix zum No-op; Red-Verifikation fing das VOR dem Ship)
- [ ] Wo Probe-Infrastruktur existiert (`%TEMP%/stryker-probe-174`): Probe gegen LOKALE CLI
      als End-to-End-Beweis (z. B. Exit-Code, JSON-Statuses, CE-Rate-Vergleich)

## Build & Test
- [ ] `dotnet build` 0/0 (TWAE; Analyzer-Fallen s. code_style-Memory: S125/MA0051/MA0002/MA0006)
- [ ] Vollsuite grün; E2E-Erstlauf-Failures → Re-Run vor Schlussfolgerung (Flaky-Klassen dokumentiert)
- [ ] FluentAssertions; kein `#pragma` ohne Begründungskommentar; kein `<NoWarn>`

## Sicherheit & Architektur
- [ ] `semgrep scan --config auto` auf geänderte Dateien: 0 Findings
- [ ] Neue Namespaces → ArchUnitNET; neue öffentliche APIs → XML-Doc; `sealed` wo passend
- [ ] `ConfigureAwait(false)`; Boundary-Catch `when (ex is not OperationCanceledException)`

## Serena-Pflicht (CLAUDE.md + User-Feedback)
- [ ] Symbol-Navigation über `find_symbol`/`get_symbols_overview`/`find_referencing_symbols`
- [ ] Edits symbolisch (`replace_symbol_body`, `insert_*_symbol`) wo ganze Symbole betroffen
- [ ] Nach Subagent-Rückkehr: Build/Test/Semgrep selbst re-verifizieren + Symbol-Spot-Check

## Ship-Zyklus (Sprint mit Code-Änderungen)
- [ ] PR mit `closes #N`-Keywords → Squash-Merge (Subject endet `(#NNN)`)
- [ ] **Tag auf den MERGE-Commit** (nie vorher!) → push → `gh release create` → release.yml grün
- [ ] Issues-Querverweise: Epics kommentieren statt schließen; #302-Checkboxen abhaken
- [ ] ADR + Änderungshistorie in architecture_specification.md
- [ ] Closing-PR: `.sprint/state.md`-Flags final; Claude-MEMORY aktualisieren;
      Serena-Memory `project_status_and_roadmap` fortschreiben (NUR diese ist volatil)
- [ ] Folge-Sprint-Hinweis: Dogfood-Manifest-Pin (`.config/dotnet-tools.json`) nachziehen
