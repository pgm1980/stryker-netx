# Stryker disable-comment syntax — stryker-netx reference

> **Sprint 160 / ADR-040 (v3.2.12+)** — The CommentParser was extended to accept
> the Stryker.JS-style `next-line` scope qualifier and to skip unrecognised
> mutator labels instead of silently falling back to `Mutator.Statement`. This
> page is the canonical reference for the supported syntax + Mutator-Kind list.

## Syntax

```
// Stryker <mode> [<scope>] [<mutator-list>] [: <comment>]
```

| Token | Values | Default | Notes |
|---|---|---|---|
| `<mode>` | `disable` \| `restore` | required | `disable` filters mutators out; `restore` re-enables them. |
| `<scope>` | `next-line` \| `once` \| (omitted) | block-disable until next `restore` | See "Scope semantik" below. |
| `<mutator-list>` | `all` or comma-separated Mutator-Kind names | required | Empty list is a silent no-op (no filter applied). |
| `<comment>` | free-form text after `:` | "Ignored via code comment." | Shown in the JSON/HTML report next to the ignored mutant. |

### Examples

```csharp
// Stryker disable all : equivalent — null-guard is unreachable in tests
public string GetName(User u) => u?.Name ?? throw new ArgumentNullException();

// Stryker disable Boolean
if (configRetries > 0) { /* … */ }
// Stryker restore Boolean

// Stryker disable next-line all : equivalent — ConfigureAwait flip is no-op under xUnit
await SomeOp().ConfigureAwait(false);

// Stryker disable once Linq, NullCoalescing : justified for migration phase
foreach (var x in collection.Where(p => p.IsActive) ?? Empty) { /* … */ }

/* Stryker disable Block : multi-line comment also works for Stryker comments */
```

## Scope semantik (stryker-netx)

| Scope | Stryker.JS semantics | stryker-netx implementation (Sprint 160) |
|---|---|---|
| (omitted, block) | Filter applies until matching `restore` | Same as Stryker.JS |
| `once` | Filter applies to the next single mutation | Same as Stryker.JS |
| `next-line` | Filter applies to all mutations on the next code line | **Currently implemented as alias for `once`** (= single-mutation scope). The `// Stryker disable next-line all` syntax is parsed without error, but only the FIRST mutation on the next line is disabled. For full-line coverage, use the block form: `// Stryker disable all` + `// Stryker restore all` around the line. Full Stryker.JS-line-scope semantik is tracked as Sprint 161+ enhancement. |

This pragmatic-implementation choice is documented in ADR-040 and is honest-deferred
because full line-scope would require a Line-Tracking refactor in `MutationContext`.

## Mutator-Kind list (legal `<mutator-list>` values)

The 20 valid Mutator-Kind names that can appear in `<mutator-list>`:

`Arithmetic`, `Assignment`, `Bitwise`, `Block`, `Boolean`, `Checked`, `CollectionExpression`,
`Conditional`, `Equality`, `Initializer`, `Linq`, `Logical`, `Math`, `NullCoalescing`,
`Regex`, `Statement`, `String`, `StringMethod`, `Unary`, `Update`.

Plus the wildcard `all` (disables every Kind).

Comparison is case-insensitive (`BOOLEAN` and `boolean` both work).

## Mutator-Class to Mutator-Kind mapping

stryker-netx has 49+ mutator classes; the disable-comment filter is **Kind-based**,
not class-based. To disable a specific mutator class, use its underlying Kind
(`// Stryker disable Boolean` disables every mutator with `Type = Mutator.Boolean`,
not just `BooleanMutator`).

