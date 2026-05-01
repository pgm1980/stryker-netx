# Sprint 17 — v2.4.0 Final Long-tail Rest: Lessons Learned

**Sprint:** 17 (2026-05-01, autonomous run)
**Branch:** `feature/17-v2.4-final-long-tail`
**Base:** v2.3.0 (Sprint 16 closed)
**Final Tag:** `v2.4.0`
**Source:** Sprint 16 deferred-list (3 items) + Sprint 17 Maxential (11 thoughts, 1 branch on jsonreport-aot-scope, E3 chosen).

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `RoslynSemanticDiagnosticsEquivalenceFilter` | ✅ pipeline always-on |
| Wired into `EquivalentMutantFilterPipeline.Default` | ✅ |
| `GenericConstraintLoosenMutator` BCL-pair extension (7 BCL pairs) | ✅ Stronger \| All |
| ADR-024 (JsonReport full AOT-trim → v3.0 deferral) | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % |
| Semgrep | ✅ clean |
| README + MIGRATION updated to 5-filter state + v3.0 roadmap | ✅ |
| Tag | `v2.4.0` |

## v2.4.0 final catalogue: 52 mutators + 5 filters

- **26 Defaults** (v1.x baseline, preserved bit-for-bit)
- **+18 Stronger** (unchanged from v2.3.0; `GenericConstraintLoosenMutator` extended internally with BCL-pair variants, no new mutator class)
- **+8 All-only** (unchanged)
- **5 equivalence filters**: IdentityArithmetic + IdempotentBoolean + ConservativeDefaultsEquality + RoslynDiagnostics (parser-only, v2.1) + **RoslynSemanticDiagnostics (v2.4)**

## What landed

### Ship 1 — `RoslynSemanticDiagnosticsEquivalenceFilter` (semantic-error pre-filter)

The Sprint 16 deferral reason for "RoslynDiagnostics filter v2 (semantic errors)" was: "needs full SyntaxTree substitution + `compilation.AddSyntaxTrees(new).GetDiagnostics()` per mutation = per-mutation Compilation cost. Cost-to-value too high."

Sprint 17 re-evaluated and found a smarter approach: Roslyn's **speculative-binding API** (`SemanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression)`) was designed exactly for this use case (IDE refactoring previews). It checks "would this expression bind in the existing semantic context?" without rebuilding the Compilation — O(1) per mutation in the existing semantic context, not O(parse + bind) per mutation.

The filter implementation:
```csharp
var info = semanticModel.GetSpeculativeSymbolInfo(
    mutation.OriginalNode.SpanStart,
    replacementExpression,
    SpeculativeBindingOption.BindAsExpression);
return info.Symbol is null && info.CandidateReason != CandidateReason.None;
```

Conservative scope: only ExpressionSyntax replacements; statement-level needs `TryGetSpeculativeSemanticModel` (bulkier, deferred). Pipeline always-on.

### Ship 2 — `GenericConstraintLoosenMutator` BCL-pair extension

Added a hardcoded BCL-interface-pair table:
```
ICloneable      ↔ IDisposable
IComparable     ↔ IEquatable
IEnumerable     ↔ ICollection
ICollection     ↔ IList
```

When a generic constraint targets one of these interfaces, the mutator emits the paired-interface alternative alongside the v2.1.0 class-constraint replacement. Catches "is the chosen interface's API actually exercised, or could a different interface have served?" tests.

Non-BCL interfaces fall back to the v2.1.0 class-constraint behavior — speculative interface-to-interface swaps for user-defined types would be noise without a clear bug-class motivation.

### Defer — JsonReport full AOT-trim (ADR-024 → v3.0)

3-way Maxential branch evaluation:
- **E1 — Full breaking-change refactor in v2.4.0**: rejected (violates v2.0.0's no-breaking-changes-for-default-profile principle).
- **E2 — Parallel concrete-type variant + `[Obsolete]` interfaces**: rejected (doubles maintenance with low marginal value; not symmetric with the `[Obsolete]`-shim pattern that fits enum/property surface but not serialization-flattening).
- **E3 — Defer to v3.0 with ADR-024**: ✅ chosen. Aligns with established v2.x deferral discipline (Sprint 15 ADR-021, Sprint 16 ADR-023).

ADR-024 documents:
- Recherche cost (7 interfaces, 34 referencing files)
- Why parallel-shim doesn't fit
- v3.0 batching with the `[Obsolete]` `MutationEngine` hard-removal
- Implementation outline for the v3.0 sprint

## Process lessons

### 1. **A re-evaluated "deferred" can sometimes ship via smarter API choice**

The Sprint 16 deferral of RoslynDiagnostics filter v2 was based on the cost analysis of one specific approach (SyntaxTree substitution + AddSyntaxTrees). Sprint 17 recherche on Roslyn's API surface surfaced `GetSpeculativeSymbolInfo` — designed exactly for this O(1) use case. Lesson: when re-visiting a deferred item, recherche the API surface first; the cost model that drove the original deferral may be invalidated by a better tool.

### 2. **BCL-pair tables are a low-cost-low-noise extension pattern**

`GenericConstraintLoosenMutator` originally only handled the class-constraint replacement for `TypeConstraintSyntax`. The BCL-pair extension adds 4-7 well-defined interface swaps via a hardcoded table — small surface, well-bounded noise (only well-known BCL pairs), conservative coverage (user-defined interfaces fall back to the existing behavior). Pattern documented for future "extend a mutator with a known-shape table" work.

### 3. **ADR-024 establishes the v2.x → v3.0 batching discipline**

Three ADRs now defer items to v3.0: ADR-021 (HotSwap-Engine `[Obsolete]` shims → hard-remove in v3.0), ADR-024 (JsonReport AOT-trim full refactor → v3.0), and the Sprint 13 documented semantic deviations (any breaking-spec-faithful change → v3.0). Pattern: v3.0 will be the focused breaking-change major-version release that batches all of these. Documented for future sprints to add to the v3.0 batch.

### 4. **Speculative-binding has a `CandidateReason != None` discriminator**

`GetSpeculativeSymbolInfo` returns a `SymbolInfo` struct; the right "binding-failed" check is `info.Symbol is null && info.CandidateReason != CandidateReason.None`. `Symbol == null && CandidateReason == None` happens for legitimately-bindings-to-nothing cases (e.g. literal expressions like `0` that don't resolve to a named symbol). Documented for future speculative-binding filter work.

## v2.4.0 progress map

```
[done]    Sprint 17 → Final long-tail rest                    → v2.4.0   ⭐ MINOR ⭐
```

## v3.0 batched items (when scheduled)

- Hard-remove `[Obsolete]` `MutationEngine` symbols (ADR-021)
- JsonReport full AOT-trim flattening (ADR-024)
- Documented semantic deviations (Sprint 13 — `AsyncAwaitMutator` → `.Result`-only, etc., if user-demand)
- Optional: ADR-022 (Proposed) incremental mutation testing — separate decision

## v2.x roadmap (truly closed for now)

- **Access-Modifier-Mutation** — controversial, kept off the roadmap unless requested
- All other long-tail items shipped or deferred to v3.0
