# Sprint 13 — Spec-gap closure: Lessons Learned

**Sprint:** 13 (2026-05-01, autonomous run, two-phase)
**Branch:** `feature/13-v2-spec-gap-closure`
**Base:** v2.0.0 (Sprint 12 closed)
**Final Tag:** `v2.0.1`
**Source:** strict reconciliation against `_input/mutation_framework_comparison.md` performed after the v2.0.0 release surfaced gaps and counting bugs.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| **Phase A** — README + MIGRATION + MEMORY corrections (count typo, weakened claims, expanded roadmap) | ✅ commit 4ae08bd |
| **Phase B** — 8 new operators closing remaining §4.1 / §4.2 / §4.4 spec items | ✅ |
| `ConfigureAwaitMutator` (greenfield, `(false) ↔ (true)`) | ✅ Stronger \| All |
| `DateTimeAddSignMutator` (greenfield, `Add*(n) ↔ Add*(-n)`) | ✅ Stronger \| All |
| `SwitchArmDeletionMutator` (cargo-mutants C3, drop arm if `_`-default) | ✅ Stronger \| All |
| `MemberVariableMutator` (PIT EXP_MEMBER_VARIABLE, type-aware) | ✅ Stronger \| All |
| `TaskWhenAllToWhenAnyMutator` (greenfield, member-rename swap) | ✅ Stronger \| All |
| `ArgumentPropagationMutator` (PIT EXP_ARGUMENT_PROPAGATION, type-aware) | ✅ All only |
| `AsSpanAsMemoryMutator` (greenfield, member-rename swap) | ✅ All only |
| `MethodBodyReplacementMutator` (cargo-mutants C1, type-aware) | ✅ All only |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (none of the new mutators active under Defaults) |
| Semgrep | ✅ clean (0 findings on 8 new files) |
| Tag | `v2.0.1` |

## Final v2.0.1 catalogue: 48 mutators

- **26 v1.x baseline (Defaults profile)** — preserved bit-for-bit
- **+15 Stronger additions** (10 v2.0.0 + 5 v2.0.1)
- **+7 All-only additions** (4 v2.0.0 + 3 v2.0.1)

Total: 26 + 15 + 7 = 48.

## What landed

### Phase A — Documentation reconciliation

The reconciliation against `_input/mutation_framework_comparison.md` after the v2.0.0 release surfaced four classes of issues:

1. **Profile-count typo** in README + MIGRATION: text said "Stronger +9 / All +5" but code carried 10 mutators with `Stronger | All` membership and 4 with `All` only. Total `40` accidentally still added up because `9 + 5 = 14 = 10 + 4`. Corrected.
2. **Overclaim of "all gaps closed"** in Sprint 11/12 lessons + MEMORY entries. The PIT spec §4.1 still had Argument Propagation + Member Variable open; cargo-mutants §4.2 still had Function-Body-Replacement + Match-Arm-Deletion open. Wording softened to "major gaps closed; minor items roadmapped".
3. **Greenfield count overclaim**: 5 v2.0.0 mutators were claimed to cover §4.4 entirely, but only 2 of 11 spec sub-items matched exactly. AsyncAwait emitted `GetAwaiter().GetResult()` not `.Result`; SpanMemory emitted `Slice-zero` not `Span↔ReadOnly` or `AsSpan↔AsMemory`; GenericConstraint dropped all clauses rather than loosening. Documented as semantic deviations.
4. **Roadmap was too high-level** — split into per-section sub-lists (cross-cutting infra / open PIT / open cargo / open greenfield / documented deviations) so future readers can trace what's open vs what shipped.

### Phase B — 8 spec-gap operators

