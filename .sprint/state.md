---
current_sprint: "4"
sprint_goal: "Bug Elimination: fix Sprint-3-deferred Bug-5 + green all Pillar-A phases → tag v1.0.0 (production)"
branch: "feature/4-bug-elimination"
started_at: "2026-04-30"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---

# Sprint 4 — Bug Elimination

**GitHub-Issue:** [#4](https://github.com/pgm1980/stryker-netx/issues/4)
**Base-Tag:** `v1.0.0-rc.1` (Sprint 3 closed PARTIAL)
**Target-Tag:** `v1.0.0` (production)
**Strategie:** Bug-5 zuerst (gating defect), dann alle deferred Pillar-A Phasen, dann ship.

## Aktueller Phase-Stand

- [ ] **4.1** — Audit + Maxential decision on Bug-5 fix approach (Path A vs Path B)
- [ ] **4.2** — Fix Bug-5 (selected approach) — mutation engine project-reference handling
- [ ] **4.3** — Re-run NetCore SingleTestProject — verify the fix
- [ ] **4.4** — NetCore MultipleTestProjects + Solution categories
- [ ] **4.5** — MTP categories (MSTest/XUnit/NUnit/TUnit/MTPSolution)
- [ ] **4.6** — Edge cases (InitCommand, WebApiWithOpenApi, Generator)
- [ ] **4.7** — Validation framework vendor + green
- [ ] **4.8** — Stryker-on-Stryker dogfooding (≥ 60 % Mutation-Score)
- [ ] **4.9** — NetFramework category (was OOS in Sprint 3)
- [ ] **4.10** — Re-enable `integration-test.yaml` `pull_request` trigger
- [ ] **4.11** — Sprint close + Tag `v1.0.0` (production signal)

## Sprint-4-DoD

- [ ] Bug-5 fixed
- [ ] Alle Integration-Test-Kategorien (NetCore + MTP + Edge cases) grün auf Windows
- [ ] NetFramework category grün ODER documented als Linux/macOS-not-applicable
- [ ] Validation framework asserts mutation correctness (alle Trait-Categories grün)
- [ ] Stryker-on-Stryker Mutation-Score ≥ 60 %
- [ ] `dotnet build stryker-netx.slnx` 0 warnings, 0 errors
- [ ] `dotnet test` all unit tests pass
- [ ] Sample E2E (`--solution Sample.slnx`) 100 %
- [ ] Sample E2E (`--config-file stryker-config.json`) 100 %
- [ ] Public API Stryker.* Libraries unverändert
- [ ] Semgrep clean (0 findings auf gesamtes src/)
- [ ] Lessons-doc `_docs/sprint_4_lessons.md`
- [ ] Tag `v1.0.0` (production signal — kein "preview", kein "rc")
- [ ] memory_updated=true, documentation_updated=true, semgrep_passed=true, tests_passed=true
- [ ] GitHub-Issue #4 geschlossen
- [ ] housekeeping_done=true

## Verweis

- `_docs/sprint_3_lessons.md` — Bug-5 root-cause analysis + path forward
- `integrationtest/` — vendored upstream suite (Phase 3.1)
- `_reference/stryker-4.14.1/` — Upstream reference for cross-checking behaviour
