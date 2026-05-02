# HANDOVER — Defer-Skip Aufarbeitung Session FINAL → v3.0.5

**Last updated:** Sprint 118 closed (v3.0.5). Session approaching context limit.
**Session status:** v3.0.0 RELEASED + 5 patches + ongoing remediation.

## Current State — v3.0.5
- **Dogfood: 1030 green / 18 skip / 1048 total**
- **Latest tag: v3.0.5**
- All architectural-deferrals continue to be remediated post-v3.0.0

## Cumulative Session (Sprints 95-118, 24 sprints)
- Dogfood: **906/99 → 1030/18** (+124 green, -81 skip, +43 new tests)
- 24 GitHub releases (v2.81.0 → v2.99.0 → v3.0.0 → v3.0.5)
- 1 production bug fixed (MsBuildHelper.GetVersion)
- DEEP_MEMORY.md: comprehensive technical-lessons reference

## Key Discoveries Sprints 114-118 (post-v3.0.0)
1. **JsonReporter** (Sprint 114, +11 green) — Sprint 110 deferral was over-conservative; structural property tests port directly
2. **HtmlReporter** (Sprint 115, +11 green) — same insight as JsonReporter
3. **Spectre.Console `.Width(160)`** (Sprint 117) — missing piece for TestConsole structural-content checks (Sprint 110 first attempt was missing this)
4. **ClearTextReporter** (Sprint 117, +3 green) — ported via Width(160) + structural-content
5. **ClearTextTreeReporter** (Sprint 118, +2 green) — same pattern

## Remaining 18 Skips
**3 PERMANENT:** BuildalyzerHelper, AnalyzerResultExtensions, VsTestHelper-wrong-project
**4 WINDOWS-CONDITIONAL:** InitialBuildProcess (DotnetFramework + MSBuild.exe path)
**11 ARCHITECTURAL-DEFERRALS** (each documented `[Fact(Skip="...")]`):

### Genuinely architectural (non-portable in current v2.x)
- `ProjectOrchestratorTests` — BuildAnalyzerTestsBase removed
- `InputFileResolverTests` — Buildalyzer removed
- `IgnoredMethodMutantFilterTests` — 130 [DataRow] C#-source-as-string requires MemberData rewrite
- `CollectionExpressionMutatorTests` — custom MSTest [CollectionExpressionTest] attribute
- `CSharpCompilingProcessTests` — full Roslyn compile pipeline
- `CSharpRollbackProcessTests` — Roslyn diagnostic-ID matrix (903 LOC)
- `CSharpMutationTestProcessTests` — production drift (Compile not orchestrator-injectable)
- `MutationTestProcessTests` — FullRunScenario+ICoverageAnalyser harness
- `SseServerTest` — real HttpListener (TestServer pattern needed)
- `CsharpMutantOrchestratorTests` — bucket-3 hardcoded mutation IDs (52 vs 40)
- `StrykerCommentTests` — bucket-3

## v3.0.x Future Work — Per-Architectural-Deferral Effort

| File | Effort | Approach |
|---|---|---|
| `CsharpMutantOrchestratorTests` | Single sprint | Structural-assertion rewrite (count + class names) |
| `StrykerCommentTests` | Single sprint | Same as above |
| `CSharpMutationTestProcessTests` | Single sprint | Mock compiler stage |
| `MutationTestProcessTests` | Single sprint | TestRunResult/CoverageRunResult mock-builders |
| `SseServerTest` | Single sprint | TestServer pattern OR port-allocation harness |
| `CSharpCompilingProcessTests` | Single sprint | Roslyn MetadataReference test harness |
| `CSharpRollbackProcessTests` | Single sprint | Diagnostic-ID matrix harness |
| `IgnoredMethodMutantFilterTests` | Single sprint | 130 [DataRow] → MemberData mechanical conversion |
| `CollectionExpressionMutatorTests` | Single sprint | Custom MSTest attribute → MemberData rewrite |
| `ProjectOrchestratorTests` | Multi-sprint | BuildAnalyzerTestsBase v2.x analog |
| `InputFileResolverTests` | Multi-sprint | Same + filesystem/.sln/.slnx parsing |

Total: 9-12 single sprints + 2 multi-sprint efforts = ~13-18 dedicated sprints to reach 0 portable skips.

## Worktree leftover (housekeeping)
3 worktree-directories busy/locked (user must close spawned-session windows):
- `.claude/worktrees/compassionate-mendeleev-5cb549`
- `.claude/worktrees/keen-proskuriakova-34f6b8`
- `.claude/worktrees/practical-darwin-0082de`

## Calculator Test Plan (post-v3.0.0)
v3.0.0+ is installable via NuGet. Plan unblocked.

## DEEP_MEMORY.md
See `memory/DEEP_MEMORY.md` for full technical-lessons reference — Sprints 95-118 lessons:
- Mock patterns + analyzer trap workarounds
- Production-drift categories (IProjectAnalysis migration, Mutation required-init, etc.)
- Spectre.Console `.Width(160)` discovery (Sprint 117)
- Architectural-Deferral Validation Heuristic (Sprint 114-115 lesson)
- Pre-port signature-grep heuristic
- Sprint-Tag-Convention + Worktree-conflict workaround (CLAUDE.md)
