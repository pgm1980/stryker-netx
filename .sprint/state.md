---
current_sprint: "170"
sprint_goal: "CI-Reanimation + Doc-Drift — P0: Nerdbank.MessagePack 1.1.62→1.2.4 (zwei post-v3.3.1 Advisories GHSA-qjvr-435c-5fjh + GHSA-92vj-hp7m-gwcj brechen via NuGetAudit+TWAE den Build, 20× NU1902). P1: stryker-on-stryker.yaml:81 null==false-Koerzierung lässt Scheduled-Runs seit ≥20 Tagen im NuGet-Modus gegen den nicht-existenten Manifest-Pin 0.0.0-localdev laufen. P2: 2 verwaiste locked Agent-Worktrees + .clone. Doc-Drift: MEMORY.md/DEEP_MEMORY.md (Sprint-0-Stand), README.md. Target tag v3.3.2 (patch — Dependency-Bump im shipped Tool, kein API-Break)."
branch: "feature/170-ci-reanimation-doc-drift"
started_at: "2026-06-11"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 170 (v3.3.2 prep)

## Trigger

Session-Start-Bestandsaufnahme 2026-06-11, GitHub-Issue #265. Kein neuer
Bug-Report — beide CI-Breaker sind seit dem v3.3.1-Release (2026-05-26)
von außen entstanden (neue Advisories) bzw. lagen latent im Workflow.

## Sprint-Backlog

| # | Item | Status |
|---|------|--------|
| P0 | Nerdbank.MessagePack 1.1.62 → 1.2.4 in `Directory.Packages.props` (GHSA-qjvr-435c-5fjh fixed 1.1.78, GHSA-92vj-hp7m-gwcj fixed 1.2.4; 1.2.4 = latest stable 2026-05-18) + 10× packages.lock.json regeneriert (RestoreLockedMode) | ✅ Build 0/0, Audit clean (25 Projekte) |
| P1 | `stryker-on-stryker.yaml:81` Expression schedule-sicher machen (`null == false` koerziert in GH-Expressions zu `0 == 0` = true → USE_LOCAL_TOOL='false' bei schedule) + `.config/dotnet-tools.json` Pin 0.0.0-localdev → 3.3.1 — Maxential 9 Thoughts + 1 ToT-Branch, ADR-050 | ✅ implementiert; tool restore → 3.3.1 ok, pack ok; Dispatch-Verifikation nach Merge |
| P2 | 2 locked Agent-Worktrees (`agent-ac69014b…` @ aae7630, `agent-afd558f7…` @ 8a6b27a) + `.clone/worktrees` — erst dirty/unmerged prüfen, dann entfernen | ✅ Inhalte verifiziert auf main (Sprint-159/160-Duplikate), Worktrees + Branches + .clone entfernt |
| D1 | README.md auf v3.3.x-Stand (Ära-Tabelle, NuGet-Badge, v3.3.x-Features, SDK-Angabe) | ✅ |
| D2 | MEMORY.md (Root) von Sprint-0-Stand auf aktuell | ✅ kompletter Rewrite |
| D3 | DEEP_MEMORY.md: Sektion 0 „Stand heute" + [Ausgang]-Anmerkungen | ✅ |
| D4 | ADR-050 + Änderungshistorie 0.33.0 in architecture_specification.md | ✅ |

## Out of scope (unverändert honest-deferred aus Sprint 169)

- B.1 (3 Sites) `byte[].AsSpan` / B.2 (1 Site) `byte[].AsMemory` Overload-Mismatch in AsSpanAsMemoryMutator
- D (4 Sites) PluginManager CS0165/CS0161 (Block-Removal-Codegen)
- Warten auf Reporter-Re-Test gegen v3.3.1 (Pass-Kriterien: <20 Safe-Mode-Warnings, <15 % CompileError)

## Session-Notizen

