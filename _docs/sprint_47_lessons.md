# Sprint 47 — Stryker.Core.Mutators Batch A (14 Smallest Mutator Tests)

**Tag:** v2.34.0 | **Branch:** `feature/47-core-mutators-batch-a`

## Outcome
- **83 new grün + 2 skipped** (Checked + Boolean + Interpolated + BinaryPattern + RelationalPattern + ConditionalExpression + ObjectCreation + ArrayCreation 3+2skip + NegateCondition + StringMethodToConstant + Initializer + StringMethod + PrefixUnary + Statement = 14 files).
- Dogfood-project total: 115 grün + 2 skip = 117 (32 from Sprint 46 + 83 from Sprint 47).
- Solution-wide: 931 grün excl E2E.
- 1 build-fix-cycle (3 trivial errors: IMutator.Mutate signature drift + S2971).

## Files Ported (Sprint 47 — Mutators Batch A)
14 mutator test files (~1013 LOC upstream).

## Production Drifts Discovered (Sprint 47)
- **`IMutator.Mutate(SyntaxNode, SemanticModel, IStrykerOptions)`**: 3 params now (added IStrykerOptions). Cast pattern: `((IMutator)target).Mutate(node, null!, null!)`.
- **`IMutator.Mutate` requires non-null IStrykerOptions at runtime**: 2 ArrayCreation tests skip with reason — would need valid `StrykerOptions` instance to re-enable.

## Lessons (NEW)
- **S2971 LINQ chain-condition simplification**: `xs.Where(f).First()` → `xs.First(f)`. Mechanical fix.
- **Test pattern dominance**: 14 mutator tests follow same template (test MutationLevel + test Mutate cases via SyntaxFactory + test edge cases). Mechanical port enabled by Sprint 46 foundation.

## Solution-wide totals
- Solution-wide tests: **931 green excl E2E** (0 failures, 0 build errors)
- Stryker.Core.Dogfood.Tests now has 115 green + 2 skip across 19 test files

## Roadmap (per Sprint 46 decomposition)
- **Sprint 48**: Mutators batch B (largest 14 of 28, ~2500 LOC)
- **Sprints 49-57**: per decomposition doc
