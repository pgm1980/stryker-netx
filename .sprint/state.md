---
current_sprint: "167"
sprint_goal: "Hotfix Sprint-166 regression: --mutate scope filter crashed CSharpCompilation.Create with ArgumentNullException(trees[N]) because skipped files left CsharpFileLeaf.MutatedSyntaxTree at its null! default. Seed unmutated original tree on out-of-scope files so compilation pipeline stays whole. Drive-by Extract-Method to satisfy MA0051. Single-file fix + regression test in Stryker.Core.Dogfood.Tests. Target tag v3.2.19 (patch — no API change, backwards-compat)."
branch: "fix/167-mutate-out-of-scope-null-tree"
started_at: "2026-05-24"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Session State — Sprint 167 (v3.2.19 prep)

## Trigger

Bug report from `filesystem-mcp-server` project (Sprint 51, 2026-05-24) filed at
`_bug_reporting/stryker-netx-3.2.18-mutate-filter-trees-null.md`. Symptom:
`dotnet stryker-netx --mutate "**/Some/File.cs"` crashes immediately after
"Disable-directive validation: scanned N files in --mutate scope (M skipped)"
with `System.ArgumentNullException: trees[N]` from Roslyn's
`CSharpCompilation.AddSyntaxTrees`. Whole-project scans (no `--mutate`) succeed.

## Root cause

Sprint 166 commit `82622e3` (ADR-046 §A) introduced `IsFileInMutateScope` skip
branch in `CsharpMutationProcess.Mutate`. The branch cleared `file.Mutants = []`
but never set `file.MutatedSyntaxTree`. Default is `null!`. Downstream:
`CsharpFileLeaf.CompilationSyntaxTrees => [MutatedSyntaxTree]` propagated the
null entries into `CSharpCompilation.Create`, which threw on the first null.

The bug report hypothesised partial-class-specificity (LoggerMessage source
generators in Aisess Infrastructure project). Verified incorrect: triggers for
any --mutate that excludes ≥1 file. Partial-class projects just amplify
visibility because skipped sibling halves leave types incomplete in IL.

## Fix (1-line semantic + Extract-Method drive-by)

`src/Stryker.Core/MutationTest/CsharpMutationProcess.cs`:
```csharp
// In the skip-branch:
file.MutatedSyntaxTree = file.SyntaxTree;  // unmutated original participates in compilation
```

Drive-by: extracted `OrchestratePerFileMutations` from `Mutate` because the
added comment + assignment pushed Mutate over MA0051 60-line cap (same pattern
as Sprint 13 `ApplyMutationInputs` and Sprint 22 `ConfigureCli`).

## Regression test

`tests/Stryker.Core.Dogfood.Tests/MutationTest/CSharpMutationTestProcessTests.cs`:
`Mutate_ShouldNotCrash_WhenMutateScopeExcludesSomeFiles` — two-file project
(Sample.cs + Helper.cs), `--mutate "**/Sample.cs"`, asserts no throw + assembly
written. Setup extracted to `BuildTwoFileMutationInput` for MA0051.

Pre-fix: throws ArgumentNullException(trees[1]) from CSharpCompilation.AddSyntaxTrees.
Post-fix: 111 ms green.

## Build/test summary

- Solution-wide build: 0 warnings, 0 errors
- Solution-wide tests: 2105 passing (+1 vs Sprint 166), 0 failures, 20 pre-existing skips
- Stryker.Core.Dogfood.Tests: 1190 → 1191 (+1 regression test)
- Semgrep auto-config on changed production file: 0 findings

## Status

- [x] Fix committed (`79b2cf1`) on `fix/167-mutate-out-of-scope-null-tree`
- [x] PR #259 opened: https://github.com/pgm1980/stryker-netx/pull/259
- [ ] PR merged + branch deleted
- [ ] Tag v3.2.19 on squash-merge commit + GitHub release + NuGet publish
- [ ] MEMORY.md `project_sprint167_closed.md` entry + index update
- [ ] `housekeeping_done: true` after all above

## Backwards-compatibility

100% — default `--mutate=**/*` matches every file so the skip branch never
executes. Only narrow-scope users (the feature this PR repairs) see a behavior
change, and that change is "no longer crashes." No API change, no enum change,
no CLI change. Patch-level v3.2.19 appropriate.

## Out-of-scope (NOT included in v3.2.19)

- `_bug_reporting/STRYKER_NETX_ANOMALIES_AND_BUGS_v2.md` review — separate
  triage sprint; v2 supersedes the v1 report closed in Sprint 166.
- Bug report §10 "Related Stryker-netx Behaviors" (`--coverage-analysis` removed,
  PascalCase-only reporter names) — intentional Sprint-1 modernizations, not
  regressions. CHANGELOG note worth adding when next minor release ships.