- Serena steht in dieser Session nicht zur Verfügung (Standalone-Server
  :9121 von anderem Projekt reserviert; stryker-netx in serena_config.yml
  nachregistriert, greift erst nach Server-Neustart). Dokumentierter
  Fallback: Built-In Tools (Read/Edit/Write/Glob/Grep) — CLAUDE.md-konform
  mit Begründung.
- Befund verschärft: stryker-on-stryker war NIE grün — 42/42 Runs failed,
  alle schedule-Events, 0 dispatch-Runs seit Sprint 24. Der korrekte
  dispatch-Default-Pfad wurde schlicht nie ausgeführt.
- Maxential `tag`-Tool hat Schema-Defekt (typloser Listen-Param wird als
  String transportiert, Pydantic-Validierung schlägt fehl) — Tags nicht
  setzbar, Entscheidungen stehen in den Thoughts selbst.

## Status

- [x] Branch `feature/170-ci-reanimation-doc-drift` geöffnet
- [x] GitHub-Issue #265 angelegt (auto-closed durch PR-Merge)
- [x] Sprint-Backlog (dieses Dokument)
- [x] P0 implementiert + Build 0/0 + Vulnerable-Audit clean
- [x] P1 implementiert (Maxential 9 Thoughts + ToT-Branch + ADR-050)
- [x] P2 aufgeräumt (Worktrees, Branches, .clone)
- [x] D1–D4 Doc-Drift behoben
- [x] Semgrep clean (0 Findings, 8 geänderte Dateien)
- [x] Tests grün: 2155 bestanden / 0 Fehler / 27 legitime Skips (inkl. E2E 18/18), `dotnet test` Exit 0, Coverage via coverlet.runsettings
- [x] PR #266 squash-merged (`01df790` auf main); ci.yml-Gate komplett grün (build+test ubuntu/windows, e2e ubuntu/windows, semgrep, CI complete)
- [x] Tag `v3.3.2` auf Merge-Commit gepusht; release.yml Run 27339842976 success: NuGet-Push HTTP-Erfolg + GitHub-Release mit `dotnet-stryker-netx.3.3.2.nupkg`-Asset (NuGet-Indexierung lief beim Sprint-Close noch — Manifest-Pin bleibt designgemäß auf 3.3.1)
- [x] Dispatch-Run 27339863955 (erster workflow_dispatch dieses Workflows überhaupt): **P1-Kriterium erfüllt** — alle 11 Jobs überstehen restore/pack/install, der frisch gepackte Stryker startet (Banner + Analyse-Log). Failure jetzt NACHGELAGERT: per-Modul `stryker-config.json` (Sprint 24 verbatim aus Upstream vendored) referenzieren Upstream-Test-Projektpfade (`../*.UnitTest.csproj` bzw. `../Stryker.Core/Stryker.Core.UnitTest/`), die im netx-Layout (`tests/*.Tests`) nie existierten → „No .csproj or .fsproj file found". Bisher unsichtbar, weil kein Run je bis dorthin kam.
- [x] Housekeeping: alle Flags true, Closing-PR

## Follow-up-Kandidaten für Sprint 171 (aus Sprint-170-Verifikation)

1. **Dogfood-Configs reparieren:** 9× `src/*/stryker-config.json` Test-Projektpfade
   von Upstream-Layout auf netx-Layout (`tests/Stryker.Core.Tests` etc.) umstellen —
   danach hat der Nightly erstmals die Chance auf echte Mutation-Runs.
2. **Integration-Test-Matrix-Reanimation:** `integration-test.yaml` (30 Jobs) failt
   auf JEDEM PR seit mindestens Sprint 167 (gemergter Sprint-169-PR: 28 Failures am
   Step „Run integration tests"). Vorbestehend, nie Teil eines Merge-Gates.
3. Optional: `.config/dotnet-tools.json` Pin 3.3.1 → 3.3.2 sobald NuGet-Indexierung
   abgeschlossen; längerfristig Auto-Bump via release.yml (ADR-050 Follow-up-Idee).
