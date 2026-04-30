# Sprint 5 — v2.0.0 Architecture Foundation: Lessons Learned

**Sprint:** 5 (2026-04-30, autonomous run)
**Branch:** `feature/5-v2-architecture-foundation`
**Base-Tag:** `v1.0.0`
**Final Tag:** none (ADR-only sprint per macro-plan)
**Reference inputs:** `_input/mutation_framework_comparison.md`, `_references/{pitest,cargo-mutants,mutmut}`

## Sprint Outcome

| Metric | Result |
|--------|--------|
| New ADRs (013–018) | ✅ 6 — appended to `_docs/architecture spec/architecture_specification.md` |
| New stub interfaces in Stryker.Abstractions | ✅ `IMutatorGroup`, `IMutationOperator`, `ITypeAwareMutator` |
| New stub enum/attribute | ✅ `MutationProfile` (Flags), `MutationProfileMembershipAttribute` |
| Behavior change | ✅ none (additive interfaces, no implementations) |
| `dotnet build` | ✅ 0 warnings, 0 errors |
| `dotnet test` | ✅ 27/27 pass |
| Sample E2E | ✅ 100.00 % Mutation-Score |
| Semgrep | ✅ 0 findings on 70 files |
| Public API of `Stryker.*` libraries | additive only (5 new types in `Stryker.Abstractions`); no breaking changes |
| Tag | none (intentional — see ADR-only sprint scope) |

## What ADRs 013–018 commit us to

| # | ADR | Implementation sprint | One-line decision |
|---|-----|----|----|
| 013 | AST/IL Hybrid | already in effect | Roslyn-AST primary; IL selectively for Hot-Swap + EquivMutFilter |
| 014 | Operator Hierarchy | Sprint 6 | `IMutatorGroup → IMutator → IMutationOperator` (replaces flat-list) |
| 015 | SemanticModel-driven Mutators | Sprint 7 | `ITypeAwareMutator` marker + `TypeAwareMutatorBase<TNode>` helpers |
| 016 | AssemblyLoadContext Hot-Swap | Sprint 8 (HIGH-RISK) | Optional `--engine hotswap` mode for 5–10× speed |
| 017 | Equivalent-Mutant Filter | Sprint 7 | `IEquivalentMutantFilter` pipeline-stage |
| 018 | Mutation Profiles | Sprint 6 | `MutationProfile` Flags enum (Defaults / Stronger / All) — orthogonal to `MutationLevel` |

## Process lessons

1. **The macro Maxential (9 thoughts) + ToT (3 branches scored) from the v2.0.0 roadmap-planning carries through Sprint 5 cleanly.** I didn't run a separate Maxential per ADR — each ADR cited the macro decision instead. This works because the architectural choices were already converged at the macro level; Sprint 5 was about *documenting* those choices, not *re-deciding* them. For Sprint 8 (Hot-Swap implementation), a fresh Maxential WILL be needed because the implementation choices (ALC vs MetadataUpdater) are independent decisions.

2. **ADR-only sprint pattern works.** No code paths changed, no tests broken, no E2E regression — all by construction. The risk profile of an ADR-only sprint is essentially zero. Worth using as a deliberate cadence-element when a release line opens up (Sprint 5 = open v2.0.0; the next major version line will benefit from a similar Sprint-N-as-ADR-anchor pattern).

3. **`v1.0.0` discipline pays off.** The 27/27 + Sample E2E + Semgrep regression-checks at Sprint 5's end confirm v1.0.0 is robust against additive API changes. This builds confidence for Sprint 6's invasive operator-hierarchy refactor.

4. **Reference repos paid off immediately.** Cloning PIT/cargo-mutants/mutmut took <1 minute total (background, parallel) and produced searchable codebases for ADR-014's hierarchy-design comparison and ADR-016's trampoline-strategy notes. Worth keeping.

## Risks carried into Sprint 6+

- **Sprint 6 (operator-hierarchy refactor of 24 mutators)** is the highest near-term risk. Mitigation: 27/27 tests + Sample E2E + integration suite (Sprint 4) act as safety nets. If Sprint 6 partial: 6b fix-sprint slot reserved.
- **ADR-001 nuance.** The original ADR-001 phrasing about Stryker.Abstractions' Roslyn-purity is softer in practice than ADR-014's text suggests — `IMutator` already references `SyntaxNode` + `SemanticModel`. New interfaces (`IMutationOperator`, `IMutatorGroup`) follow the existing pattern (Roslyn types allowed in interface signatures) rather than aspiring to a stricter purity. Documented here as historical clarification.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[next]    Sprint 6  → Operator-Hierarchy + Profile refactor → v2.0.0-preview.1
          Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
          Sprint 8  → AssemblyLoadContext Hot-Swap          → v2.0.0-preview.3
          Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
          Sprint 10 → Coverage-Driven + PIT-1 Operators     → v2.0.0-preview.5
          Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
