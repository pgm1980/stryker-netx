# HANDOVER — v3.0.24 + Sprint 138 Closing

## Final State — v3.0.24 (echt released auf NuGet.org)
- **Dogfood: 1175 green / 9 skip / 1184 total**
- **Latest tag: v3.0.24** (24 v3.0.x patches since v3.0.0)
- **NuGet.org: `dotnet-stryker-netx 3.0.24` öffentlich auffindbar und installierbar** ✓ (seit Sprint 138 / 2026-05-06)
- **GitHub Release v3.0.24:** `dotnet-stryker-netx.3.0.24.nupkg` (44 MB) als Asset attached ✓
- Repo `pgm1980/stryker-netx`: **public** (Apache-2.0, NOTICE-attributed, "independent community fork")

## Cumulative Session (Sprints 95-138, 44 sprints)
- Dogfood: **906/99 → 1175/9** (+269 green, -90 skip, +179 new tests)
- 43 GitHub releases (v2.81.0 → v3.0.24); Sprint 138 had no version-bump (CI-reparation only)
- 4 production bugs fixed:
  - Sprint 99: MsBuildHelper.GetVersion missing-space + multi-line
  - Sprint 136: SseServer.Dispose double-close
  - Sprint 137: RoslynSemanticDiagnosticsEquivalenceFilter speculative-binding crash on MemberBindingExpression
  - Sprint 138: release.yml CI-reparation (Quota + permissions + NUGET_API_KEY + workflow_dispatch hardening) → **erstmaliger erfolgreicher NuGet.org-Push**

## Sprint 135-138 — Final Cleanup + Release Pipeline
- **Sprint 135 (v3.0.22):** Last attackable architectural-deferral ELIMINATED (CSharpRollbackProcess null-SourceTree)
- **Sprint 136 (v3.0.23):** SseServer.Dispose production fix (best-effort per-writer disposal)
- **Sprint 137 (v3.0.24):** RoslynSemanticDiagnosticsEquivalenceFilter speculative-binding fix (Sprint 23 known-bug ELIMINATED)
- **Sprint 138 (no tag):** Release workflow + first NuGet.org push. CI-reparation only, no `src/` diff.

## Sprint 138 (CI-Reparatur + erster echter NuGet-Push)

**Trigger:** Calculator-Test entdeckte am 2026-05-06 dass `dotnet tool install -g dotnet-stryker-netx` HTTP 404 lieferte — das Tool war nirgendwo öffentlich beziehbar.

**Diagnose:**
1. GitHub Actions Quota erschöpft seit 2026-05-02 21:47Z — private Repo + Free-Plan = 2000 Min/Monat-Limit, ALLE Workflow-Runs failed sofort vor Job-Start (`runner_id:0`, `steps:[]`, kein Runner allokiert)
2. `default_workflow_permissions: read` auf Repo-Level überschrieb das `permissions: contents: write` im Workflow-File
3. `NUGET_API_KEY`-Secret war nie gesetzt → Push-Step nahm immer den "Skipped notice"-Branch
4. release.yml hatte zusätzlich Anti-Patterns die nicht greifen würden (`if: ${{ secrets.X != '' }}` step-level, fehlender `tag_name`+`ref` für workflow_dispatch)

