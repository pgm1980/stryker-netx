# Migration: stryker-netx v1.x → v2.0.0

> **TL;DR — Zero breaking changes for the default profile.** Upgrading from v1.x to v2.0.0 requires no `stryker-config.json` edits, no CLI flag changes, and no test rewrites. Run the same command you ran before; you'll get the same mutation report. The new operators are **opt-in** via `--mutation-profile Stronger` or `--mutation-profile All`.

---

## What changed

v2.0.0 is the catalogue-and-architecture release. It adds 14 net-new mutators, an opt-in `MutationProfile` axis, a SemanticModel-driven type-aware mutator infrastructure, an Equivalent-Mutant Filter pipeline, and the scaffolding for a `MetadataUpdater`-based `HotSwap` engine.

### Behavior preservation

The single most important contract for v2.0.0:

> **Default profile = exact v1.x parity.** All 14 new mutators carry `[MutationProfileMembership(Stronger | All)]` or `[MutationProfileMembership(All)]`. Under the default `Defaults` profile, the orchestrator filters them out. The 26 v1.x mutators still run in the same order with the same output.

This is verified end-to-end: the project's reference Sample E2E mutation run scored 100% under the default profile in every sprint from Sprint 9 through Sprint 12.

---

## Upgrade steps

### 1. Update the global tool

```bash
dotnet tool update -g dotnet-stryker-netx --version 2.0.0
```

### 2. Run as before — no config changes needed

```bash
dotnet stryker-netx                 # uses your existing stryker-config.json
dotnet stryker-netx --solution X.slnx
dotnet stryker-netx --config-file stryker-config.json
```

You should see exactly the same mutant counts, the same scores, and the same reporter output as v1.x.

### 3. (Optional) Opt into the expanded catalogue

```bash
dotnet stryker-netx --mutation-profile Stronger
```

Or in `stryker-config.json`:

```json
{
  "stryker-config": {
    "mutation-profile": "Stronger"
  }
}
```

The `Stronger` profile activates 9 additional mutators that close the gap with PIT and cargo-mutants. The `All` profile activates 5 more that are intentionally noisy — useful for high-stakes code review but not appropriate as a CI gate.

---

## What you get under each profile

### `Defaults` (= v1.x parity)

| Family | Mutators |
|--------|----------|
| Arithmetic | BinaryExpression, Math |
| Relational | BinaryExpression, RelationalPattern |
| Logical | Boolean, NegateCondition, ConditionalExpression, BinaryPattern |
| Unary | PrefixUnary, PostfixUnary |
| Strings | String, StringEmpty, StringMethod, StringMethodToConstant, InterpolatedString |
| Collections | Linq, Collection, Initializer, ArrayCreation, ObjectCreation |
| Other | Block, Statement, Assignment, Checked, Regex, NullCoalescing, IsPatternExpression |

26 mutators total.

### `Stronger` (= Defaults + catalogue closure)

Adds:
v2.0.0 adds:
- **TypeDrivenReturnMutator** (cargo-mutants C2 — type-driven default returns: `Task<T>` → `Task.FromResult(default)`, `IEnumerable<T>` → `Enumerable.Empty<T>()`, etc.)
- **InlineConstantsMutator** (PIT INLINE_CONSTS — every numeric literal `n` → `n+1` and `n-1`)
- **AodMutator** (PIT-style Arithmetic Operator Deletion — `a + b → a` AND `a + b → b`)
- **RorMatrixMutator** (PIT ROR full 5-replacement matrix per relational op)
- **ConstructorNullMutator** (PIT CONSTRUCTOR_CALLS — `new Foo(...) → null` where the surrounding context permits)
- **MatchGuardMutator** (cargo-mutants C4 — `case X when expr → when true/false`)
- **WithExpressionMutator** (cargo-mutants C5 — record `with { ... }` field deletion)
- **AsyncAwaitMutator** (greenfield — `await x → x.GetAwaiter().GetResult()`)
- **DateTimeMutator** (greenfield — `DateTime.Now ↔ UtcNow`)
- **SpanMemoryMutator** (greenfield — `span.Slice(s, l) → span.Slice(0, l)`)

v2.0.1 adds (spec-gap closure):
- **ConfigureAwaitMutator** (greenfield — `ConfigureAwait(false) ↔ ConfigureAwait(true)` literal swap)
- **DateTimeAddSignMutator** (greenfield — `AddDays(n) ↔ AddDays(-n)` for the full `Add*` family)
- **SwitchArmDeletionMutator** (cargo-mutants C3 — drop a non-default switch-expression arm; only when a `_`-default exists to catch deleted cases)
- **MemberVariableMutator** (PIT EXP_MEMBER_VARIABLE — instance field/property assignment reset to `default`)
- **TaskWhenAllToWhenAnyMutator** (greenfield — `Task.WhenAll(...) ↔ Task.WhenAny(...)` swap)

41 mutators total (= 26 + 15).

### `All` (= Stronger + the noisiest experimental operators)

v2.0.0 adds:
- **UoiMutator** (PIT Unary Operator Insertion — `x → x++/++x/x--/--x` on every identifier; very high mutation volume)
- **NakedReceiverMutator** (PIT EXP_NAKED_RECEIVER — `a.M(b) → a`)
- **ExceptionSwapMutator** (greenfield — `throw new ArgumentNullException → throw new ArgumentException` and family swaps)
- **GenericConstraintMutator** (greenfield — drops `where T : ...` clauses; may produce non-compiling mutants which the runner correctly classifies as killed)

