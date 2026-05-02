# HANDOVER — v3.0.14 (33 Sprint Session)

**Last updated:** Sprint 127 closed (v3.0.14).

## Final State — v3.0.14
- **Dogfood: 1133 green / 16 skip / 1149 total**
- **Latest tag: v3.0.14** (14 v3.0.x patches since v3.0.0)

## Cumulative Session (Sprints 95-127, 33 sprints)
- Dogfood: **906/99 → 1133/16** (+227 green, -83 skip, +144 new tests)
- 33 GitHub releases (v2.81.0 → v3.0.14)
- 1 production bug fixed (MsBuildHelper.GetVersion)

## Sprint 119-127 Aggressive Architectural-Deferral Remediation
- Sprint 119 (v3.0.6): CsharpMutantOrchestrator structural rewrite (CountMutations helper) — 2 green
- Sprint 120 (v3.0.7): StrykerComment structural rewrite — 2 green
- Sprint 121 (v3.0.8): SseServer constructor+properties (no real listener) — 4 green
- Sprint 122 (v3.0.9): CSharpMutationTestProcess constructor+interface — 2 green
- Sprint 123 (v3.0.10): CollectionExpressionMutator partial port (3 simple-DataRow tests) — 9 green
- Sprint 124 (v3.0.11): IgnoredMethodMutantFilter substantial port (5 tests, 31 green)
- Sprint 125 (v3.0.12): Compiling+Rollback structural-smoke (2 files, 4 green)
- Sprint 126 (v3.0.13): IgnoredMethodMutantFilter expanded (+45 more green)
- Sprint 127 (v3.0.14): **FullRunScenario class ported** + 4 green structural tests (-1 architectural skip)

## Final 16 Skips Breakdown

### 3 PERMANENT
- BuildalyzerHelperTests, AnalyzerResultExtensionsTests, VsTestHelperTests

### 4 WINDOWS-CONDITIONAL
- InitialBuildProcessTests (DotnetFramework + MSBuild.exe)

### 1 KNOWN-BUG
- CsharpMutantOrchestratorTests.ShouldMutateConditionalExpression_StructuralAssertion (Sprint 23 follow-up)

### 8 REDUCED-SCOPE-DEFERRALS (each massively reduced from original placeholders)
1. ProjectOrchestratorTests — Buildalyzer-removed (multi-sprint) — **forever-skip per user**
2. InputFileResolverTests — Buildalyzer-removed (multi-sprint) — **forever-skip per user**
3. IgnoredMethodMutantFilterTests — ~50 remaining edge-case [DataRow] tests
4. CollectionExpressionMutatorTests — single custom-attribute test
5. CSharpCompilingProcessTests — full Compile() integration (constructor smoke covered)
6. CSharpRollbackProcessTests — full Start() diagnostic-ID matrix (constructor smoke covered)
7. CSharpMutationTestProcessTests — disk-write integration (constructor smoke covered)
8. SseServerTest — real-HttpListener integration (constructor smoke covered)

**All 8 architectural-deferrals are now scope-reduced** — each had 1 catch-all skip but
now also has multiple green structural-smoke tests covering constructor + interface contract.
Only the genuinely-hard end-to-end integration paths remain skipped.

## Architectural-Deferral Reduction Timeline
- Sprint 113 (v3.0.0): 17 architectural-deferrals
- Sprint 123 (v3.0.10): 9 architectural-deferrals (-8 via structural rewrites)
- Sprint 127 (v3.0.14): 8 architectural-deferrals (-1 via FullRunScenario port)

**+2 forever-skip per user** (ProjectOrchestrator + InputFileResolver Buildalyzer-removed)
**= 6 truly attackable architectural-deferrals remaining** for future v3.0.x patches.

## v3.0.x Future Work — Per-Architectural-Deferral
| File | Effort | Approach |
|---|---|---|
| `IgnoredMethodMutantFilterTests` (~50 [DataRow]) | Single sprint | Mechanical [InlineData] conversion |
| `CollectionExpressionMutatorTests` (custom-attr) | Single sprint | MemberData rewrite + fixture-loader |
| `CSharpCompilingProcessTests` (full Compile) | Single sprint | Roslyn MetadataReference + emit harness |
| `CSharpRollbackProcessTests` (diagnostic-ID matrix) | Single sprint | Roslyn diagnostic-ID matrix harness |
| `CSharpMutationTestProcessTests` (disk-write) | Single sprint | Compiler-pipeline mock-harness |
| `SseServerTest` (real HttpListener) | Single sprint | TestServer pattern OR port-allocation |

## Reusable Artifacts Produced (15+ patterns)
- `LoggerMockExtensions.EnableAllLogLevels<T>()` (Sprint 96)
- `LoggerMockExtensions.VerifyNoOtherLogCalls<T>()` (Sprint 97)
- `MockJsonReport`, `MockJsonReportFileComponent` test stubs
- `BuildScanDiffTarget` GitDiff mock-builder pattern
- `TestHelper.GetItemPaths` default empty (Sprint 112)
- `MutantOrchestratorTestsBase.CountMutations(source)` — bucket-3 structural-assertion helper (Sprint 119)
- `Mutation NewMutation()` Sprint 2 required-init helper
- `FullRunScenario` mutant+test+coverage harness (Sprint 127, ported from upstream)
- Drift-cheat-sheet (Sprint 97 memory)
- Pre-port signature-grep heuristic (Sprint 100/101)
- Architectural-Deferral Validation Heuristic (Sprint 114-115 lesson)
- Spectre.Console `.Width(160)` discovery (Sprint 117)
- Architectural-deferral consolidation pattern (Sprint 108-111)
- Structural-smoke pattern (constructor + interface contract — Sprint 121-125)
- CLAUDE.md docs: Sprint-Tag-Convention + Worktree-conflict workaround (Sprint 99)

## Calculator Test Plan (post-v3.0.0)
v3.0.14 is now installable via NuGet. Plan unblocked.

## DEEP_MEMORY.md
See `memory/DEEP_MEMORY.md` for comprehensive technical-lessons reference (Sprints 95-127).

## Worktree leftover (housekeeping)
3 worktree-directories busy/locked (user must close spawned-session windows).