**Fix (PR #222 squash-merge `5dfe7b6`):**
- Repo auf public gewechselt → unbegrenzte Actions-Minuten
- default_workflow_permissions auf `write`
- NUGET_API_KEY-Secret hinterlegt (Glob-pinned auf dotnet-stryker-netx, push-only scope)
- release.yml gehärtet: workflow_dispatch + inputs.tag + secrets-env-pipe + explicit tag_name/ref + concurrency-group + Test-Step entfernt (= ci.yml-Concern)

**Erfolg:** Workflow-Run `25405963930` (1m27s) → HTTP 201 Created → "Your package was pushed." → Indexing-Lag ~14 Min → public installierbar.

## Bug-Report aus Real-Life-Test (Calculator-Tester)

Nach erfolgreicher Installation hat Calculator-Tester einen extrem strukturierten Bug-Report eingereicht (`_bug_reporting/bug_report_stryker_netx.md`). 6 echte Bugs + 2 Hinweise — **alle gegen Code verifiziert:**

| # | Schwere | Status | Sprint |
|---|---|---|---|
| #1 | 🔴 HOCH | Profile-Flag ohne sichtbaren Effekt | Doku-Lücke (Profile × Level conjunctive) — Stufe 1: **Sprint 139 Doku**, Stufe 2: **Sprint 140 Code** (ToT + Maxential für Auto-Bump-Entscheidung) |
| #2 | 🟡 MITTEL | Banner zeigt 1.0.0-preview.1 statt 3.0.24 | **Sprint 139** — Directory.Build.props + release.yml Version-Properties komplett setzen |
| #3 | 🟡 MITTEL | "Update verfügbar" obwohl bereits aktuell | **Sprint 139** (auto-behoben durch #2) |
| #4 | 🟡 NIEDRIG | `--version` = Project-Version statt Tool-Version (upstream-Erbe) | **Sprint 141** (additiv `--tool-version` oder Breaking-Change zu v3.1) |
| #5 | 🟡 NIEDRIG | Log-Rauschen "Could not find a valid analysis for target  for project" | **Sprint 139** — IsNullOrEmpty-Check in InputFileResolver:524 |
| #6 | 🟡 NIEDRIG | `--reporters` plural in unserer Doku, Tool akzeptiert nur `--reporter` singular | **Sprint 138 Closing** ✓ (3 Stellen in `_config_neuprojekte/` korrigiert) |
| Hinweis #7 | 🟢 INFO | NuGet-Indexing-Latenz | **Sprint 141** — Doku-Hinweis |
| Hinweis #8 | 🟢 NIEDRIG | Multi-Source-Project-Setup verlangt manuelles `--project` | **Sprint 141+** — `--all-source-projects` Feature |

## Final 9 Skips Breakdown — ALL legitimate (unverändert seit Sprint 137)

### 3 PERMANENT (Sprint 1 architectural removal)
- BuildalyzerHelperTests, AnalyzerResultExtensionsTests, VsTestHelperTests

### 4 WINDOWS-CONDITIONAL (legitimate platform-skip)
- InitialBuildProcessTests (DotnetFramework + MSBuild.exe path × 4)

### 2 FOREVER-SKIP (per user decision — Buildalyzer-removed Sprint 1)
- ProjectOrchestratorTests
- InputFileResolverTests

## Roadmap — Sprints 139-141 (Bug-Report-Aufarbeitung)

### Sprint 139 — Doku + Quick-Fixes (Tag v3.0.25)
- **Bug #2:** release.yml + `Directory.Build.props` Version-Properties komplett — Banner zeigt korrekte Version
- **Bug #3:** auto-behoben durch #2
- **Bug #5:** `IsNullOrEmpty(targetFramework)`-Check in `InputFileResolver.cs:509-524`
- **Bug #1 Stufe 1 (Doku):** `Stryker_NetX_Installation.md` Sektion "Mutation Level + Profile" neu schreiben mit klarer Conjunctive-Erklärung + konkrete Befehlsbeispiele:
  - `--mutation-profile Stronger --mutation-level Advanced` für volle Wirkung
  - `--mutation-profile All --mutation-level Complete` für maximalen Operator-Set
- Tag v3.0.25 → release.yml auto-trigger (verifiziert push-tag-Auto-Path)

### Sprint 140 — Bug #1 Code-Strategie (ToT + Maxential, Tag v3.0.26 oder v3.1.0)
- **ToT first** (User-direktive): Multi-Branch-Exploration der Profile/Level-Kopplung
  - Branch-Optionen z.B.: 
    - A: Status-quo + Warning-Log
    - B: Auto-Bump (Profile setzt implizit Mutation-Level)
    - C: Profile-Default-Level-Bundle ("StrongerStandard", "AllComplete" als kombinierte Profiles)
    - D: Hybrid (Auto-Bump als Default, opt-out via `--no-auto-mutation-level`)
- **Maxential afterwards** mit den ToT-besten Branches
- ADR-026 schreiben (Profile/Level-Semantik formalisieren)
- Implementation
- Tag entscheidet sich am Ende der Maxential — wenn Auto-Bump dann v3.1.0 (Mini-Breaking), wenn nur Warning dann v3.0.26

### Sprint 141 — UX-Polish (Bugs #4, Hinweis #7, #8)
- Bug #4: ADR — additiv `--tool-version` oder breaking `--version`-Reclaim?
- Hinweis #7: Doku-Hinweis NuGet-Indexing-Latenz (5–30 Min normal)
- Hinweis #8: `--all-source-projects` Feature (oder Multi-`--project`)

## Reusable Artifacts Produced (18+ patterns)
- `LoggerMockExtensions.EnableAllLogLevels<T>()` (Sprint 96)
- `LoggerMockExtensions.VerifyNoOtherLogCalls<T>()` (Sprint 97)
- `MockJsonReport`, `MockJsonReportFileComponent` test stubs
- `BuildScanDiffTarget` GitDiff mock-builder pattern
- `TestHelper.GetItemPaths` default empty (Sprint 112)
- `MutantOrchestratorTestsBase.CountMutations(source)` (Sprint 119)
- `Mutation NewMutation()` Sprint 2 required-init helper
- `FullRunScenario` mutant+test+coverage harness (Sprint 127)
- `IgnoredMethodMutantFilterTests` BuildMutantsToFilter helpers (Sprint 124-128)
- End-to-end Compile() integration setup pattern (Sprint 131-134)
- Real-HttpListener integration with HttpClient (Sprint 130)
- Roslyn speculative-binding fallback pattern (Sprint 137 — pre-check + try/catch)
- Best-effort Dispose pattern (Sprint 136 — per-resource try/catch)
- Drift-cheat-sheet (Sprint 97)
- Pre-port signature-grep heuristic (Sprint 100/101)
- Architectural-Deferral Validation Heuristic (Sprint 114-115 lesson)
- Spectre.Console `.Width(160)` discovery (Sprint 117)
- **Release-Workflow workflow_dispatch + inputs.tag pattern** (Sprint 138 — re-runnable for existing tags)
- **secrets-context env-pipe pattern** (Sprint 138 — replaces step-level `if: ${{ secrets.X != '' }}` anti-pattern)
- **Trennung ci.yml/release.yml Concerns** (Sprint 138 — Quality-Gating vs Pack+Push)

## DEEP_MEMORY.md
See `memory/DEEP_MEMORY.md` for comprehensive technical-lessons reference (Sprints 95-138).

## Worktree leftover (housekeeping)
3 worktree-directories busy/locked (user must close spawned-session windows before file-system cleanup).
