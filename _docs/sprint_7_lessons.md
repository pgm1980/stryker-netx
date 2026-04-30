# Sprint 7 — SemanticModel + Equivalent-Mutant Filter: Lessons Learned

**Sprint:** 7 (2026-05-01, autonomous run)
**Branch:** `feature/7-v2-semanticmodel-equivmutfilter`
**Base:** v2.0.0-preview.1 (Sprint 6 closed)
**Final Tag:** `v2.0.0-preview.2`
**ADRs implemented:** 015 (SemanticModel-driven), 017 (Equivalent-Mutant Filter)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `TypeAwareMutatorBase<T>` base class | ✅ added under `src/Stryker.Core/Mutators/` with `GetExpressionType` + `GetReturnType` helpers |
| `IEquivalentMutantFilter` pipeline-stage interface | ✅ |
| `EquivalentMutantFilterPipeline` coordinator + Default chain | ✅ |
| Initial filter set: `IdentityArithmeticFilter`, `IdempotentBooleanFilter` | ✅ 2 filters in `src/Stryker.Core/Mutants/Filters/` |
| Wired into `CsharpMutantOrchestrator.GenerateMutationsForNode` | ✅ filter check before mutant creation; debug-log for caught equivalents |
| `dotnet build`: | ✅ 0 / 0 |
| `dotnet test`: | ✅ 27/27 |
| Sample E2E: | ✅ 100.00 % |
| Public API of `Stryker.*` libs | additive only (one new abstract class + 4 new types in Stryker.Core); no breaking changes |
| Tag | `v2.0.0-preview.2` |

## What landed

1. **`TypeAwareMutatorBase<T>`** — sister to `MutatorBase<T>` for mutators that need `SemanticModel`. Implements `ITypeAwareMutator` (Sprint-5 marker) and short-circuits to empty when no semantic model is available (matches the contract from ADR-015).
2. **Filter pipeline** — `IEquivalentMutantFilter` + `EquivalentMutantFilterPipeline` + 2 initial filters. Pipeline.Default is the orchestrator's default chain.
3. **Orchestrator integration** — `GenerateMutationsForNode` now consults the pipeline before creating each `Mutant`; equivalent matches are logged at Debug level and skipped silently. Conservative-by-design: filters only catch literal-zero/-one/-true/-false patterns; variable-operand cases are abstained-on so real bugs are still tested.

## What deliberately deferred

- **No Type-Driven mutator yet.** `TypeAwareMutatorBase` is the foundation; the first concrete `ITypeAwareMutator` implementations land in Sprint 9 (Type-Driven Mutators per the v2.0.0 Roadmap).
- **Filter-pipeline DI registration.** `EquivalentMutantFilterPipeline.Default` is a static singleton — fine for v2.0.0-preview.2 but will become a DI binding when filters need per-run options (e.g. `--disable-filter IdentityArithmetic`). Sprint-10+ work item.
- **`IsEquivalent` reporting.** Equivalent matches are only Debug-logged; they don't appear in the mutation report's HTML/JSON output as a distinct "Equivalent" status. PIT does both. Adding a `MutantStatus.Equivalent` value + reporter integration is its own scoped task.
- **More filters.** The catalogue has 2 entries. PIT ships ~12. Each new filter is isolated and additive — easy to add as new operator-derived equivalent patterns surface.

## Process lessons

1. **Serena's `replace_symbol_body` was unreliable for overload disambiguation again.** Same issue as Sprint 6 — for the orchestrator's `GenerateMutationsForNode` modification, I used `Edit` after `Read` (post-Sprint-6 documented fallback). Serena was used for `find_symbol` to locate the method, but the actual body replacement went through `Edit`. This is the right pattern given current Serena limitations.
2. **Roslyn `IsKind` is on `SyntaxNode` and `SyntaxToken`, NOT on the `SyntaxKind` enum directly.** First-pass code wrote `node.Kind().IsKind(...)` which doesn't compile (the enum has no `IsKind` extension). Fixed by either `node.IsKind(SyntaxKind.X)` or direct `node.Kind() == SyntaxKind.X`. Trivial but caught all over.
3. **Conservative filters give you the right risk shape.** Both filters require LITERAL operands (zero/one/true/false). A real `x + y` where `y == 0` at runtime won't be caught — but that's correct: dynamic-zero is a behaviour the test should observe, while compile-time zero is an obvious no-op. Keeping the filters literal-only means we won't accidentally hide a real bug.

## Risks carried forward

- Sprint 8's Trampoline (HIGH-RISK, engine rewrite) is the next sprint and the most likely fix-sprint generator. Filter pipeline being in place gives Sprint-8 some incidental safety: even if the engine refactor breaks edge-cases, the filter layer is independent and won't compound the error.
- Sprint 9 type-driven mutators will be the first real consumers of `TypeAwareMutatorBase` — its `GetReturnType` helper is currently unverified against complex generic-method-return scenarios. May need refinement.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2  ⭐
[next]    Sprint 8  → AssemblyLoadContext Hot-Swap          → v2.0.0-preview.3  (HIGH-RISK)
          Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
          Sprint 10 → Coverage-Driven + PIT-1 Operators     → v2.0.0-preview.5
          Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
