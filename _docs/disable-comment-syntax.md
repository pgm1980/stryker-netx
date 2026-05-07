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

> **Note**: passing a class name like `ConfigureAwait` directly to a disable-comment
> produces an ERR-log because Mutator-Class names are not accepted; the parser
> emits a hint in the error message pointing to this doc. To disable
> ConfigureAwait-related mutations, use `// Stryker disable Boolean` (which
> disables ALL mutators of Kind `Boolean`, including `BooleanMutator`,
> `MatchGuardMutator`, `NegateConditionMutator`, and `ConfigureAwaitMutator`).

A finer-grained per-class filter is tracked as a Sprint 161+ UX-improvement
(Issue α in ADR-040).

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

## See also

- [ADR-040 in `architecture_specification.md`](architecture%20spec/architecture_specification.md) — full architecture decision record
- Maxential-Session `sprint-160-adr-040-comment-parser` — 6 thoughts of design rationale
