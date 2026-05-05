---
current_sprint: "139"
sprint_goal: "Bug-Report-Batch — #2 Version-Sync, #3 Update-Hinweis (Folge), #5 IsNullOrEmpty, #1 Stufe 1 Profile×Level Doku"
branch: "feature/139-bug-batch"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 139 (active)

## Goal
Bug-Report-Batch (4 von 6 Bugs). Die verbleibenden zwei (Bug #1 Code, Bug #4) gehen in Sprint 140/141.

## Bug-Fixes in diesem Sprint

### Bug #5 (NIEDRIG) — Log-Rauschen "Could not find a valid analysis for target  for project ..."
**File:** `src/Stryker.Core/Initialisation/InputFileResolver.cs:509`
**Fix:** `if (targetFramework is null)` → `if (string.IsNullOrEmpty(targetFramework))` mit Begründung-Kommentar verweisend auf Sprint 139 Bug #5

### Bug #2 (MITTEL) — Banner zeigt 1.0.0-preview.1 statt aktueller Version
**Files:**
- `Directory.Build.props` Z.37-50: `VersionPrefix=1.0.0`/`VersionSuffix=preview.1` → `VersionPrefix=0.0.0`/`VersionSuffix=localdev` (sinnvoller Local-Default, klar erkennbar als Non-Release)
- `.github/workflows/release.yml` Pack-Step: zusätzlicher Pre-Pack-Build-Step der ALLE Version-Properties aus dem Tag setzt (`-p:Version=$VERSION -p:AssemblyVersion=$ASMVERSION -p:FileVersion=$FILEVERSION -p:InformationalVersion=$VERSION`); der Pack-Step zusätzlich auch noch defensiv

**Wirkung:** Banner zeigt ab v3.0.25 die korrekte Version aus dem Tag. Local-Builds zeigen `0.0.0-localdev` was klar nicht-released ist.

### Bug #3 (MITTEL) — Update-Hinweis "neue Version verfügbar (= installierte Version)"
**Auto-behoben durch Bug #2.** Der Update-Check `StrykerCli.cs:181-191` vergleicht `currentVersion` (aus AssemblyInformationalVersion) mit `latestVersion` (NuGet-API). Mit Bug #2 fixed sind beide identisch → keine falsche Warnung.

### Bug #1 Stufe 1 (HOCH) — Profile-Flag ohne sichtbaren Effekt — Doku-Lücke
**Files:**
- `_config_neuprojekte/Stryker_NetX_Installation.md`: zwei alte Sektionen "Mutation Level" + "Mutation Profile" zusammengefasst zu einer neuen, größeren "Mutation Profile × Level — der conjunctive Filter (PFLICHT-LESSON)" mit:
  - Klarer Erklärung der Filter-Logik
  - Konsequenz: Profile alleine reicht nicht
  - Anwender-Fehler-Beispiel
  - Empfohlene Kombinationen (Defaults+Standard / **Stronger+Advanced** / All+Complete / Defaults+Basic für Smoke)
  - Mutation-Profile-Pool-Tabelle
  - Mutation-Level-Wirkung-Tabelle
  - Strategie für Projekte
  - Out-of-the-box stryker-config.json
  - Vorgriff auf Sprint 140 Code-Fix (geplante Warning)
- `_config_neuprojekte/CLAUDE_CS.md`: Mutation-Profile-Beispiele auf empfohlene Kombination upgedatet, mit Verweis auf vollständige Erklärung

**Sprint 140 (Stufe 2):** Code-Warning bei `profile != Defaults && level <= Standard`. ToT + Maxential vorab.

## Tag-Strategy für Sprint 139
**v3.0.25** — erster echter Sprint nach Sprint 138 (CI-fix-only). Push triggers automatisch release.yml dank push-tag-trigger; verifiziert auch den Auto-Trigger-Path (Sprint 138 hatte nur workflow_dispatch verifiziert).

Sprint-Tag-Convention beachten:
1. `gh pr merge --squash --delete-branch`
2. `git checkout main && git pull --ff-only`
3. `git tag -a v3.0.25 -m "..."`
4. `git push origin v3.0.25`

## Test plan
- [ ] Lokaler Build mit `-p:Version=3.0.25-rc.1 -p:InformationalVersion=3.0.25-rc.1` — verifiziert Bug #2 Fix (Banner sollte 3.0.25-rc.1 zeigen)
- [ ] Lokaler Pack-Test
- [ ] Tests grün
- [ ] Semgrep grün
- [ ] PR + squash-merge
- [ ] Tag v3.0.25 → release.yml auto-trigger → NuGet-push
- [ ] NuGet.org `dotnet-stryker-netx 3.0.25` öffentlich

## Out of scope (für Sprint 140/141)
- Bug #1 Stufe 2 (Code-Warning bei mismatch)
- Bug #4 (`--version` Tool-Convention)
- Hinweis #7 (NuGet-Indexing-Doku)
- Hinweis #8 (Multi-Source-Project-UX)