v2.0.1 adds (spec-gap closure):
- **ArgumentPropagationMutator** (PIT EXP_ARGUMENT_PROPAGATION — `foo.Bar(a, b) → a` when arg-type is implicitly convertible to return-type; type-aware via SemanticModel)
- **AsSpanAsMemoryMutator** (greenfield — `AsSpan() ↔ AsMemory()` and read-only variants; high compile-failure rate, runner classifies failed-compile as killed)
- **MethodBodyReplacementMutator** (cargo-mutants C1 "function-body replacement genre" — non-void method bodies replaced with `{ return default; }`, void with `{ }`; skips async/abstract/partial/extern/expression-bodied)

48 mutators total (= 26 + 15 + 7).

---

## Performance and noise expectations

| Profile | Mutation count vs. Defaults | Wall-clock vs. Defaults | When to use |
|---------|----------------------------|--------------------------|-------------|
| Defaults | 1.0× | 1.0× | CI gate, regular development |
| Stronger | ~1.5–2× | ~1.5–2× | Quality bar before a release; periodic deep audit |
| All | ~3–5× | ~3–5× | High-stakes code review (e.g. crypto, payment), academic mutation testing studies |

These are rough estimates from the project's own dogfooding. Real numbers depend heavily on code shape (UoiMutator scales with the count of bare identifier references; GenericConstraintMutator scales with the count of generic methods).

**Recommendation:** Keep `Defaults` for CI. Run `Stronger` periodically (weekly cron / pre-release). Reach for `All` only on isolated subsystems you genuinely want to grind.

---

## New CLI flags

```
--mutation-profile <Defaults|Stronger|All>   default: Defaults
--engine <Recompile|HotSwap>                 default: Recompile
                                             (HotSwap is scaffolding-only in v2.0.0)
```

## Removed / changed CLI flags

**None.** Every v1.x flag works exactly as before.

## Removed / changed config fields

**None.** Every v1.x config field works exactly as before.

## API surface (for embedders)

The `Stryker.Abstractions` and `Stryker.Core` public surfaces are **strictly additive**:

- `MutationProfile` enum (new)
- `MutationProfileMembershipAttribute` (new)
- `IMutationOperator`, `IMutatorGroup`, `ITypeAwareMutator` (new interfaces — stubs in v2.0.0, intended for future operator-hierarchy work)
- `IMutationEngine` + `MutationEngine` enum (new)
- `IStrykerOptions.MutationProfile` property (new — defaults to `MutationProfile.Defaults`)
- `IStrykerOptions.MutationEngine` property (new — defaults to `MutationEngine.Recompile`)
- `EquivalentMutantFilterPipeline` (new — internal in v2.0.0, may surface in v2.1)

No type was renamed, removed, or had a method signature change.

## Roadmap (v2.0.x → v2.1)

After v2.0.1 the operator catalogue closes nearly all operator-shaped recommendations from `_input/mutation_framework_comparison.md`. Remaining open items below.

### Cross-cutting infrastructure (v2.0.x → v2.1)

- **HotSwap engine implementation** (`MetadataUpdater.ApplyUpdate`-based — see `_docs/architecture spec/architecture_specification.md` ADR-016)
- **Coverage-driven mutation skip** (mutmut-style: skip mutants in lines with no test coverage)
- **Roslyn Diagnostics filter** (mutmut-style: feed compilation diagnostics into the equivalence-filter pipeline as a new `IEquivalentMutantFilter`)

### Open operators (§4.1 / §4.4)

- **CRCR full matrix** (constant-replacement composite — partial overlap with `InlineConstantsMutator`'s `n+1` / `n-1` axes; missing are the `0`, `1`, `-1`, `-c` substitutions)
- **Span/Memory — declaration-site `Span<T> ↔ ReadOnlySpan<T>` swap** (the v2.0.0 `SpanMemoryMutator` emits `Slice(start, length) → Slice(0, length)`; the v2.0.1 `AsSpanAsMemoryMutator` covers the `AsSpan()/AsMemory()` invocation swap; the declaration-type swap is the third remaining piece)
- **Access-Modifier-Mutation** (`private ↔ public`) — controversial; kept off the roadmap unless requested

### Documented semantic deviations from the spec

These are intentionally implemented differently from the spec's exact wording — they catch a closely-related bug class but with distinct semantics. Spelled out so future readers are not surprised:

- **`AsyncAwaitMutator`** emits `await x → x.GetAwaiter().GetResult()` rather than the spec's `await x → x.Result`. Both are sync-over-async substitutions; `.Result` wraps exceptions in `AggregateException`, `GetAwaiter().GetResult()` unwraps. Either way, tests that fail to await the result fail the mutant.
- **`GenericConstraintMutator`** drops the entire `where T : ...` clause set rather than performing the spec-listed *loosening* (`where T : class → where T : new()`). Closely related but more aggressive. A loosening variant may be added under a separate mutator in v2.1.
- **`SpanMemoryMutator`** targets `span.Slice(start, length) → span.Slice(0, length)`. The spec asked for `Span<T> ↔ ReadOnlySpan<T>` and `AsSpan() → AsMemory()` — the latter ships in v2.0.1 as `AsSpanAsMemoryMutator`; the former is roadmapped.

## Questions or issues

Open a GitHub issue at https://github.com/pgm1980/stryker-netx/issues. Reference the per-sprint lessons docs in [`_docs/`](_docs/) for the rationale behind each architectural decision (ADRs 013–018) and each new operator family.
