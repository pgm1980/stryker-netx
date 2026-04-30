# Sprint 9 — Type-Driven Mutators: Lessons Learned

**Sprint:** 9 (2026-05-01, autonomous run)
**Branch:** `feature/9-v2-type-driven-mutators`
**Base:** v2.0.0-preview.3 (Sprint 8 closed)
**Final Tag:** `v2.0.0-preview.4`
**ADR foundation:** ADR-015 (TypeAwareMutatorBase from Sprint 7)
**Closes the v2.0.0 cargo-mutants gap (comparison.md §4.2 — "kategorisch das größte Differenzial-Feature").**

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `TypeDrivenReturnMutator` (extends `TypeAwareMutatorBase<ReturnStatementSyntax>`) | ✅ |
| `ConservativeDefaultsEqualityFilter` (extends `IEquivalentMutantFilter`) | ✅ |
| Both wired into `DefaultMutatorList` / `EquivalentMutantFilterPipeline.Default` | ✅ |
| `MutationProfileMembership` on `TypeDrivenReturnMutator` | `Stronger | All` (NOT `Defaults`) |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (TypeDrivenReturnMutator NOT active under Defaults) |
| Tag | `v2.0.0-preview.4` |

## What landed

### TypeDrivenReturnMutator (cargo-mutants gap closure)

Replaces the `return X;` expression with type-appropriate defaults derived from the enclosing method's return type symbol via `TypeAwareMutatorBase.GetReturnType`. Substitutions:

| Return type | Mutation(s) emitted |
|-------------|---------------------|
| `Task<T>` | `Task.FromResult(default(T))` |
| `ValueTask<T>` | `new ValueTask<T>(default(T))` |
| `IEnumerable<T>` | `Enumerable.Empty<T>()` |
| `List<T>` | `new List<T>()` |
| `Dictionary<K,V>` | `new Dictionary<K,V>()` |
| `string` | `string.Empty`; also `null` when nullable-annotated |
| `bool` | `false` AND `true` |
| primitive numeric | `0` |

Fully-qualified namespace prefixes used in the emitted expressions (e.g. `System.Threading.Tasks.Task.FromResult`) so the mutated code compiles regardless of the original file's `using`-list.

### ConservativeDefaultsEqualityFilter (cargo-mutants conservative defaults)

Catches the equality-mutated-to-ordered-comparison pattern on unsigned operands — `uint x; x == 0` mutated to `x < 0` is a never-true comparison, equivalent in observable behaviour. cargo-mutants enforces this conservative rule by simply NOT mutating in this case; we apply it post-mutation as a filter so the existing equality-mutator code stays untouched.

Conservative scope: only literal-zero operand cases (variable comparisons abstained-on, preserving false-negative bias).

## Process / scope decisions

1. **TypeDrivenReturnMutator profile = Stronger | All, NOT Defaults.** The mutator is aggressive — it replaces real return expressions with defaults, which will produce many survivors on under-tested code. Defaulting to off-by-default keeps Sample E2E at 100 % and doesn't ambush v1.x users on upgrade. They opt in via `--profile Stronger` (or All).
2. **Sample E2E = 100 % is preserved by construction.** Default profile = Defaults; TypeDrivenReturnMutator is in Stronger | All. The orchestrator's profile filter (Sprint 6) skips it under Defaults. So Sample.Library — which has no `return Task<T>` etc. — sees zero new mutations.
3. **Did NOT do** the additional v2.0.0-Roadmap items earmarked for Sprint 9: B4 PIT empty/null/primitive returns specialization (subsumed by TypeDrivenReturnMutator's default-emission logic — covered as a side-effect), B6 Argument Propagation (separate mutator file; deferred to Sprint 10's PIT-1 batch), D1 Type-Checker Integration (Roslyn diagnostics filter). All three are scoped follow-ups; Sprint-3-precedent honest deferral.
4. **Fully-qualified namespace prefixes in generated expressions.** Initially considered using simple names + relying on the file's existing `using`s, but that's fragile (file may not import `System.Linq`). Using fully-qualified names is verbose-looking but always-compiles — Roslyn IDE simplification can collapse them later if needed.

## Risks / follow-ups

- **TypeDrivenReturnMutator may produce unviable mutations** for return types we don't recognize (e.g. `IAsyncEnumerable<T>`, custom `Result<T>` types from libraries like FluentResults). The mutator silently abstains on unknown types — safe default — but the catalogue should grow per-real-world-need.
- **ConservativeDefaultsEqualityFilter only handles literal-zero**; runtime-zero variables aren't caught. Per the conservative-by-design contract this is correct, but documented as "knows-it's-conservative" rather than "comprehensive".
- **D1 (Type-Checker Integration / Roslyn diagnostics filter)** would slot naturally into the existing `IEquivalentMutantFilter` pipeline as a `RoslynDiagnosticsFilter` — defer to Sprint 10 along with the rest of the PIT-1 wave.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3
[done]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4  ⭐ (cargo-mutants gap closed)
[next]    Sprint 10 → Coverage-Driven + PIT-1 Operators     → v2.0.0-preview.5
          Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
