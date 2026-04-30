---
current_sprint: "3"
sprint_goal: "Production Hardening: adopt upstream integration suite + NuGet/CI distribution → tag v1.0.0 (no preview)"
branch: "feature/3-production-hardening"
started_at: "2026-04-30"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---

# Sprint 3 — Production Hardening

**GitHub-Issue:** [#3](https://github.com/pgm1980/stryker-netx/issues/3)
**Base-Tag:** `v1.0.0-preview.2` (Sprint 2 closed)
**Reference-Suite:** `_reference/stryker-4.14.1/integrationtest/` + `_reference/stryker-4.14.1/.github/workflows/`
**Strategie:** Adoption + Adaption von Upstream-Suite, 12 Sub-Phasen, Pillar-A-zuerst, autonom per CLAUDE.md

## Aktueller Phase-Stand

### Pillar A — Real-world hardening
- [ ] **3.1** — Vendor `integrationtest/` mit identity-migration (`dotnet-stryker` → `dotnet-stryker-netx`)
- [ ] **3.2** — NetCore Kategorien (SingleTestProject, MultipleTestProjects, Solution)
- [ ] **3.3** — MTP Kategorien (MSTest/XUnit/NUnit/TUnit/MTPSolution)
- [ ] **3.4** — Edge cases (InitCommand, WebApiWithOpenApi, Generator)
- [ ] **3.5** — Validation framework vendor + green
- [ ] **3.6** — Stryker-on-Stryker (dogfooding, Ziel ≥ 60 % Mutation-Score)

### Pillar B — Distribution
- [ ] **3.7** — NuGet packaging (PackAsTool dotnet-stryker-netx)
- [ ] **3.8** — GitHub Actions CI (build/test/semgrep auf PR)
- [ ] **3.9** — GitHub Actions Integration-Test-Matrix (~30 jobs)
- [ ] **3.10** — GitHub Actions Release pipeline (Tag-Push → NuGet-Push)
- [ ] **3.11** — README + Migration Guide
- [ ] **3.12** — Sprint-3-Closing + Tag `v1.0.0` (production signal)

## Sprint-3-DoD

- [ ] Alle 12 Sub-Phasen ausgeführt
- [ ] `dotnet build stryker-netx.slnx` 0 warnings, 0 errors
- [ ] `dotnet test` 27/27+N pass (mit neuen Validation-Tests)
- [ ] Alle Integration-Test-Kategorien grün auf Windows
- [ ] CI Matrix grün auf mindestens Windows + Ubuntu
- [ ] Stryker-on-Stryker Mutation-Score ≥ 60 %
- [ ] NuGet-Package lokal installierbar (`dotnet tool install -g dotnet-stryker-netx --add-source ./publish`)
- [ ] README enthält install + quickstart + .slnx value-prop
- [ ] Migration Guide komplett
- [ ] GitHub Actions Release Workflow (mindestens dry-run)
- [ ] Public API Stryker.* Libraries unverändert
- [ ] Semgrep clean
- [ ] Lessons-doc `_docs/sprint_3_lessons.md`
- [ ] Tag `v1.0.0` (production signal — kein "preview" mehr)
- [ ] memory_updated=true, documentation_updated=true, semgrep_passed=true, tests_passed=true
- [ ] GitHub-Issue #3 geschlossen
- [ ] housekeeping_done=true

## Verweis

`_reference/stryker-4.14.1/` — vollständige Upstream-Codebase für Adaption-Referenz
`integrationtest/` (geplant Phase 3.1) — vendored integration suite