Implementation order ascending in risk per the Maxential session (17 thoughts, 1 branch on the MethodBodyReplacement skip-vs-implement decision — chose implement based on the user's reconciliation explicitly naming it).

#### `ConfigureAwaitMutator` (Stronger | All)

`x.ConfigureAwait(false) ↔ x.ConfigureAwait(true)` literal swap. Always compiles. Catches "is the synchronization-context behavior actually tested?" Conservative scope: only when arg is exactly a boolean literal (variables/expressions skipped).

#### `DateTimeAddSignMutator` (Stronger | All)

`AddDays(n) ↔ AddDays(-n)` for the full `Add*` family on `DateTime` / `DateTimeOffset` / `TimeSpan`. Negates the single argument, dropping a leading `-` if present (so `AddDays(-1)` becomes `AddDays(1)` not `AddDays(--1)`). Method-name filter only — no SemanticModel needed because any `AddDays(int)` accepts a negated `int`.

#### `SwitchArmDeletionMutator` (Stronger | All)

For `switch` expressions ending in a `_`-discard arm and having ≥2 arms total, drops one non-discard arm at a time (one mutation per arm). The discard catches the formerly-routed cases — exhaustiveness-preserving, always compiles. Skipped without a discard arm because deletion would change exhaustiveness.

#### `MemberVariableMutator` (Stronger | All, type-aware)

Resets instance field/property assignments (`_field = expr; → _field = default;`). Uses `SemanticModel.GetSymbolInfo(node.Left).Symbol` to verify `IFieldSymbol` or `IPropertySymbol` and exclude statics, locals, parameters. `default` literal is assignable to every field type — always compiles.

#### `TaskWhenAllToWhenAnyMutator` (Stronger | All)

`Task.WhenAll(...) ↔ Task.WhenAny(...)` symmetric swap. Compile-safety is partial — `WhenAll<T>(...)` returns `Task<T[]>` and `WhenAny<T>(...)` returns `Task<Task<T>>`, so call-sites that index into the result array won't compile after swap. Runner classifies non-compiling mutants as killed (precedent: GenericConstraintMutator).

#### `ArgumentPropagationMutator` (All only, type-aware)

`foo.Bar(a, b) → a` (or `→ b`) when the argument's static type is implicitly convertible to the call's return type. Uses `CSharpCompilation.ClassifyConversion(argType, returnType)` — required cast through `(CSharpCompilation)semanticModel.Compilation` because the base `Compilation.ClassifyConversion` is C#-specific and lives only on the C# subclass. Skips `void` return types and zero-arg calls. Most disruptive operator in the catalogue (every multi-arg invocation × N mutants).

#### `AsSpanAsMemoryMutator` (All only)

Member-rename swap across the four-member set: `AsSpan ↔ AsMemory`, `AsReadOnlySpan ↔ AsReadOnlyMemory`. High compile-failure rate because `Span<T>` is a `ref struct` while `Memory<T>` is heap-allocatable — most call-sites that use `Span<T>` downstream won't accept a `Memory<T>`. Runner classifies non-compiling mutants as killed.

#### `MethodBodyReplacementMutator` (All only, type-aware)

Whole-body replacement: non-void → `{ return default; }`, void → `{ }`. Skips abstract/partial/extern (no body), expression-bodied (round-tripped by other mutators), and async (would lose `Task` wrapping; future iteration could emit `Task.CompletedTask` / `Task.FromResult(default)`). Uses `SemanticModel.GetTypeInfo(node.ReturnType).Type` to detect void. Idempotent — skips bodies that are already trivial-default.

## Process lessons

### 1. `Compilation.ClassifyConversion` lives on `CSharpCompilation`, not the base `Compilation`

The first build attempt failed with `CS1061: 'Compilation' contains no definition for 'ClassifyConversion'`. The C#-specific conversion classification is on `Microsoft.CodeAnalysis.CSharp.CSharpCompilation`. Required pattern in type-aware mutators that need conversion classification:

```csharp
if (semanticModel.Compilation is not CSharpCompilation csharpCompilation)
{
    yield break;
}
var conversion = csharpCompilation.ClassifyConversion(argType, returnType);
```

For VB-style projects this returns false-but-correct (`yield break` short-circuit), preserving v1.x compile behavior in non-C# scenarios.

### 2. Meziantou MA0051 method-length cap (60 lines) bites collection-literal grow-up

The `DefaultMutatorList` grew past 60 lines as Sprint 13's 8 mutators landed. Refactor split it into `BaselineMutators()` (26 v1.x) + `V2OperatorBatches()` (sprint-9-through-13 batches) helpers, both `IEnumerable<IMutator>`. The public method now just composes them. Documented for future sprints.

### 3. Sonar S3358 forbids nested ternary in tuple deconstruction

`var (replacement, label) = a ? (X, Y) : b ? (Z, W) : (null, null);` was rejected. Refactored to `string? replacement = null; string? label = null; if (a) { … } else if (b) { … }`. Same semantics, more readable.

### 4. Phase A reconciliation discipline pays off in Phase B

By doing the documentation correction *before* the new operator implementations, Phase B knew exactly what was actually missing — no risk of re-implementing something the spec might call done. Phase A also produced the test for "did Sprint 13 close the gaps?": the README's Known-Limitations entries that disappear after Sprint 13 ships are the user-visible promise.

### 5. Honest deferral pattern remains load-bearing

Even after v2.0.1, three classes of items remain open: cross-cutting infra (HotSwap, coverage-driven skip, Roslyn-diagnostics filter), CRCR full matrix, and the declaration-site Span↔ReadOnlySpan swap. All explicitly named in the v2.1 roadmap with the rationale ("partial overlap with InlineConstants", "needs new IEquivalentMutantFilter", etc.). This pattern (precedent: Sprint 8 Hot-Swap scaffolding, Sprint 11 CRCR-deferred) is now the project's house style and produces the level of trust the user explicitly demanded with their reconciliation request.

## v2.0 progress map (CLOSED)

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3
[done]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
[done]    Sprint 10 → PIT-1 Operator Batch                  → v2.0.0-preview.5
[done]    Sprint 11 → PIT-2 + cargo-mutants Batch           → v2.0.0-rc.1
[done]    Sprint 12 → Greenfield + Release                  → v2.0.0
[done]    Sprint 13 → Spec-gap closure                      → v2.0.1       ⭐ PATCH ⭐
```

## v2.1 roadmap

- HotSwap engine implementation (`MetadataUpdater.ApplyUpdate` per ADR-016)
- CRCR full matrix (`c → 0, 1, -1, c+1, c-1, -c`)
- Coverage-driven mutation skip (mutmut-style)
- Roslyn Diagnostics filter (mutmut-style, as new `IEquivalentMutantFilter`)
- Declaration-site `Span<T> ↔ ReadOnlySpan<T>` swap
- Generic-constraint *loosening* (separate from the existing drop-all `GenericConstraintMutator`)
