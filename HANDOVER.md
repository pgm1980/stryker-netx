# HANDOVER — v3.0.21 (40 Sprint Session)

## Final State — v3.0.21
- **Dogfood: 1173 green / 11 skip / 1184 total**
- **Latest tag: v3.0.21** (21 v3.0.x patches since v3.0.0)

## Cumulative Session (Sprints 95-134, 40 sprints)
- Dogfood: **906/99 → 1173/11** (+267 green, -88 skip, +179 new tests)
- 40 GitHub releases (v2.81.0 → v3.0.21)
- 1 production bug fixed (MsBuildHelper.GetVersion)

## Sprint 130-134 — Final Architectural-Deferral Attack
- Sprint 130 (v3.0.17): **SseServer real-HttpListener integration** — architectural-deferral ELIMINATED. 6 green incl. real HttpListener+HttpClient roundtrip. Production bug spawned (Dispose double-close).
- Sprint 131 (v3.0.18): **CSharpMutationTestProcess disk-write integration** — architectural-deferral ELIMINATED. End-to-end Mutate→CompileMutations→DiskWrite test passes.
- Sprint 132 (v3.0.19): **CSharpCompilingProcess Compile() integration** — architectural-deferral ELIMINATED. Used Sprint 131 setup pattern, real Compile() emits valid IL.
- Sprint 133 (v3.0.20): **CSharpRollbackProcess Start() integration** — architectural-deferral scope-reduced. 2 new green Start() integration tests, 1 edge case deferred (real-syntax-error rollback).
- Sprint 134 (v3.0.21): **CollectionExpressionMutator Compile() roundtrip** — architectural-deferral ELIMINATED. Real Compile() succeeds on collection-expression source.

## Final 11 Skips Breakdown

### 3 PERMANENT (Sprint 1 architectural removal)
- BuildalyzerHelperTests, AnalyzerResultExtensionsTests, VsTestHelperTests

### 4 WINDOWS-CONDITIONAL (legitimate platform-skip)
- InitialBuildProcessTests (DotnetFramework + MSBuild.exe path)

### 1 KNOWN-BUG (Sprint 23 follow-up)
- CsharpMutantOrchestratorTests.ShouldMutateConditionalExpression_StructuralAssertion — production VisitQualifiedName crash on conditional+LINQ inputs

### 2 FOREVER-SKIP (per user — Buildalyzer-removed Sprint 1)
- ProjectOrchestratorTests
- InputFileResolverTests

### 1 REDUCED-SCOPE-DEFERRAL
- CSharpRollbackProcessTests.Start_ShouldHandleDiagnosticsWithNullSourceTree (real-syntax-error rollback edge case requires actual rollback-eligible mutations)

## Architectural-Deferral Reduction Timeline
- Sprint 113 (v3.0.0): **17** architectural-deferrals
- Sprint 123 (v3.0.10): **9** (-8 via structural rewrites)
- Sprint 127 (v3.0.14): **8** (-1 via FullRunScenario port)
- Sprint 128 (v3.0.15): **7** (-1 via IgnoredMethodMutantFilter COMPLETE)
- Sprint 130 (v3.0.17): **6** (-1 via SseServer real-listener)
- Sprint 131 (v3.0.18): **5** (-1 via CSharpMutationTestProcess disk-write)
- Sprint 132 (v3.0.19): **4** (-1 via CSharpCompilingProcess Compile())
- Sprint 133 (v3.0.20): **4** (scope-reduced — 1 edge case kept)
- Sprint 134 (v3.0.21): **3** (-1 via CollectionExpressionMutator Compile() roundtrip)

**Net reduction: 17 → 3 architectural-deferrals (= 14 eliminated/transformed).**
- 2 of 3 remaining are **forever-skip per user** (ProjectOrchestrator + InputFileResolver Buildalyzer-removed)
- 1 of 3 is a single edge case (CSharpRollbackProcess null-SourceTree)

## Skip Categories Final
| Category | Count | Status |
|---|---|---|
| Permanent (architectural removal) | 3 | Forever |
| Windows-conditional | 4 | Legitimate |
| Known production bug | 1 | Fix needed |
| User forever-skip (Buildalyzer-removed) | 2 | Per user decision |
| Reduced-scope edge case | 1 | Could be remediated |
| **TOTAL** | **11** | |

## Reusable Artifacts Produced (17+ patterns)
- `LoggerMockExtensions.EnableAllLogLevels<T>()` (Sprint 96)
- `LoggerMockExtensions.VerifyNoOtherLogCalls<T>()` (Sprint 97)
- `MockJsonReport`, `MockJsonReportFileComponent` test stubs
- `BuildScanDiffTarget` GitDiff mock-builder pattern
- `TestHelper.GetItemPaths` default empty (Sprint 112)
- `MutantOrchestratorTestsBase.CountMutations(source)` (Sprint 119)
- `Mutation NewMutation()` Sprint 2 required-init helper
- `FullRunScenario` mutant+test+coverage harness (Sprint 127)
- `IgnoredMethodMutantFilterTests` BuildMutantsToFilter helpers (Sprint 124-128)
- **End-to-end Compile() integration setup pattern** (Sprint 131-134) — proven setup for full Roslyn pipeline tests
- **Real-HttpListener integration with HttpClient** (Sprint 130)
- Drift-cheat-sheet (Sprint 97)
- Pre-port signature-grep heuristic (Sprint 100/101)
- Architectural-Deferral Validation Heuristic (Sprint 114-115 lesson)
- Spectre.Console `.Width(160)` discovery (Sprint 117)
- Architectural-deferral consolidation pattern (Sprint 108-111)
- Structural-smoke pattern (Sprint 121-125)
- CLAUDE.md docs: Sprint-Tag-Convention + Worktree-conflict workaround (Sprint 99)

## v3.0.x Future Work
Only 1 truly attackable architectural-deferral remains:
- `CSharpRollbackProcessTests.Start_ShouldHandleDiagnosticsWithNullSourceTree` — needs setup that produces both real-syntax-error diagnostics AND rollback-eligible mutations in syntax tree

Plus 1 production bug to fix:
- `CsharpMutantOrchestratorTests.ShouldMutateConditionalExpression_StructuralAssertion` — Sprint 23 VisitQualifiedName crash needs production fix in UoiMutator or DoNotMutateOrchestrator<QualifiedNameSyntax>

Plus 1 spawned bug fix in queue:
- `SseServer.Dispose` double-close edge case (spawned task at end of Sprint 130)

## Calculator Test Plan (post-v3.0.0)
v3.0.21 is now installable via NuGet. Plan unblocked.

## DEEP_MEMORY.md
See `memory/DEEP_MEMORY.md` for comprehensive technical-lessons reference (Sprints 95-134).

## Worktree leftover (housekeeping)
3 worktree-directories busy/locked (user must close spawned-session windows before file-system cleanup).
