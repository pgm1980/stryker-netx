# Sprint 46+ — Stryker.Core.UnitTest Track Decomposition

**Largest dogfood track**: 25,192 LOC across 161 files in `_references/stryker-net/src/Stryker.Core/Stryker.Core.UnitTest/`.

## Directory Inventory

| Directory | Files | Notes |
|-----------|-------|-------|
| Options | 48 | Largest — needs 2-3 sub-sprints |
| Mutators | 28 | Includes TestMutator helper (12 LOC) — needs 2 sub-sprints |
| Reporters | 20 | + Json/ + Progress/ subdirs — needs 2 sub-sprints |
| ROOT | 12 | StrykerRunner, ExclusionPattern, FilePattern, helpers — Sprint 46 foundation candidates |
| Initialisation | 11 | + Buildalyzer/ subdir — **HIGHEST DRIFT RISK** (Sprint 1 Phase 9 Buildalyzer removal) |
| MutantFilters | 9 | Filter pipeline — 1 sub-sprint |
| ProjectComponents | 6 | + SourceProjects/ subdir — 1 sub-sprint |
| Mutants | 5 | Orchestrator-related — 1 sub-sprint |
| Baseline | 5 | + Providers/ + Utils/ subdirs — 1 sub-sprint |
| TestResources | 4 | Resource files (no tests) |
| MutationTest | 4 | 1 sub-sprint |
| Helpers | 2 | VsTestHelperTests + TextSpanHelperTests — Sprint 46 foundation |
| Compiling | 2 | 1 sub-sprint |
| ToolHelpers | 1 | BuildalyzerHelperTests — DRIFT RISK (Buildalyzer removal) |
| InjectedHelpers | 1 | 1 file |
| DiffProviders | 1 | 1 file |
| DashboardCompare | 1 | 1 file |
| Clients | 1 | 1 file |
| **Total** | **161** | — |

## Existing Infrastructure (Reusable)
- `tests/Stryker.TestHelpers/` (Sprint 25): TestBase, TestLoggerFactory, TestHelper, StringExtensions, MockExtensions, LoggerMockExtensions — **reusable via ProjectReference**, no duplication needed.

## Project Layout Decision
- **New project**: `tests/Stryker.Core.Dogfood.Tests/`
- **Rationale**: avoid namespace collision with existing `tests/Stryker.Core.Tests/` (Sprint 18-23, 388 green tests for our own quality assurance)
- **CI implication**: separate project allows filter-based exclusion if needed

## Decomposition Plan (Sprints 46-55+)

| Sprint | Scope | Est. LOC | Drift Risk |
|--------|-------|----------|------------|
| **46 (this)** | Foundation + 4 smallest leaf tests (smoke) | ~250 | LOW |
| 47 | Mutators batch A (smallest 14) | ~1500 | MEDIUM (depends on Sprint-1 mutator overhauls) |
| 48 | Mutators batch B (largest 14) | ~2500 | MEDIUM |
| 49 | Options batch A (Inputs/ subdir, 24 files) | ~3000 | LOW |
| 50 | Options batch B (top-level, 24 files) | ~3000 | LOW |
| 51 | Reporters batch A (top-level + Json/) | ~2500 | LOW |
| 52 | Reporters batch B (Progress/ + remaining) | ~2000 | LOW |
| 53 | Initialisation (11 files) + ToolHelpers (1) | ~3000 | **HIGH** (Buildalyzer removal — Sprint 1 Phase 9) |
| 54 | ROOT (8 remaining) + Helpers (2) + small dirs | ~1500 | LOW |
| 55 | MutantFilters (9) + Mutants (5) + MutationTest (4) | ~2000 | MEDIUM |
| 56 | ProjectComponents (6) + Baseline (5) + Compiling (2) | ~2500 | MEDIUM |
| 57 | DashboardCompare + Clients + DiffProviders + InjectedHelpers (4 single-file dirs) | ~500 | LOW |

**Total estimated: 11 sub-sprints (Sprints 46-57)**.

## Sprint 46 Foundation Scope
- New `tests/Stryker.Core.Dogfood.Tests/` project + slnx entry
- ProjectReference to `tests/Stryker.TestHelpers/` (reuse existing helpers)
- ProjectReference to `src/Stryker.Core/`
- Port 4 smallest leaf-test files (smoke, ~250 LOC):
  1. `ExclusionPatternTests.cs` (38 LOC, ROOT-level)
  2. `Helpers/TextSpanHelperTests.cs` (60 LOC)
  3. `Mutators/PostfixUnaryMutatorTests.cs` (37 LOC)
  4. `MutantFilters/ExcludeMutationMutantFilterTests.cs` (57 LOC)
  5. `StrykerRunResultTests.cs` (60 LOC, ROOT-level)

## Risk Notes
- **Sprint 53 (Initialisation/Buildalyzer)** is the highest-risk sprint. Sprint 1 Phase 9 removed Buildalyzer; tests that mock `IAnalyzerResult` will need rewriting against `IProjectAnalysis` (Sprint 25 lesson). Consider IProjectAnalysis-mock-builder sub-sprint (sized in Sprint 45 Investigation, ~2 days) BEFORE Sprint 53.
- **Sprints 47-48 (Mutators)** may have drift from Sprint 6's MutationProfile/MutationLevel hierarchy refactor + Sprints 9-13 new mutators. May need test updates to match v2.x mutator APIs.
- **Sprints 51-52 (Reporters)** may have drift from Sprint 16 JsonReport hybrid source-gen rewrite. Test JSON-shape expectations may differ.

## Out-of-Sprint-46 Roadmap
- See above table for Sprints 47-57 sequencing.
- Optional sub-sprints from Sprint 45 Investigation report still apply (VsTest-Refactor, MTP-mock-server harness, IProjectAnalysis-mock-builder).
