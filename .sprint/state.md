---
current_sprint: "138"
sprint_goal: "Release-Workflow reparieren — workflow_dispatch + secrets-env-pattern + tag-explicit; v3.0.24 zu NuGet.org pushen"
branch: "main"
started_at: "2026-05-06"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 138 closed

## Final State
- main HEAD: squash-merge `5dfe7b6` (Sprint 138 PR #222) + closing-housekeeping merge (this PR)
- Latest tag: **v3.0.24** (still — Sprint 138 was pure CI-reparation, no version bump per User decision C1)
- NuGet.org: **`dotnet-stryker-netx 3.0.24` öffentlich auffindbar und installierbar** ✓
- GitHub Release v3.0.24: `dotnet-stryker-netx.3.0.24.nupkg` (44 MB) als Asset attached ✓

## Sprint 138 Achievements
1. **Quota-Problem gelöst** — Repo `pgm1980/stryker-netx` von private auf public umgestellt (User-Aktion). Free-Plan-2000-Min-Limit eliminiert.
2. **default_workflow_permissions** auf `write` gesetzt (via API).
3. **NUGET_API_KEY-Secret** im Repo hinterlegt (User: NuGet.org-Glob-pinned, push-only scope).
4. **release.yml hardened** (PR #222):
   - `workflow_dispatch:` mit `inputs.tag` als zusätzlicher Trigger
   - `secrets`-context env-pipe statt step-level if (Best-Practice)
   - Explizite `tag_name` + `ref` in `softprops/action-gh-release` und `actions/checkout` (sonst landet workflow_dispatch auf main statt am Target-Tag)
   - `concurrency` group pro Tag
   - Test-Step entfernt (Quality-Gating ist ci.yml-Concern, nicht release-Concern)
   - Job-level `permissions: contents: write` defensiv
5. **Erstmaliger NuGet-Push für v3.0.24:**
   - Workflow-Run `25405963930` via `gh workflow run release.yml --ref main -f tag=v3.0.24`
   - Push: HTTP 201 Created in 2.1s ("Your package was pushed.")
   - Indexing-Lag: ~14 min (push 22:34Z → flatcontainer indexed 22:48Z)
6. **Bug-Report-Verifikation** (vom Calculator-Tester nach Real-Life-Use):
   - Bug #1 (HOCH): Profile-Flag ohne Effekt — verifiziert: Profile × Level conjunctive (Sprint 22 lesson, nicht in Doku) → **Sprint 139 Doku, Sprint 140 Code**
   - Bug #2 (MITTEL): Banner zeigt 1.0.0-preview.1 — verifiziert: `Directory.Build.props` Zeile 38-39 baseline, release.yml setzt nur PackageVersion → **Sprint 139**
   - Bug #3 (MITTEL): Update-Hinweis spinnt — Folge von #2 → **Sprint 139 (autobehoben)**
   - Bug #4 (NIEDRIG): `--version` Flag = Project-Version statt Tool-Version (upstream-Erbe) → **Sprint 141**
   - Bug #5 (NIEDRIG): Log-Rauschen mit leerem TargetFramework — verifiziert: `InputFileResolver.cs:524` `null`-only-Check → **Sprint 139**
   - Bug #6 (NIEDRIG): `--reporters` (plural) in unserer Doku — **HEUTE im Closing gefixt** ✓ (3 Stellen in `_config_neuprojekte/{Stryker_NetX_Installation,CLAUDE_CS}.md`)
   - Hinweis #7: NuGet-Indexing-Latenz — Doku-Update Sprint 141
   - Hinweis #8: Multi-Source-Project-UX — Sprint 141+
7. **Sprint 138 Closing-Housekeeping** (this commit):
   - Bug #6 Doku-Quick-Fix
   - state.md final
   - MEMORY.md Sprint 138 entry
   - HANDOVER.md updated mit echter NuGet-Realität + Sprint 139/140/141 Roadmap

## Tag-Strategy für Sprint 138
**C1 (kein neuer Tag).** Sprint 138 hatte 0 Code-Diff in `src/`, nur `.github/workflows/release.yml` + Doku + Sprint-State. v3.0.24 bleibt als letzter "echter" Sprint-137-Release. Sprint 139 wird der erste neue Tag (v3.0.25) und verifiziert dabei automatisch den Push-Tag-Auto-Trigger der release.yml.

## Diagnose-Trail für Reproduzier (vergangene Sprint-138-Iterationen)
1. **Falsche Initial-Hypothese** (User korrigiert): `actions/checkout@v6` existiert nicht → falsch, alle Action-Versionen existieren. Lehre: Symptome systematisch via API verifizieren statt aus Memory raten.
2. **Echte Ursache** (durch `runner_id:0` + `steps:[]`-Pattern entdeckt): Quota-erschöpft seit 2026-05-02 21:47Z.
3. **Plus** sekundäres Issue: NUGET_API_KEY-Secret hat NIE existiert — selbst wenn Quota verfügbar gewesen wäre, hätte der Push immer skipped.

## Next: Sprint 139 (Bug-Report-Batch)
- feature/139-bug-batch
- Bug #2 (Version-Sync) + Bug #3 (Update-Hinweis Folge) + Bug #5 (IsNullOrEmpty) + Bug #1 Stufe 1 (Profile/Level conjunctive Doku-rewrite) + Bug #6 (war schon Sprint 138)
- Tag v3.0.25 → release.yml auto-trigger via push-tag → automatischer NuGet-Push → verifiziert auch den Auto-Trigger-Path
