---
current_sprint: "138"
sprint_goal: "Release-Workflow reparieren — workflow_dispatch + secrets-env-pattern + tag-explicit; v3.0.24 zu NuGet.org pushen"
branch: "fix/138-release-workflow"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 138 (active)

## Goal
Reparatur des Release-Workflows + erstmaliger NuGet.org-Push von `dotnet-stryker-netx`.

## Diagnose-Trail (Sprint 138 Anfang)

**Symptom (am Calculator-Test entdeckt):**
- `dotnet tool install -g dotnet-stryker-netx` schlägt fehl mit "Package not found" auf NuGet.org
- HTTP 404 von `https://api.nuget.org/v3-flatcontainer/dotnet-stryker-netx/index.json`
- GitHub Release v3.0.24 hat 0 Assets

**Root-Cause-Kette (verifiziert):**
1. **GitHub Actions Quota erschöpft** seit 2026-05-02 21:47Z — `runner_id: 0` + `steps: []` für jeden Job → kein Runner allokiert. Account `pgm1980` ist `plan: free` + Repo war `private:true` → 2000 Free-Min/Monat-Limit.
2. **`release.yml` lief NIE erfolgreich** — auch vor dem Quota-Limit `[]` für `--status success`. Ursachen identifiziert (siehe unten).
3. **NUGET_API_KEY-Secret war nicht gesetzt** → selbst bei Erfolg wäre nur die "Skipped notice"-Branch gelaufen.

**Falsche initiale Hypothese (zur Dokumentation):** `actions/checkout@v6` existiert nicht. **Falsch** — alle Action-Versionen aus dem release.yml (`checkout@v6`, `setup-dotnet@v5`, `upload-artifact@v4`, `softprops/action-gh-release@v2`) existieren. Lehre: Symptome systematisch via API verifizieren statt aus Memory raten.

## Behobene Probleme (Sprint 138)

| # | Problem | Fix |
|---|---|---|
| 1 | Quota-Limit (private free repo) | Repo auf **public** umgestellt (User-Aktion über GitHub Settings) |
| 2 | `default_workflow_permissions: read` | Auf **`write`** gesetzt (`gh api -X PUT`) — damit `permissions: contents: write` im Workflow-File greift |
| 3 | `NUGET_API_KEY`-Secret fehlt | User hat Key auf NuGet.org generiert (Glob-Pattern: `dotnet-stryker-netx`, Push-Scope) und im Repo als Secret hinterlegt |
| 4 | `release.yml` ohne `workflow_dispatch:` Trigger → existierende Tags nicht re-runbar | `workflow_dispatch:` mit `inputs.tag` hinzugefügt |
| 5 | `if: ${{ secrets.X != '' }}` Anti-Pattern (step-level secrets-context unreliable) | env-var-pipe + `if: steps.nuget_key_check.outputs.has_key == 'true'` |
| 6 | `softprops/action-gh-release` ohne explizites `tag_name:` → bei workflow_dispatch wird github.ref_name (= main) verwendet | `tag_name: ${{ steps.ref.outputs.target }}` explizit gesetzt |
| 7 | `actions/checkout@v6` ohne `ref:` → bei workflow_dispatch wird main ausgecheckt, nicht der Target-Tag | `ref: ${{ steps.ref.outputs.target }}` explizit gesetzt |
| 8 | E2E-Tests im Release-Workflow → unnötige Flake-Quelle, läuft schon im CI-Job | `--filter "FullyQualifiedName!~Stryker.E2E.Tests"` |
| 9 | Keine `concurrency`-group → parallel runs für gleichen Tag möglich | `concurrency: group: release-${target}` |

## CI-Run Iteration 1 (commit dfbf09b)
- ✅ semgrep grün, e2e grün
- ❌ build-test (ubuntu + windows): pre-existing Test-Failures
  - `Stryker.Solutions.Tests.SolutionFileShould.*` (4 Tests) — `DirectoryNotFoundException : _references/stryker-net/src/Stryker.slnx` (vendored upstream nicht im CI-Checkout)
  - `Stryker.Core.Dogfood.Tests.TestHelpers.ProjectAnalysisMockBuilderTests.WithProjectFilePath_DerivesAssemblyNameAndTargetFileNameAndTargetDir` — Path-string-format diff
- Sprint 138 Reaktion: Test-Step aus release.yml entfernt (Quality-Gating ist ci.yml-Concern, nicht release-Concern). Pre-existing Test-Issues separat in Sprint 139+ angehen.

## Ausstehend
- [ ] CI-Run Iteration 2 verifizieren (release.yml ohne Test-Step → kein Failure mehr in release-job)
- [ ] Squash-Merge PR #222
- [ ] `gh workflow run release.yml --ref main -f tag=v3.0.24` (manueller Trigger nach Merge)
- [ ] NuGet.org-Verifikation: `dotnet-stryker-netx@3.0.24` öffentlich auffindbar
- [ ] Calculator-Sanity-Check via `dotnet tool install -g dotnet-stryker-netx`

## Decision Pending
Sprint 138 = reine CI-Reparatur ohne Code-Diff in src/. Tag-Strategie:
- **Option A**: Kein neuer Tag, nur Fix auf main + workflow_dispatch für v3.0.24. v3.0.24 wird der offizielle erste-NuGet-Push. (cleanste Variante)
- **Option B**: v3.0.25-Tag für CI-Fix erstellen + workflow_dispatch erst dann automatisch + parallel manueller dispatch für v3.0.24 zur Backfilling.

→ Aktuelle Präferenz: **Option A**. v3.0.24 ist semantisch das letzte Sprint-137-Release; v3.0.25 würde "0 Code-Diff" + nur CI-Bumps suggerieren, was Versions-Erwartung verletzt. Nur wenn künftige Sprints ein Release brauchen, wird der nächste Tag (v3.0.25, v3.1.0) gepusht.