| Mutator-Class | Mutator-Kind to use |
|---|---|
| `AodMutator` | `Arithmetic` |
| `ArgumentPropagationMutator` | `Statement` |
| `ArrayCreationMutator` | `Initializer` |
| `AsSpanAsMemoryMutator` | `Statement` |
| `AssignmentExpressionMutator` | `Assignment` |
| `AsyncAwaitMutator` | `Statement` |
| `AsyncAwaitResultMutator` | `Statement` |
| `BinaryExpressionMutator` | `Logical` |
| `BinaryPatternMutator` | `Logical` |
| `BlockMutator` | `Block` |
| `BooleanMutator` | `Boolean` |
| `CheckedMutator` | `Checked` |
| `ConditionalExpressionMutator` | `Conditional` |
| **`ConfigureAwaitMutator`** | **`Boolean`** |
| `ConstantReplacementMutator` | `Linq` |
| `ConstructorNullMutator` | `Initializer` |
| `DateTimeAddSignMutator` | `Statement` |
| `DateTimeMutator` | `Statement` |
| `ExceptionSwapMutator` | `Initializer` |
| `GenericConstraintLoosenMutator` | `Statement` |
| `GenericConstraintMutator` | `Statement` |
| `InitializerMutator` | `Initializer` |
| `InlineConstantsMutator` | `Linq` |
| `InterpolatedStringMutator` | `String` |
| `IsPatternExpressionMutator` | `Equality` |
| `LinqMutator` | `Linq` |
| `MatchGuardMutator` | `Boolean` |
| `MathMutator` | `Math` |
| `MemberVariableMutator` | `Initializer` |
| `MethodBodyReplacementMutator` | `Statement` |
| `NakedReceiverMutator` | `Statement` |
| `NegateConditionMutator` | `Boolean` |
| `NullCoalescingExpressionMutator` | `NullCoalescing` |
| `ObjectCreationMutator` | `Initializer` |
| `PostfixUnaryMutator` | `Update` |
| `RegexMutator` | `Regex` |
| `RelationalPatternMutator` | `Equality` |
| `RorMatrixMutator` | `Equality` |
| `SpanMemoryMutator` | `Statement` |
| `SpanReadOnlySpanDeclarationMutator` | `Statement` |
| `StatementMutator` | `Statement` |
| `StringEmptyMutator` | `String` |
| `StringMethodMutator` | `StringMethod` |
| `StringMethodToConstantMutator` | `StringMethod` |
| `StringMutator` | `String` |
| `SwitchArmDeletionMutator` | `Statement` |
| `TaskWhenAllToWhenAnyMutator` | `Statement` |
| `TypeDrivenReturnMutator` | `Statement` |
| `UoiMutator` | `Update` |
| `WithExpressionMutator` | `Initializer` |

> **Sprint 166 update (v3.2.18, ADR-046 §B)**: the three most-commonly-confused
> mutator class names are now ACCEPTED as user-input aliases and silently
> resolved to the underlying kind. Case-insensitive:
>
> | Alias label | Resolves to | Why |
> |---|---|---|
> | `ConfigureAwait` | `Boolean` | `ConfigureAwaitMutator` swaps the `.ConfigureAwait(false)` literal |
> | `AsyncAwait` | `Boolean` | `AsyncAwaitMutator` rewrites `await x` to `x.GetAwaiter().GetResult()` (Boolean-typed) |
> | `AsyncAwaitResult` | `Boolean` | `AsyncAwaitResultMutator` — v2.3 spec-faithful `.Result` variant |
>
> So `// Stryker disable next-line ConfigureAwait : equivalent` now works as
> expected. Previously (v3.2.13 – v3.2.17) it produced the Sprint-161 hint URL
> error; even earlier (v3.2.12 and below) it silently disabled `Statement`
> mutations as a fallback (Bug C). All OTHER class names still produce the
> hint URL — the alias table is intentionally small (3 entries) to keep the
> user-input surface area predictable. To disable ALL mutators of Kind
> `Boolean` (including those not in the alias table), use
> `// Stryker disable Boolean` as before.

A finer-grained per-class filter (where `ConfigureAwait` disables ONLY
`ConfigureAwaitMutator` mutations and not other Boolean-kind mutations on the
same line) is tracked as a v3.3+ enhancement (Branch C from ADR-046 §B
Maxential).

## Parse-failure behavior (Sprint 160 ADR-040 Bug-C fix)

If the parser encounters an unrecognised mutator label inside the comma-separated
`<mutator-list>`, it now **skips that single label** (with an ERR-log) but
continues to apply the remaining valid labels. Pre-Sprint-160 (v3.2.11 and
earlier), an unrecognised label silently fell back to `Mutator.Statement` and
caused undocumented Statement-mutation suppression. The fix is fully
backwards-compatible for valid disable-comments.

Example:

```csharp
// Stryker disable Boolean, ConfigureAwait : reason
```

| Stryker version | Behaviour |
|---|---|
| ≤ v3.2.11 | `Boolean` disabled + `Statement` silently disabled (Bug C: ConfigureAwait fell back to default `Mutator.Statement`) |
| ≥ v3.2.12 | `Boolean` disabled, `ConfigureAwait` skipped with ERR-log including class-name hint, `Statement` left active |

## Backwards compatibility

All disable-comment forms that were valid before Sprint 160 produce the same
filter behaviour after Sprint 160. ADR-040 is a pure parser-layer fix with no
API breakage.

## Pitfalls & Subtleties (Sprint 161 / ADR-041 — observed on production codebases)

The Aisess Platform Team's Hardening-Sprint-2.5 validation (24 mutation runs over
v3.2.12) surfaced three subtleties that are **arguably correct behaviour** but
catch users off-guard. Documented here for reference.

### `next-line` covers exactly ONE statement

