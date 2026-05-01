---
current_sprint: "20"
sprint_goal: "Integration Tests across 6 layers (Orchestrator+Mutator-Pipeline, Profile-Filter, Filter-Pipeline+Orchestrator, MutantPlacer+injection, Reporter, Configuration→Options) → v2.7.0"
branch: "feature/20-integration-tests"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 20 — Integration Tests

**GitHub-Issue:** [#20](https://github.com/pgm1980/stryker-netx/issues/20)
**Base-Tag:** `v2.6.0` (Sprint 19 closed)
**Final-Tag:** `v2.7.0`
**Reference:** Sprint 20 Maxential 13 thoughts (1 branch D1 project-location) + ToT 8 nodes (6-layer scoping).

## Sprint Context (auto-saved before compaction at 2026-05-01T12:12:51Z)

### Current Branch
feature/20-integration-tests

### Last 10 Commits
```
ae8045b Merge branch 'feature/19-filter-tests-fscheck-properties' into main
47ae432 test(hardening): Sprint 19 — Item B (filter tests) + Item C (FsCheck properties) → v2.6.0
d3a4777 Merge branch 'feature/18-hardening-mutator-unit-tests' into main
abdb4d8 test(hardening): Sprint 18 — Hardening Super-Sprint A: 256 unit tests for all 52 mutators → v2.5.0
bb2b8a5 Merge branch 'feature/17-v2.4-final-long-tail' into main
d32e5d7 feat(filter+operator): Sprint 17 — v2.4.0 final long-tail rest (2 ship + 1 defer)
9fdba49 Merge branch 'feature/16-v2.3-long-tail' into main
fb64061 feat(long-tail): Sprint 16 — v2.3.0 long-tail items (3 ship + 2 defer)
71d7dc0 Merge branch 'feature/15-v2.2-hotswap-walkback' into main
06b79a8 chore(walk-back): Sprint 15 — v2.2.0 walks back ADR-016 (HotSwap engine), ADR-021 + ADR-022
```

### Recently Changed Files
```
.sprint/state.md
MIGRATION-v1-to-v2.md
README.md
_docs/architecture spec/architecture_specification.md
_docs/sprint_15_lessons.md
_docs/sprint_16_lessons.md
_docs/sprint_17_lessons.md
_docs/sprint_18_lessons.md
_docs/sprint_19_lessons.md
integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs
src/Stryker.Abstractions/IMutationEngine.cs
src/Stryker.Abstractions/MutationEngine.cs
src/Stryker.Abstractions/Options/IStrykerOptions.cs
src/Stryker.Configuration/Options/Inputs/MutationEngineInput.cs
src/Stryker.Configuration/Options/StrykerInputs.cs
src/Stryker.Configuration/Options/StrykerOptions.cs
src/Stryker.Core/Engines/HotSwapEngine.cs
src/Stryker.Core/Engines/RecompileEngine.cs
src/Stryker.Core/Mutants/CsharpMutantOrchestrator.cs
src/Stryker.Core/Mutants/Filters/EquivalentMutantFilterPipeline.cs
```

### Uncommitted Changes
```
 M .sprint/state.md
?? tests/Stryker.Core.Tests/Integration/
```
