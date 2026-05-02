---
current_sprint: "118"
sprint_goal: "v3.0.0+: post-release defer-skip aufarbeitung continues (Sprint 114-118 → v3.0.5)"
branch: "main"
started_at: "2026-05-02"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — v3.0.5 (Sprint 118 closed)

## Current
- Dogfood: **1030 green / 18 skip / 1048 total**
- Latest tag: **v3.0.5**
- Active sprint: Sprint 118 closed cleanly

## Cumulative session (Sprints 95-118, 24 sprints)
- Dogfood: **906/99 → 1030/18** (+124 green, -81 skip, +43 new tests)
- Tags: v2.81.0 → v2.99.0 → v3.0.0 → v3.0.5 (24 releases)
- 1 production bug fixed (MsBuildHelper.GetVersion)

## Sprint 114-118 (post-v3.0.0 patches)
- Sprint 114 (v3.0.1): JsonReporter structural port (1→11 green) — Sprint 110 deferral was over-conservative
- Sprint 115 (v3.0.2): HtmlReporter structural port (1→11 green) — same insight
- Sprint 116 (v3.0.3): ClearTextTreeReporter minimum-viable (1 green + 1 skip)
- Sprint 117 (v3.0.4): ClearTextReporter full port — `.Width(160)` was the missing piece (1+1skip → 4 green)
- Sprint 118 (v3.0.5): ClearTextTreeReporter full port — same Width(160) trick (1+1skip → 3 green)

## Final 18 skips remaining
- 3 PERMANENT (Buildalyzer×2, VsTestHelper-wrong-project)
- 4 WINDOWS-CONDITIONAL (InitialBuildProcess DotnetFramework)
- 11 ARCHITECTURAL-DEFERRALS:
  1. CSharpCompilingProcessTests
  2. CSharpRollbackProcessTests
  3. InputFileResolverTests
  4. ProjectOrchestratorTests
  5. IgnoredMethodMutantFilterTests
  6. CsharpMutantOrchestratorTests (5 upstream tests consolidated to 1)
  7. StrykerCommentTests (bucket-3)
  8. CSharpMutationTestProcessTests (production drift)
  9. MutationTestProcessTests (FullRunScenario)
  10. CollectionExpressionMutatorTests (custom MSTest attribute)
  11. SseServerTest (real HttpListener)
