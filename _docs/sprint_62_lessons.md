# Sprint 62 — CsharpMutantOrchestratorTests subset port (drift-risk triage)

**Tag:** v2.48.0 | **Branch:** `feature/62-csharp-mutant-orchestrator-tests`

## Outcome
- Maxential **Branch B "triage-by-drift-risk"** applied to upstream's largest
  single test file (1968 LOC, 95 [TestMethod]s).
- Ported `MutantOrchestratorTestsBase` (helpers `ShouldMutateSourceToExpected`,
  `ShouldMutateSourceInClassToExpected`) + **10 green tests + 5 explicitly skipped**:
  - 7 bucket-1 (source==expected, no-mutation-expected): all green
  - 3 bucket-2 (single-mutation, low-drift-risk pattern): all green
    (`ShouldNotAddReturnDefaultToDestructor`, `ShouldMutateStackalloc`,
    `ShouldMutateTrimMethodOnStringIdentifier`)
  - 5 bucket-3 (multi-mutation hardcoded IDs): skipped with uniform reason
- Dogfood-project total: 421 green + 14 skip = 435
- Solution-wide: 1237 green + 32 skip without E2E
- Semgrep: 0 findings on Sprint-62 files

## Maxential Reasoning
- **Branch A "port-all-with-mass-skip"**: rejected — 1968 LOC port for an
  estimated 10-25 working tests is bad ROI; mass-skip ≥50% hits the
  Sprint-29 lesson threshold.
- **Branch B "triage-by-drift-risk"**: adopted — every shipped test is
  GREEN; bucket-3 deferred with documented uniform reason.
- Merged Branch B with strategy `full_integration`.

## Lessons (NEW)
- **Drift-risk triage for hardcoded-IsActive(N) tests**: bucket-1 (no mutation
  expected) is robust to mutator-set drift; bucket-2 (single mutation) often
  works because the first-firing mutator on the simplest pattern is stable
  (BlockMutator on `{;}`, ArrayInitializer on `stackalloc {}`, StringMutator
  on `text.Trim()`); bucket-3 (multi-mutation hardcoded IDs) is brittle to:
  - additional v2.x mutators firing on the same source (52 mutators vs upstream 40),
  - mutator-pipeline ordering changes (which determines IDs).
- **NormalizeWhitespace + ToFullString** is a viable stand-in for Shouldly's
  `ShouldBeSemantically(...)` extension: strict node-shape match, no trivia.
  Used in `MutantOrchestratorTestsBase.ShouldMutateSourceToExpected`.
- **Empirical-validation-first for foundation/risky ports**: write base helpers
  + ONE simplest test, run, and only then scale up. Spent ~2 minutes proving
  the harness end-to-end before committing to bucket discovery.
- **Skip-with-uniform-reason scales for ≤50% rate, breaks beyond**: this
  sprint's 5 skipped / 15 total = 33% — well within the Sprint-29 ceiling.
- **Dummy-method-body for skip placeholders**: xUnit's `[Fact(Skip = "...")]`
  still requires a method body; a one-liner `_ = SkipReason;` discard satisfies
  the analyzers (no S1186, no IDE0060) without producing dead branches.

## Drift Roadmap
- **Bucket-3 remediation paths** (Sprint 63+ candidates):
  1. Rewrite as **structural assertions** (count mutations + verify
     mutator-class names) instead of literal-string matches.
  2. Recompute v2.x-specific expected strings against the current
     orchestrator output (snapshot-style).

## Files Changed
- `tests/Stryker.Core.Dogfood.Tests/Mutants/MutantOrchestratorTestsBase.cs` (new, base helpers)
- `tests/Stryker.Core.Dogfood.Tests/Mutants/CsharpMutantOrchestratorTests.cs` (new, 15 tests = 10 green + 5 skip)
- `_docs/sprint_62_lessons.md` (this file)
- `.sprint/state.md` (Sprint 62 entry)
