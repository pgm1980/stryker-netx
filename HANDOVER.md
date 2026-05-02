# HANDOVER — v3.0.16 (35 Sprint Session)

## Final State — v3.0.16
- **Dogfood: 1166 green / 15 skip / 1181 total**
- **Latest tag: v3.0.16** (16 v3.0.x patches since v3.0.0)

## Cumulative Session (Sprints 95-129, 35 sprints)
- Dogfood: **906/99 → 1166/15** (+260 green, -84 skip, +176 new tests)
- 35 GitHub releases (v2.81.0 → v3.0.16)
- 1 production bug fixed (MsBuildHelper.GetVersion)

## Sprint 128-129 Newest
- Sprint 128 (v3.0.15): **IgnoredMethodMutantFilter COMPLETE port** — 103/103 green, 0 skip, file fully ported (-1 architectural skip, +27 new green)
- Sprint 129 (v3.0.16): **CollectionExpressionMutator structural-count port** — custom [CollectionExpressionTest] MSTest attribute → xUnit [Theory] + [InlineData] (6 mutation-count tests), full Compile() roundtrip scope-reduced

## Final 15 Skips Breakdown

### 3 PERMANENT
- BuildalyzerHelperTests, AnalyzerResultExtensionsTests, VsTestHelperTests

### 4 WINDOWS-CONDITIONAL
- InitialBuildProcessTests (DotnetFramework + MSBuild.exe)

### 1 KNOWN-BUG (Sprint 23 follow-up)
- CsharpMutantOrchestratorTests.ShouldMutateConditionalExpression_StructuralAssertion (production VisitQualifiedName crash on conditional+LINQ)

### 7 SCOPE-REDUCED-DEFERRALS (each massively reduced)
1. ProjectOrchestratorTests — Buildalyzer-removed (multi-sprint) — **forever-skip per user**
2. InputFileResolverTests — Buildalyzer-removed (multi-sprint) — **forever-skip per user**
3. CollectionExpressionMutatorTests — full Compile() roundtrip (mutation-count covered)
4. CSharpCompilingProcessTests — full Compile() integration (constructor smoke covered)
5. CSharpRollbackProcessTests — full Start() diagnostic-ID matrix (constructor smoke covered)
6. CSharpMutationTestProcessTests — disk-write integration (constructor smoke covered)
7. SseServerTest — real-HttpListener integration (constructor smoke covered)

## Architectural-Deferral Reduction Timeline
- Sprint 113 (v3.0.0): **17** architectural-deferrals
- Sprint 123 (v3.0.10): **9** (-8 via structural rewrites)
- Sprint 127 (v3.0.14): **8** (-1 via FullRunScenario port)
- Sprint 128 (v3.0.15): **7** (-1 via IgnoredMethodMutantFilter COMPLETE)
- Sprint 129 (v3.0.16): **7** (CollectionExpressionMutator scope-reduced, not eliminated)

**Per-user-decision: 2 forever-skip** (ProjectOrchestrator + InputFileResolver Buildalyzer-removed)
**= 5 truly attackable architectural-deferrals remaining** for future v3.0.x patches.

## v3.0.x Future Work — Per-Architectural-Deferral
| File | Effort | Approach |
|---|---|---|
| `CollectionExpressionMutatorTests` | Single sprint | Compile() roundtrip via CsharpCompilingProcess + MetadataReference |
| `CSharpCompilingProcessTests` | Single sprint | Same Roslyn MetadataReference + emit harness |
| `CSharpRollbackProcessTests` | Single sprint | Roslyn diagnostic-ID matrix harness |
| `CSharpMutationTestProcessTests` | Single sprint | Compiler-pipeline mock-harness |
| `SseServerTest` | Single sprint | TestServer pattern OR port-allocation |

All 5 remaining are SINGLE-sprint efforts (no more multi-sprint deferrals attackable per user).

## Reusable Artifacts Produced (16+ patterns)
- `LoggerMockExtensions.EnableAllLogLevels<T>()` (Sprint 96)
- `LoggerMockExtensions.VerifyNoOtherLogCalls<T>()` (Sprint 97)
- `MockJsonReport`, `MockJsonReportFileComponent` test stubs
- `BuildScanDiffTarget` GitDiff mock-builder pattern
- `TestHelper.GetItemPaths` default empty (Sprint 112)
- `MutantOrchestratorTestsBase.CountMutations(source)` (Sprint 119)
- `Mutation NewMutation()` Sprint 2 required-init helper
- `FullRunScenario` mutant+test+coverage harness (Sprint 127, ported from upstream)
- `IgnoredMethodMutantFilterTests` BuildMutantsToFilter/FindEnclosingNode/BuildExpressionMutant helpers (Sprint 124-128)
- Drift-cheat-sheet (Sprint 97 memory)
- Pre-port signature-grep heuristic (Sprint 100/101)
- Architectural-Deferral Validation Heuristic (Sprint 114-115 lesson)
- Spectre.Console `.Width(160)` discovery (Sprint 117)
- Architectural-deferral consolidation pattern (Sprint 108-111)
- Structural-smoke pattern (constructor + interface contract — Sprint 121-125)
- CLAUDE.md docs: Sprint-Tag-Convention + Worktree-conflict workaround (Sprint 99)

## Calculator Test Plan (post-v3.0.0)
v3.0.16 is now installable via NuGet. Plan unblocked.

## DEEP_MEMORY.md
See `memory/DEEP_MEMORY.md` for comprehensive technical-lessons reference (Sprints 95-129).

## Worktree leftover (housekeeping)
3 worktree-directories busy/locked (user must close spawned-session windows).
