# HANDOVER — v3.1.2 + Sprint 142 Hotfix (Bug #9 from 2nd Calculator-tester report)

## Final State — v3.1.2 (Bug #9 Hotfix released)
- **Dogfood: 1184 green / 9 skip / 1193 total** (Sprint 142 +3 UoiMutator-Regression-Tests in Stryker.Core.Tests, jetzt 391)
- **Latest tag: v3.1.2** (Patch-bump: hotfix für `--mutation-profile All` InvalidCastException; SpanReadOnly disabled)
- **NuGet.org: `dotnet-stryker-netx 3.1.2` öffentlich auffindbar** ✓ (seit Sprint 142 / 2026-05-06)
- **Banner zeigt korrekte Version**: `Version: 3.1.2`
- **`-T` production-verified**: `3.1.2`
- **Bug #9 production-verified**: lokale Repro mit `data.Length`-Pattern → kein Crash mehr
- Repo `pgm1980/stryker-netx`: **public** (Apache-2.0, NOTICE-attributed, "independent community fork")
- **Versionssprung-Reihenfolge der letzten 5 Sprints:** v3.0.24 (Sprint 137) → v3.0.25 (Sprint 139) → v3.1.0 (Sprint 140 ADR-025) → v3.1.1 (Sprint 141) → **v3.1.2 (Sprint 142 Bug #9 hotfix, ADR-026)**

## Cumulative Session (Sprints 95-142, 48 sprints)
- Dogfood: **906/99 → 1184/9** (+278 green, -90 skip, +188 new tests). Sprint 142 +3 UoiMutator-Regression-Tests.
- 47 GitHub releases (v2.81.0 → v3.1.2); Sprint 138 had no version-bump (CI-reparation only)
- 8 production bug-fix sprints:
  - Sprint 99: MsBuildHelper.GetVersion missing-space + multi-line
  - Sprint 136: SseServer.Dispose double-close
  - Sprint 137: RoslynSemanticDiagnosticsEquivalenceFilter speculative-binding crash on MemberBindingExpression
  - Sprint 138: release.yml CI-reparation → erstmaliger NuGet-Push (v3.0.24)
  - Sprint 139: Calculator-tester Bug-Report-1-Batch → 4 of 6 bugs closed in v3.0.25
  - Sprint 140: Bug #1 Stufe 2 Code-Side via ADR-025 → v3.1.0
  - Sprint 141: last 3 bug-report-1 items (Bug #4 + Hinweis #7 + #8) → v3.1.1
  - Sprint 142: Bug #9 hotfix from Calculator-tester Bug-Report-2 (ADR-026 ConditionalInstrumentation × TypeSyntax incompat) → v3.1.2

## Sprint 135-141 — Final Cleanup + Release Pipeline + Bug-Report Vollständig
- **Sprint 135 (v3.0.22):** Last attackable architectural-deferral ELIMINATED (CSharpRollbackProcess null-SourceTree)
- **Sprint 136 (v3.0.23):** SseServer.Dispose production fix (best-effort per-writer disposal)
- **Sprint 137 (v3.0.24):** RoslynSemanticDiagnosticsEquivalenceFilter speculative-binding fix (Sprint 23 known-bug ELIMINATED)
- **Sprint 138 (no tag):** Release workflow + first NuGet.org push. CI-reparation only, no `src/` diff.
- **Sprint 139 (v3.0.25):** Calculator-tester real-life bug report batch — 4 of 6 bugs closed (Bug #5 IsNullOrEmpty + Bug #2 version-sync + Bug #3 auto-resolved + Bug #1 Stufe 1 Profile×Level conjunctive doc). Auto-trigger via push-tag verified.
- **Sprint 140 (v3.1.0):** Bug #1 Stufe 2 Code-Side — mutation-profile auto-bumps mutation-level (ADR-025). Decision-trail: ToT (5 branches) + Maxential (14 thoughts, 2 branches full-integration-merged). Auto-Bump production-verified.
- **Sprint 141 (v3.1.1):** Last 3 bug-report items — Bug #4 (additive `--tool-version`/`-T` flag), Hinweis #7 (NuGet-indexing-doc), Hinweis #8 (Solution-mode hint in error-message + doc-section). **Calculator-tester Bug-Report VOLLSTÄNDIG ABGEARBEITET — alle 8 von 8 Items closed.** ✓

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
| #1 | 🔴 HOCH | Profile-Flag ohne sichtbaren Effekt | Doku-Lücke (Profile × Level conjunctive) — Stufe 1 (Doku): **Sprint 139 ✓**, Stufe 2 (Code): **Sprint 140 ✓** (ADR-025 Auto-Bump, production-verified mit `Version: 3.1.0` aus public NuGet) |
| #2 | 🟡 MITTEL | Banner zeigt 1.0.0-preview.1 statt 3.0.24 | **Sprint 139** ✓ — Directory.Build.props + release.yml Version-Properties komplett gesetzt; production-verified |
| #3 | 🟡 MITTEL | "Update verfügbar" obwohl bereits aktuell | **Sprint 139** ✓ (auto-behoben durch #2) |
| #4 | 🟡 NIEDRIG | `--version` = Project-Version statt Tool-Version (upstream-Erbe) | **Sprint 141 ✓** — additiv `--tool-version` / `-T` (kein Breaking-Change), production-verified |
| #5 | 🟡 NIEDRIG | Log-Rauschen "Could not find a valid analysis for target  for project" | **Sprint 139** ✓ — IsNullOrEmpty-Check in InputFileResolver:509 |
| #6 | 🟡 NIEDRIG | `--reporters` plural in unserer Doku, Tool akzeptiert nur `--reporter` singular | **Sprint 138 Closing** ✓ (3 Stellen in `_config_neuprojekte/` korrigiert) |
| Hinweis #7 | 🟢 INFO | NuGet-Indexing-Latenz | **Sprint 141 ✓** — Doku-Sektion "Häufige Stolpersteine" mit Real-World-Numbers |
| Hinweis #8 | 🟢 NIEDRIG | Multi-Source-Project-Setup verlangt manuelles `--project` | **Sprint 141 ✓** — Solution-Mode existiert schon, Error-Message erweitert + Doku-Sektion "Multi-Source-Project Setups" |

## Final 9 Skips Breakdown — ALL legitimate (unverändert seit Sprint 137)

### 3 PERMANENT (Sprint 1 architectural removal)
- BuildalyzerHelperTests, AnalyzerResultExtensionsTests, VsTestHelperTests

### 4 WINDOWS-CONDITIONAL (legitimate platform-skip)
- InitialBuildProcessTests (DotnetFramework + MSBuild.exe path × 4)

### 2 FOREVER-SKIP (per user decision — Buildalyzer-removed Sprint 1)
- ProjectOrchestratorTests
- InputFileResolverTests

## Roadmap — Sprints 139-141 (Bug-Report-Aufarbeitung)

### Sprint 139 — Doku + Quick-Fixes (Tag v3.0.25) ✓ DONE
- **Bug #2 ✓:** release.yml + `Directory.Build.props` Version-Properties komplett — Banner zeigt korrekte Version (production-verified: `Version: 3.0.25` aus public NuGet)
- **Bug #3 ✓:** auto-behoben durch #2
- **Bug #5 ✓:** `IsNullOrEmpty(targetFramework)`-Check in `InputFileResolver.cs:509`
- **Bug #1 Stufe 1 (Doku) ✓:** `Stryker_NetX_Installation.md` Sektion "Mutation Profile × Level" mit Filter-Logik, Empfehlungs-Tabellen, Anwender-Fehler-Beispiel, Strategie für Projekte
- **Tag v3.0.25 → release.yml auto-trigger via push-tag verified** ✓ (Sprint 138 hatte nur workflow_dispatch verifiziert)

### Sprint 140 — Bug #1 Code-Strategie ✓ DONE (Tag v3.1.0)
- **ToT first** (User-Direktive): 5 Branches A-E. Pruned C (breaks 1:1 schema-compat with upstream) und E (diverges from upstream Level-semantic). Top-2: D-Hybrid (0.82) und B-AutoBump (0.78).
- **Maxential afterwards**: 14 Thoughts, 2 Branches `B-autobump` + `D-hybrid` both full-integration-merged. D-Hybrid's opt-out flag YAGNI-pruned for the 1% use-case → D collapses to D-lite = essentially B with better Info-log announcement. **Final: B-AutoBump** (tagged `decision`+`synthesis`).
- **ADR-025** geschrieben (nicht ADR-026 — ADR-024's Forward-Reference auf "ADR-025: JsonReport concrete-types" wurde auf "Future-ADR (TBD-Nummer)" umnummeriert, da Sprint 140 den Slot zuerst genommen hat)
- **Implementation**: `StrykerInputs.ResolveMutationLevel()` + `[LoggerMessage]` `LogAutoBumpedMutationLevel`; 9 neue AutoBump-Tests (3 profiles × 3 level-settings); pre-existing CLI-Test gefixt (Sprint-139's `0.0.0-localdev` baseline-shift).
- **Tag v3.1.0 production-verified**: NuGet-Push erfolgreich (1m55s), Indexing-Lag, Tool-Install + Banner zeigt `Version: 3.1.0`.

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
