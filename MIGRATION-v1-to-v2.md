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

v2.1.0 adds (filter pipeline + operator completion):
- **ConstantReplacementMutator** (PIT CRCR full matrix — `c → 0, 1, -1, -c`; complements `InlineConstantsMutator`'s `c+1`/`c-1` axes)
- **GenericConstraintLoosenMutator** (per-clause constraint loosening: `where T : class → where T : new()` etc.; complements v2.0.0's drop-all `GenericConstraintMutator`)

v2.3.0 adds (long-tail):
- **AsyncAwaitResultMutator** (greenfield — `await x → x.Result`; spec-faithful semantic variant of v2.0.0's `AsyncAwaitMutator` which emits `GetAwaiter().GetResult()`). Both ship — different exception-wrapping signatures catch different test-spec assumptions.

44 mutators total (= 26 + 18).

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

v2.1.0 adds:
- **SpanReadOnlySpanDeclarationMutator** (greenfield — declaration-site `Span<T> ↔ ReadOnlySpan<T>` and `Memory<T> ↔ ReadOnlyMemory<T>` swap; complements v2.0.1's invocation-site `AsSpanAsMemoryMutator`)

52 mutators total (= 26 + 18 + 8).

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
--engine <Recompile|HotSwap>                 deprecated v2.2.0 — accepted as no-op shim
                                             with deprecation warning per ADR-021
```

## Removed / changed CLI flags

**None.** Every v1.x flag works exactly as before. The `--engine` flag added in v2.0.0 was deprecated in v2.2.0 (see "Documented removals (v2.2)" below) but remains accepted for backwards compatibility.

## Removed / changed config fields

**None.** Every v1.x config field works exactly as before.

## API surface (for embedders)

The `Stryker.Abstractions` and `Stryker.Core` public surfaces are **strictly additive**:

- `MutationProfile` enum (new)
- `MutationProfileMembershipAttribute` (new)
- `IMutationOperator`, `IMutatorGroup`, `ITypeAwareMutator` (new interfaces — stubs in v2.0.0, intended for future operator-hierarchy work)
- `IMutationEngine` + `MutationEngine` enum (new in v2.0.0; **deprecated `[Obsolete]` in v2.2.0** per ADR-021)
- `IStrykerOptions.MutationProfile` property (new — defaults to `MutationProfile.Defaults`)
- `IStrykerOptions.MutationEngine` property (new in v2.0.0; **deprecated `[Obsolete]` in v2.2.0** per ADR-021)
- `EquivalentMutantFilterPipeline` (new — internal in v2.0.0, may surface in v2.1)

No type was renamed, removed, or had a method signature change.

## Documented removals (v2.2)

v2.2.0 walks back ADR-016 per ADR-021 — the HotSwap mutation engine is removed. **No breaking changes for users:** the `--engine` flag still accepts `Recompile` and `HotSwap` (logged as deprecated), the `MutationEngine` enum and related abstractions remain as `[Obsolete]` source-compat shims. The implementation classes (`HotSwapEngine`, `RecompileEngine`) have been deleted because they had no execution path.

**Why:** pre-implementation recherche for v2.2.0 surfaced that ADR-016's "5–10× perf boost" claim was based on a wrong mental model of Stryker.NET's cost structure. Stryker.NET compiles ALL mutations into a SINGLE assembly with runtime `ActiveMutationId` switching — there is no per-mutant compile to optimize away with a hot-swap pattern. The actual perf-relevant configuration is `--coverage-analysis` (default `perTest`), which has shipped since v1.x.

See [ADR-021](_docs/architecture%20spec/architecture_specification.md) for the full recherche trail and [ADR-022 (Proposed)](_docs/architecture%20spec/architecture_specification.md) for the legitimate future perf direction (incremental mutation testing — file-watcher + change-driven re-run, no commitment).

**v3.0 (future):** the deprecated `MutationEngine` symbols may be hard-removed.

## Roadmap (v2.2 → v2.x)

After v2.1.0, the operator-shaped recommendations from `_input/mutation_framework_comparison.md` are essentially exhausted. After v2.2.0, the misguided HotSwap engine work is cleaned up. The remaining roadmap is small.

### v2.x — long-tail items

- **Access-Modifier-Mutation** (`private ↔ public`) — controversial; kept off the roadmap unless requested.
- **Generic-constraint loosening — interface-target case**: `GenericConstraintLoosenMutator` (v2.1.0) treats interface-typed constraints by emitting a `class`-constraint replacement rather than the per-interface alternative; a future iteration could add ICloneable→IDisposable-style swaps if the bug-class proves real.
- **RoslynDiagnostics filter — semantic errors**: the v2.1.0 filter checks parser diagnostics only. A future v2.2 ADR may extend the `IEquivalentMutantFilter` contract to carry a `Compilation` parameter so semantic-error pre-filtering becomes possible.

### Already implemented (v1.x — the comparison.md roadmap entry was a documentation gap, not a code gap)

- **Coverage-driven mutation skip** — shipped as `OptimizationModes.SkipUncoveredMutants` and `CoverageBasedTest`, exposed via the `--coverage-analysis` flag (default `perTest`). The `CoverageAnalyser` runs an initial coverage capture pass and skips uncovered mutants — exactly the mutmut "skip mutants in lines with no test coverage" semantic.

### Documented semantic deviations from the spec

These are intentionally implemented differently from the spec's exact wording — they catch a closely-related bug class but with distinct semantics. Spelled out so future readers are not surprised:

- **`AsyncAwaitMutator`** emits `await x → x.GetAwaiter().GetResult()` rather than the spec's `await x → x.Result`. Both are sync-over-async substitutions; `.Result` wraps exceptions in `AggregateException`, `GetAwaiter().GetResult()` unwraps. Either way, tests that fail to await the result fail the mutant.
- **`GenericConstraintMutator` (drop-all, v2.0.0) + `GenericConstraintLoosenMutator` (per-clause, v2.1.0)** — both ship. The drop-all is the maximally aggressive variant (All only); the per-clause loosening is the spec-faithful one (Stronger | All). Use the profile to pick.
- **`SpanMemoryMutator` (Slice-zero) + `AsSpanAsMemoryMutator` (invocation-site) + `SpanReadOnlySpanDeclarationMutator` (declaration-site)** — all three coexist. The Slice-zero variant is stryker-netx-specific; the other two correspond directly to spec items.

## Questions or issues

Open a GitHub issue at https://github.com/pgm1980/stryker-netx/issues. Reference the per-sprint lessons docs in [`_docs/`](_docs/) for the rationale behind each architectural decision (ADRs 013–018) and each new operator family.
