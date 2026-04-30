# Sprint 10 — PIT-1 Operator Batch: Lessons Learned

**Sprint:** 10 (2026-05-01, autonomous run)
**Branch:** `feature/10-v2-pit-batch-1`
**Base:** v2.0.0-preview.4 (Sprint 9 closed)
**Final Tag:** `v2.0.0-preview.5`
**Source:** comparison.md §4.1 (PIT-derived gaps)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `InlineConstantsMutator` (PIT INLINE_CONSTS — Stryker.NET v1.x had nothing for numeric literals) | ✅ profile `Stronger | All` |
| `AodMutator` (Arithmetic Operator Deletion — `a+b → a` AND `a+b → b`) | ✅ profile `Stronger | All` |
| `RorMatrixMutator` (ROR full 5-replacement matrix per relational op) | ✅ profile `Stronger | All` |
| `UoiMutator` (Unary Operator Insertion — `x → x++/++x/x--/--x`) | ✅ profile `All` only |
| All 4 wired into `DefaultMutatorList` | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (none of the new mutators active under Defaults) |
| Tag | `v2.0.0-preview.5` |

## What landed

### InlineConstantsMutator

The **biggest single missing-mutator gap** flagged in comparison.md §4.1. Stryker.NET v1.x had no mutator for numeric literals at all (only string + boolean). PIT and mutmut both do. Per-literal output: two mutations (`n + 1` and `n - 1`). Detects classic off-by-one bugs.

### AodMutator (Arithmetic Operator Deletion)

`a + b → a` AND `a + b → b` for every arithmetic op (`+`, `-`, `*`, `/`, `%`). Two mutations per arithmetic site. Catches "is the operator necessary" tests. Famously noisy in literature — `Stronger | All` only.

### RorMatrixMutator (Relational Operator Replacement Vollmatrix)

The PIT ROR full matrix: for each relational operator (`<`, `<=`, `>`, `>=`, `==`, `!=`), emit ALL 5 alternative replacements (vs Stryker.NET v1.x's single boundary swap + single negation in `BinaryExpressionMutator`). 5 mutations per relational site is huge on real code — `Stronger | All` only. The Defaults-profile single-swap mutators stay active and cover the most-likely cases.

### UoiMutator (Unary Operator Insertion)

`x → x++/++x/x--/--x` on bare identifier references. 4 mutations per identifier use. Most aggressive PIT-1 operator — `All` profile only. Conservative scope: skips assignment LHS, member-access heads, invocation targets, ref/out arguments — all places where wrapping in increment would either fail to compile or change semantics dramatically.

## What deferred to Sprint 11+

- **CRCR (Constant Replacement: c → 0, 1, -1, c+1, c-1, -c)** — overlaps with InlineConstantsMutator (the `+1`/`-1` substitutions) but adds the literal-zero-and-one drops + negation. Sprint 11.
- **Coverage-driven mutation skip (D3)** — needs coverage-data plumbing through the orchestrator + a pre-filter step that checks per-line coverage before emitting mutations. Sprint 11+.
- **D1 Roslyn Diagnostics filter** — natural fit for the `IEquivalentMutantFilter` pipeline; Sprint 11.

## Process lessons

1. **Sonar S101 enforces PascalCase for acronyms.** `AODMutator`/`UOIMutator`/`RORMatrixMutator` had to become `AodMutator`/`UoiMutator`/`RorMatrixMutator`. Build-error caught it; renamed file + class. Documented for future acronym-named mutators in Sprints 11-12.
2. **Profile-based opt-in pays off again.** All 4 new operators are `Stronger | All` only (3) or `All` only (1). Default profile = current 26 + TypeDrivenReturn (already off-by-default) + new 4 (off-by-default). Sample E2E preserved at 100 % by construction with zero special-casing.
3. **`yield break` analyzer fight.** Sonar S3626 flagged the trailing `yield break` in `InlineConstantsMutator`'s float-branch as redundant (since it's the last statement before method end). Removed. Documented.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3
[done]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
[done]    Sprint 10 → PIT-1 Operator Batch                  → v2.0.0-preview.5  ⭐
[next]    Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
