---
current_sprint: "172"
sprint_goal: "Fix Issue #270 — MatchesFilter verstümmelt gepunktete project-Filter ohne Endung: Path.GetFileNameWithoutExtension auf der FILTER-Seite macht aus 'Stryker.Configuration' → 'Stryker' (letztes Namens-Segment als 'Extension' interpretiert) ⇒ ADR-039-Layer-3-Fallback feuert immer ⇒ Multi-Referenz-Module sterben an der Disambiguierung (8/11 Nightly-Module). Die in #270 ursprünglich vermutete TargetFileName-Leere war Zwischenhypothese und wurde falsifiziert (alle 12 Analysen Succeeded=True via --diag); ebenso falsifiziert: IsTestProject-Fehlklassifikation (12-Projekte-Scan erklärt sich durch MSBuildWorkspace-transitive ProjectReferences — Timing-Beweis: 11 Folge-Analysen im 130-ms-Burst nach Dogfood). Fix: Filter-Seite roh vergleichen, Pfad-Seite mit UND ohne echte Endung anbieten. Schließt nebenbei den latenten Cross-Match ('Foo.Bar' matchte Foo.csproj). TDD 13 Tests (Red 6/13 → Green 13/13). Vorgezogen vor das 360°-Analyse-Programm (Sprints 173+), damit der Nightly als Sicherheitsnetz läuft. Target tag v3.3.3 (patch — 1-Methoden-Fix in shipped Stryker.Core)."
branch: "feature/172-layer3-gate-project-filter-fix"
started_at: "2026-06-11"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 172 (v3.3.3 prep)

## Trigger

Issue #270 (aus Sprint-171-Dispatch-Verifikation), User-Direktive: Fix vorziehen,
danach 360°-Analyse-Programm (6 Sprints, Findings-only, src/ tief + tests/ als
Orakel — User-Approval liegt vor).

## Root-Cause (final, ersetzt die #270-Zwischenhypothese)

`InputFileResolver.MatchesFilter` wendete `Path.GetFileNameWithoutExtension`
auch auf den FILTER an: `'Stryker.Configuration'` → `'Stryker'` (".Configuration"
als Extension interpretiert) ⇒ nie ein Match für gepunktete Namen ohne `.csproj`
⇒ Layer-3-Fallback verwirft den Filter ⇒ „more than one project reference".
Falsifizierte Zwischenhypothesen (dokumentiert für die Nachwelt):
- TargetFileName/BuildsAnAssembly leer → widerlegt: --diag zeigt Succeeded=True
  für alle 12 Analysen (gebaut wie ungebaut identisch).
- IsTestProject-Fehlklassifikation von Core/VsTest → widerlegt: Properties leer,
  References clean, Name-Suffix negativ; der 12-Projekte-Scan erklärt sich durch
  MSBuildWorkspace-TRANSITIVE ProjectReferences (Timing: 11 Analysen im
  130-ms-Burst = Workspace-Cache-Hits nach Dogfoods 4,5-s-Load).
Latenter Zweitschaden derselben Zeile: 'Foo.Bar' matchte 'Foo.csproj'
(beidseitige Verstümmelung) — falsches Projekt mutierbar. Mit gefixt.

## Sprint-Backlog

| # | Item | Status |
|---|------|--------|
| 1 | TDD: ProjectFilterMatchingTests (13 Fälle: #270-Klasse, Endungs-Klasse, Cross-Match-Verbote, Case, Degenerate) | ✅ Red 6/13 dokumentiert → Green 13/13 |
| 2 | Fix `MatchesFilter`: Filter roh, Pfad-Seite mit+ohne Endung; `internal` für Unit-Test (IVT vorhanden) | ✅ |
| 3 | Real-Szenario-Probe: src/Stryker.Configuration Volllauf bis initial-test-run — kein WRN, 1 Projekt, 1196 Tests, 24 s | ✅ (Ganz-Output-Grep, Sprint-171-Lehre) |
| 4 | ADR-052 + Änderungshistorie 0.35.0 + Issue-#270-Korrektur-Kommentar | ✅ |
| 5 | Volle Suite + Semgrep; PR; Gate; Merge; Tag v3.3.3; Release; Dispatch-Nightly-Verifikation | Suite ✅ (Build 0/0; **2168 bestanden / 0 Fehler / 27 Skips**, Core.Tests 492→505; E2E 18/18; Marker BUILD/TEST/E2E_EXIT=0); Semgrep ✅ 0 Findings; Rest läuft |

## Status

- [x] Branch geöffnet, Diagnose-Kette komplett (Repro → 2 Falsifikationen → Beweis-Experiment `--project X.csproj`)
- [x] TDD Red 6/13 → Green 13/13
- [x] ADR-052 + 0.35.0 + Issue-#270-Korrektur (Kommentar gesetzt)
- [x] Memory korrigiert (falsifizierte TargetFileName-These entfernt — falsche Memories sind schlimmer als keine)
- [x] Suite grün (2168/0/27), Semgrep clean
- [x] PR #272: Gate-Iteration — Erstversion brach MultipleTestProjects (targetProjectMode
  übergibt Filter als FULL PATH; altes GetFileNameWithoutExtension strippte das
  Verzeichnis load-bearing) → **die in Sprint 171 reanimierte Integration-Matrix hat
  ihren ersten echten Regressions-Fang gemacht**. Iteration 2: GetFileName auf der
  Filter-Seite; Tests auf 18 Fälle + Forward-Slash-Portabilität (ubuntu-Lehre:
  Backslash-InlineData splittet nur auf Windows). Windows-Flake im Gate
  (InitialTestProcess_ShouldCalculateTestTimeout, 1/1200) → Issue #273, Rerun grün.
- [x] Squash-Merge `aef20e0`; Tag v3.3.3; release.yml success (NuGet-Push +
  Release-Asset `dotnet-stryker-netx.3.3.3.nupkg`); Issue #270 auto-closed
- [x] Dispatch-Verifikation Run 27353164648: **0× Filter-WRN** (Frühmessung über
  Job-Logs) + nach 21 min **9/11 Module SUCCESS** (Sprint 171: 3/11), Core lief
  noch tief in seiner Mutation-Loop (120-min-Fenster, Ausgang im nächsten
  Scheduled-Run sichtbar). Einziger Failure: TestRunner.MTP „Failed to start
  test server" — NEUER Befund, vermutlich Upstream-Vendoring-Config-Rest
  (`test-runner: mtp` auf xunit-Projekt) → **Issue #274**, vorregistriert für
  Analyse-Bereich D. Messlatte (>3/11 + 0 WRN) klar übertroffen.
- [x] Issues: #270 closed (Fix), #268/#265 closed (Vor-Sprints); #273 (Flake) +
  #274 (MTP-Server) bewusst OFFEN als Analyse-Programm-Träger
- [x] Housekeeping + Closing-PR; danach Start Analyse-Sprint 173 (Mutatoren-Katalog)
