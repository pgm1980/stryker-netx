---
current_sprint: "174"
sprint_goal: "360°-Analyse B — Mutations-Pipeline (Findings-only). ~96 Dateien / ~9,5k LOC: Mutants/ (42, inkl. MutantPlacer, CsharpNodeOrchestrators, CommentParser, RollbackProcess), Instrumentation (8), InjectedHelpers (3 — läuft in User-Prozessen), Compiling (6), MutantFilters (13), CoverageAnalysis (2), MutationTest (7), ProjectComponents (12), root (3). Pflicht-Schwerpunkte aus Sprint-173-Register: F-14 (AddEndingReturn-Asymmetrie BaseFunctionOrchestrator:117–142 — warum wirkt die CS0161-Mitigation im Mutations-Pfad nicht?), F-10 (Deklarations-Level-Mutation-Hosting), #277-Folgefrage (Orchestrator-try/catch-Robustheit), F-08 (Equivalence-Pipeline-Erweiterbarkeit), F-25 (No-op-Return-Abdeckung). Register _docs/analysis/sprint_174_findings.md batch-weise committet. Teil des Programms 173–178 (Issue #276). Kein Tag (Findings-only)."
branch: "feature/174-analysis-b-mutation-pipeline"
started_at: "2026-06-11"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 174 (360°-Analyse B: Mutations-Pipeline)

## Kontext

Sprint 173 (Analyse A) geschlossen: `c0ab231` auf main, 41 Findings, Issues
#277/#278/#279/#280. Programm-Issue #276 trägt weiter. Nightly-Dogfood läuft
als 10/11-Sicherheitsnetz.

## Scope Sprint 174

| Block | Dateien | LOC |
|-------|---------|-----|
| Mutants/ (Orchestratoren, Placer, CommentParser, Rollback) | 42 | ~3.075 |
| MutantFilters/ | 13 | ~891 |
| ProjectComponents/ | 12 | ~558 |
| Instrumentation/ | 8 | ~358 |
| MutationTest/ | 7 | ~689 |
| Compiling/ | 6 | ~840 |
| InjectedHelpers/ (läuft im User-Prozess!) | 3 | ~393 |
| CoverageAnalysis/ | 2 | ~224 |
| root (3 Dateien) | 3 | ~120 |

## Status

- [x] Branch + state.md + Register angelegt
- [ ] Batch-Lektüre (Pflicht-Schwerpunkte zuerst: BaseFunctionOrchestrator-Umfeld)
- [ ] Verifikations-/Issue-Phase, Register-PR, Close

## Sprint Context (auto-saved before compaction at 2026-06-11T17:23:54Z)

### Current Branch
feature/174-analysis-b-mutation-pipeline

### Last 10 Commits
```
a4079ed analysis(sprint-174): G-03/G-04 — F-10 resolved (declaration-level mutations have no hosting path; null-forgiven expression! lands in ADR-028 guard as 'orchestrator bug' CE)
f6d5eb4 analysis(sprint-174): setup + G-01 — F-14 mechanics resolved (EndingReturnEngine never invoked on mutation path)
c0ab231 analysis(sprint-173): 360°-Analyse A — Mutatoren-Katalog komplett, 41 Findings (kein Tag) (#281)
5919ace chore(sprint-172): close Sprint 172 — all housekeeping items done (v3.3.3 shipped) (#275)
aef20e0 fix(sprint-172): ADR-052 MatchesFilter — Filter-Seite via GetFileName, nie GetFileNameWithoutExtension (closes #270, v3.3.3 prep) (#272)
1115be7 chore(sprint-171): close Sprint 171 — all housekeeping items done (no tag) (#271)
d1669a6 feat(sprint-171): ADR-051 Dogfood-Configs netx-Layout + Fixture-Restore-Pflicht + NetFramework honest-deferred (kein Tag) (#269)
26e36c1 chore(sprint-170): close Sprint 170 — all housekeeping items done (v3.3.2 shipped) (#267)
01df790 feat(sprint-170): ADR-050 CI-Reanimation — NuGet-Audit-Bump + Nightly-Dogfood Scheduled-Mode-Repair + Doc-Drift (v3.3.2 prep) (#266)
f67b989 chore(sprint-169): close Sprint 169 — all housekeeping items done (v3.3.1 shipped) (#264)
```

### Recently Changed Files
```
.sprint/state.md
_docs/analysis/sprint_173_findings.md
_docs/analysis/sprint_174_findings.md
_docs/architecture spec/architecture_specification.md
src/Stryker.Core/Initialisation/InputFileResolver.cs
tests/Stryker.Core.Tests/Initialisation/ProjectFilterMatchingTests.cs
```