`// Stryker disable next-line all` disables mutations on the **immediately
following single C# statement**, not on the entire next textual line. For
multi-statement sequences, each statement that hosts mutations needs its own
`next-line` directive — or wrap the whole block with `// Stryker disable …` /
`// Stryker restore …`:

```csharp
// ❌ ONLY the first statement (var x = …) is disabled below:
// Stryker disable next-line all : reason
var x = SomeOp();
DoSomethingElse();    // ← still mutated

// ✅ One directive per statement:
// Stryker disable next-line all : reason
var x = SomeOp();
// Stryker disable next-line all : reason
DoSomethingElse();

// ✅ Or use the block form for an entire method:
// Stryker disable all : reason
public IServiceCollection AddHealthChecks(...) { /* … */ }
// Stryker restore all
```

### Multi-line method-chains: `next-line` between chain-links now works (Sprint 165 / ADR-045)

Before v3.2.17, `// Stryker disable next-line` placed **between continuation
lines of a multi-line method-chain expression** (such as `.ConfigureAwait(false)`
on its own line) was silently ignored, because Roslyn attaches the comment
trivia to the chain-link's operator token (`.` / `?.`) rather than to the
parent statement. The verbose wrap-style workaround was the only reliable form.

**v3.2.17 fixes this.** The directive can now be placed directly above the line
containing the mutation token, even mid-chain:

```csharp
// ✅ v3.2.17+: directly above the chain-link works as expected.
var framework = await _repository
    .GetBySlugAsync(slug, cancellationToken)
    // Stryker disable next-line Boolean : equivalent — xUnit no SyncContext
    .ConfigureAwait(false);

// ✅ Same for LINQ chains:
var page = items
    .Where(x => x.IsActive)
    // Stryker disable next-line Linq : equivalent — boundary-test elsewhere
    .Select(x => x.Name);

// ✅ Same for conditional-access (?.) and binary-expression continuations.
```

Covered chain-link types: `MemberAccessExpression` (`.X`),
`ConditionalAccessExpression` (`?.`), `BinaryExpression` (`+`, `-`, `&&`, etc.),
`AssignmentExpression` (`=`, `+=`, etc.), `MemberBindingExpression` (the `.X`
member-binding inside a `?.X` chain), and `InvocationExpression` whose call
target is one of the above. The wrap-style (`// Stryker disable all` /
`// Stryker restore all`) remains available as an alternative when disabling a
multi-statement block is more readable than one directive per chain-link.

### stryker-netx scans ALL files for disable-comments — even outside `--mutate` scope

The parser walks every C# file in the analyzed solution looking for
Stryker-disable directives, **regardless of the `--mutate` filter**. A
malformed disable-comment in any file produces ERR-logs even when that file is
not being mutated.

```bash
# Run targets HealthChecks ONLY:
dotnet stryker-netx --mutate "**/HealthChecks/**"

# But a malformed comment in a non-targeted file still surfaces:
[ERR] foo not recognized as a mutator at 42,8, src/Aisess.Api/Middleware/TenantContextMiddleware.cs.
```

This is by design — the parser cannot know in advance which files will produce
mutation candidates after coverage filtering. **Action**: fix or remove
malformed disable-comments project-wide, not just in `--mutate`-targeted files.

### `--mutation-profile Stronger` auto-sets `--mutation-level Advanced` (ADR-025)

Since v3.1.0, the mutation profile silently raises the mutation level when no
explicit `--mutation-level` is supplied:

| `--mutation-profile` | Auto-applied `--mutation-level` (when not set explicitly) |
|---|---|
| `Defaults` | `Basic` |
| `Stronger` | `Advanced` |
| `All` | `Complete` |

Override only if you want a different combination, e.g.

```bash
dotnet stryker-netx --mutation-profile All --mutation-level Basic
```

for the maximum mutator catalogue but with conservative mutation aggressiveness.

The auto-bump is logged at INFO level on stryker-netx startup with a message
like:

```
[INF] mutation-level auto-set to Advanced based on mutation-profile=Stronger
      (no explicit --mutation-level supplied). Override with --mutation-level
      if needed. (ADR-025)
```

This is **normal informational output**, not an error.

## See also

- [ADR-040 in `architecture_specification.md`](architecture%20spec/architecture_specification.md) — Sprint 160 decision record
- [ADR-041 in `architecture_specification.md`](architecture%20spec/architecture_specification.md) — Sprint 161 decision record (Aisess-Validation followup)
- Maxential-Session `sprint-160-adr-040-comment-parser` — 6 thoughts of design rationale (Sprint 160)
- Maxential-Session `sprint-161-adr-041-aisess-followup` — 4 thoughts of design rationale (Sprint 161)
