---
current_sprint: "171"
sprint_goal: "Dogfood-Configs + Integration-Matrix + Manifest-Nachzug — Item 1: src/*/stryker-config.json (Sprint-24-Vendoring) referenzieren Upstream-Test-Projektpfade (../*.UnitTest.csproj), die im netx-Layout (tests/*.Tests) nie existierten; sichtbar geworden durch ADR-050-Dispatch-Run 27339863955 (alle 11 Jobs 'No .csproj or .fsproj file found'). Item 2: integration-test.yaml (30 Jobs) failt auf jedem PR seit ≥ Sprint 167 am Step 'Run integration tests' — Root-Cause-Diagnose + Fix wenn tractable. Item 3: .config/dotnet-tools.json Pin 3.3.1→3.3.2 nach NuGet-Indexierung + Auto-Bump-Abwägung. Tag-Frage offen: CI-/Config-only-Sprint → Sprint-138-Präzedenz (kein Tag) außer shipped Code ändert sich."
branch: "feature/171-dogfood-configs-integration-matrix"
started_at: "2026-06-11"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 171

## Trigger

Sprint-170-Verifikation (ADR-050): Der erste funktionierende Dispatch-Run des
Nightly-Dogfood brachte alle 11 Jobs erstmals bis in die Stryker-Execution —
und legte dort die nächste Schuldenschicht frei. GitHub-Issue #268.
User-Direktive: Sprint 171 mit den drei Sprint-170-Follow-ups durchziehen,
danach Übergang zur eigentlichen Aufgabe (separat zu besprechen).

## Sprint-Backlog

| # | Item | Status |
|---|------|--------|
| 1 | Dogfood-Configs: 9 umgestellt + **2 fehlende ergänzt** (Solutions, TestRunner hatten gar keine!) + project-info.name → pgm1980/stryker-netx; Mapping per Referenz-Matrix (ADR-051 E1) | ✅ 11/11 `--break-after analysis` EXIT=0, 0 Warn-Treffer |
| 2 | Integration-Matrix: Root-Cause netcore (27 Jobs) lokal BEWIESEN — Fixtures nie restauriert, MSBuildWorkspace braucht assets der referenzierten Source-Projekte → Restore-Fixture per Kategorie + Nightly-Versicherungs-Restore (E2). netframework (2 Jobs) = ANDERE Klasse (MSBuildWorkspace×Legacy-csproj, TypeInitializationException) → continue-on-error honest-deferred (E3). InitCommand war 3× grün. | ✅ Repro-Paar clean→rot/restored→grün auf NetCore+MTP-Pfad; ps1-Syntax 0 Fehler |
| 3 | Manifest-Pin → 3.3.2 (Indexierung via WebFetch bestätigt, restore verifiziert); Auto-Bump verworfen (E4: ungated main-Push, Chicken-Egg, Release-Pfad-Risiko) → Prozess-Konvention Pin-Nachzug im Folge-PR | ✅ |

## Out of scope

- B.1/B.2/D aus BUG_REPORT_9_FOLLOWUP_2 (Reporter-Re-Test abwarten)
- „11/11 Nightly-Mutation-Runs grün" als hartes Kriterium (Erstläufe können
  weitere Schichten zeigen — Kriterium ist: Configs lösen auf, Initial-Phase läuft)

## Status

- [x] Branch `feature/171-dogfood-configs-integration-matrix` geöffnet
- [x] GitHub-Issue #268 angelegt
- [x] Sprint-Backlog (dieses Dokument)
- [x] Item 3 (Manifest 3.3.2 + Auto-Bump-Abwägung, Maxential 3 Thoughts)
- [x] Item 1 (11 Dogfood-Configs, lokal verifiziert)
- [x] Item 2 (Restore-Pflicht + continue-on-error, Maxential 4 Thoughts, Repro-Paar)
- [x] ADR-051 + Änderungshistorie 0.34.0
- [x] Semgrep clean (0 Findings, 17 Dateien); ps1-Parser 0 Syntax-Fehler
- [x] Build 0/0 (BUILD_EXIT=0); Tests: 9 Suiten lokal grün **2137 bestanden / 0 Fehler / 27 legitime Skips**
- [x] E2E-Verdikt (ehrlich dokumentiert): Lokale E2E-Läufe heute Nachmittag durch
  Verifikations-Infrastruktur kontaminiert — Harness meldete Background-Tasks
  wiederholt verfrüht „completed", wodurch 3 E2E-Suiten überlappend gegen dieselben
  samples/-Fixtures liefen (gegenseitige Störung, danach von mir gekillte
  MSBuild-Nodes/Build-Server → ein 240s-Subprozess-Timeout). Sprint 171 ändert
  KEINEN E2E-relevanten Codepfad (Configs wirken nur in src/-CWDs, Rest sind
  CI-Skripte/Workflow/Manifest). Letzter sauberer E2E-Beweis: heute Vormittag
  (Sprint-170-Verifikation, identischer Pfad) 18/18 EXITCODE=0. Maßgebliche
  E2E-Evidenz für den Merge: ci.yml-Gate (e2e-Jobs auf 2 frischen OS-Runnern).
- [ ] PR + ci.yml-Gate + Merge (KEIN Tag — Sprint-138-Präzedenz, CI-/Config-only)
- [ ] Post-Merge: Dispatch integration-test (Erwartung: 28 grün + 2 allowed-failures) + stryker-on-stryker (Erwartung: Module erreichen Mutation-Loop)
- [ ] Housekeeping + Closing-PR

## Backlog-Notiz für Aufgaben-Diskussion (nach Sprint 171)

Tool-seitiger Auto-Restore vor Analysis wenn `project.assets.json` fehlt
(ADR-051-Backlog): CI-First-User treffen sonst „Failed to analyze project
builds" ohne Hinweis. Upstream deckte das via Buildalyzer implizit ab.
