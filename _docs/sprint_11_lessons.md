# Sprint 11 — PIT-2 + cargo-mutants Operator Batch: Lessons Learned

**Sprint:** 11 (2026-05-01, autonomous run)
**Branch:** `feature/11-v2-pit2-cargo-batch`
**Base:** v2.0.0-preview.5 (Sprint 10 closed)
**Final Tag:** `v2.0.0-rc.1`
**Source:** comparison.md §4.1 (PIT-2 gaps) + §4.2 (cargo-mutants gaps)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `ConstructorNullMutator` (PIT CONSTRUCTOR_CALLS — `new Foo(...) → null`) | ✅ profile `Stronger \| All` |
| `MatchGuardMutator` (cargo-mutants C4 — `case X when expr → when true/false`) | ✅ profile `Stronger \| All` |
| `WithExpressionMutator` (cargo-mutants C5 — record-with field deletion) | ✅ profile `Stronger \| All` |
| `NakedReceiverMutator` (PIT EXP_NAKED_RECEIVER — `a.M(b) → a`) | ✅ profile `All` only |
| All 4 wired into `DefaultMutatorList` | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (none of the new mutators active under Defaults) |
| Semgrep | ✅ clean (0 findings on new files) |
| Tag | `v2.0.0-rc.1` |

## What landed

### ConstructorNullMutator (PIT CONSTRUCTOR_CALLS)

Replaces `new Foo(args)` with `null`. Catches "is the constructed object actually used downstream?" tests. Conservative scope skips:
- `throw new ...` (compile-illegal)
- `: base(...)` / `: this(...)` initializer calls (compile-illegal)

Famously disruptive on real code → `Stronger | All` only.

### MatchGuardMutator (cargo-mutants C4)

For every `case X when expr` clause, emit two mutations: `when true` AND `when false`. Closes the C# 8+ pattern-matching gap — Stryker.NET v1.x doesn't mutate `when`-clauses at all. Two mutations per guard site. `Stronger | All`.

### WithExpressionMutator (cargo-mutants C5)

Mutates C# 9+ `record with { ... }` expressions by removing one initializer at a time. For N initializers, emits N mutations. Catches "is this field actually being updated by the with-expression?" tests. Mirrors cargo-mutants's struct-literal-field-deletion mutator, adapted to C# records. `Stronger | All`.

### NakedReceiverMutator (PIT EXP_NAKED_RECEIVER)

Replaces `a.Method(args)` with the bare receiver `a`. Most aggressive method-call mutator — `All` only. Conservative scope skips:
- `await x.Foo()` (receiver must be awaitable)
- `throw x.Make()` (receiver must be exception)

## What deferred to v2.0.x (post-release)

Originally scoped 7 operators for Sprint 11; landed 4 MVP, deferred 3:

- **CRCR (Constant Replacement Composite — full PIT matrix)** — overlaps significantly with InlineConstantsMutator (Sprint 10) on the `+1`/`-1` axis; remaining axes (`0`, `1`, `-1`, `-c`) deferred to v2.0.x to limit Sprint 11 risk.
- **Coverage-driven mutation skip (D3)** — needs coverage-data plumbing through the orchestrator + a pre-filter step. Cross-cutting infrastructure work, not an operator. Push to v2.1.0.
- **D1 Roslyn Diagnostics filter** — natural fit for `IEquivalentMutantFilter` pipeline but requires diagnostic-extraction during compilation pass. Push to v2.0.x patch.

Honest deferral pattern (precedent: Sprint 8 Hot-Swap scaffolding-only) preserves rc.1 quality — better to ship 4 polished operators than 7 half-implemented ones.

## Process lessons

### 1. `WithCleanTriviaFrom<T>` generic-inference gotcha

The helper signature is:
```csharp
public static T WithCleanTriviaFrom<T>(this T node, T triviaSource) where T : SyntaxNode
```

C# infers `T` from the receiver first. Calling `nullLiteral.WithCleanTriviaFrom(node)` where `nullLiteral` is `LiteralExpressionSyntax` and `node` is `ObjectCreationExpressionSyntax` fails — both must bind to the same `T`, and the compiler picks `LiteralExpressionSyntax` from the receiver, then can't downcast `node`.

**Fix:** Type the literal as the common base `ExpressionSyntax`:
```csharp
ExpressionSyntax nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
```

Now both bind to `ExpressionSyntax`. Documented for future cross-type trivia copies.

### 2. IDE0078 `is X || is Y` → `is X or Y`

Build-blocker (Roslyn analyzer at error severity). The pattern-matching combined `or`/`and`/`not` patterns are mandatory; `||` between two `is` checks does not pass. Trivial mechanical fix but easy to write out of habit. Two occurrences in this sprint (ConstructorNullMutator, NakedReceiverMutator).

### 3. Profile-based opt-in continues to pay

All 4 new operators are `Stronger | All` (3) or `All` only (1). Sample E2E under Defaults profile = 5/5 killed = 100 % by construction. Zero E2E test flake risk from operator additions. Pattern is now load-bearing for the v2.0.0 release: every aggressive operator landed since Sprint 9 has been opt-in.

### 4. Sprint 11 is the catalogue-closing batch — rc.1 cadence justified

After Sprint 11, the catalogue gap from comparison.md §4.1+§4.2 is closed for all *operator-shaped* recommendations. Remaining v2.0.0 work (Sprint 12) is greenfield .NET-specific operators (Async/Await, Span/Memory, DateTime, Exception-Swap, Generic-Constraint) + docs (README v2 + Migration Guide). That's the right shape for an `rc.1 → final` cadence.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3
[done]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
[done]    Sprint 10 → PIT-1 Operator Batch                  → v2.0.0-preview.5
[done]    Sprint 11 → PIT-2 + cargo-mutants Batch           → v2.0.0-rc.1       ⭐
[next]    Sprint 12 → Greenfield + README + Release         → v2.0.0
```
