# Sprint 46 — Stryker.Core.UnitTest Foundation + 5 Smallest Leaf Tests

**Tag:** v2.33.0 | **Branch:** `feature/46-core-unittest-foundation`

## Outcome
- Decomposition document produced (`_docs/sprint_46_decomposition.md`) — 11 sub-sprints projected for full track.
- New `tests/Stryker.Core.Dogfood.Tests/` project + slnx (avoids collision with existing `tests/Stryker.Core.Tests/` 388-test project).
- **32/32 grün, zero skips** across 5 leaf-test files (~250 LOC).
- 1 build-fix-cycle (8 trivial errors all production-drift adaptations).
- Solution-wide: 848 grün excl E2E.

## Files Ported (Sprint 46 — Foundation Slate)
| File | Tests | Notes |
|------|-------|-------|
| `ExclusionPatternTests` | 3 | Tests `Stryker.Configuration.ExclusionPattern` (readonly partial struct in production) |
| `Helpers/TextSpanHelperTests` | 17 | 2 Theories with 8+8 InlineData cases |
| `Mutators/PostfixUnaryMutatorTests` | 3 | First mutator-test; production drift on ApplyMutations(node, null!) signature |
| `MutantFilters/ExcludeMutationMutantFilterTests` | 3 | Sprint 2 production drift: Mutation has required members |
| `StrykerRunResultTests` | 6 | Two Theories with 3+3 InlineData cases |
| **Total** | **32** | — |

## Production Drifts Discovered (Sprint 46)
- **Sprint 2 modernization**: `Mutation` has required members `OriginalNode`, `ReplacementNode`, `DisplayName` — test setup must provide placeholders (e.g., `SyntaxFactory.IdentifierName("_")`).
- **Sprint ?**: `IMutator.ApplyMutations(node, SemanticModel)` 2nd param became non-nullable in stryker-netx — use `null!` to suppress nullability warning.
- **`SyntaxNode.IsKind(SyntaxKind)` extension method** namespace-dependent — replaced with `node.Kind() == SyntaxKind.X` (always available, simpler).
- **Production class rename**: upstream `ExcludeMutationMutantFilter` → stryker-netx `IgnoreMutationMutantFilter` (test class name preserved per upstream convention but tests target the new name).

## Lessons (NEW)
- **Project naming convention `*.Dogfood.Tests`**: avoids name collision with our own quality-assurance test projects (Sprint 18-23 `tests/Stryker.Core.Tests/`).
- **TestHelpers reuse**: Sprint 25 `tests/Stryker.TestHelpers/` (TestBase/TestHelper/etc.) is reusable via ProjectReference — no duplication needed for upstream `: TestBase` ports. Drop the `: TestBase` inheritance in xUnit ports (xUnit doesn't need test base class for [Fact] discovery).
- **IDE0300 collection-expression**: `new[] { x }` → `[x]` mechanical fix.

## Decomposition: 11 Sub-Sprints Projected (Sprints 46-57)
See `_docs/sprint_46_decomposition.md` for full table. Key risks:
- Sprint 53 (Initialisation/Buildalyzer) — HIGHEST drift risk
- Sprints 47-48 (Mutators 28 files) — MEDIUM drift from Sprints 6-13 mutator overhauls
- Sprints 51-52 (Reporters 20 files) — MEDIUM drift from Sprint 16 JsonReport rewrite

## Solution-wide totals after Sprint 46
- Solution-wide tests: **848 green excl E2E** (0 failures, 0 build errors)

## Roadmap
- **Sprint 47**: Mutators batch A (smallest 14, ~1500 LOC)
- **Sprint 48-57**: per decomposition document
- **Optional sub-sprints from Sprint 45 Investigation**: VsTest-Refactor, MTP-mock-server, IProjectAnalysis-mock-builder
