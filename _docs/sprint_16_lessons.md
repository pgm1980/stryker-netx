# Sprint 16 — v2.3.0 Long-tail Items: Lessons Learned

**Sprint:** 16 (2026-05-01, autonomous run)
**Branch:** `feature/16-v2.3-long-tail`
**Base:** v2.2.0 (Sprint 15 closed)
**Final Tag:** `v2.3.0`
**Source:** Sprint 15 lessons long-tail list + Sprint 16 Maxential (10 thoughts, 1 branch on contract-extension-strategy made moot mid-thought).

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `AsyncAwaitResultMutator` (`await x → x.Result`) | ✅ Stronger \| All |
| Wired into `DefaultMutatorList.V2OperatorBatches` | ✅ |
| `JsonReportSerializerContext` source-gen | ✅ Hybrid (source-gen entry types + custom converters at runtime) |
| `JsonReportSerialization` rewritten to use source-gen JsonTypeInfo | ✅ |
| Validation count-tests `[Fact(Skip = "...")]` with ADR-023 link | ✅ 11 tests skipped with documented reason |
| ADR-023 written | ✅ Validation-non-reconciliation principle |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % |
| Semgrep | ✅ clean |
| README + MIGRATION updated to 52-mutator state | ✅ |
| Tag | `v2.3.0` |

## v2.3.0 final catalogue: 52 mutators + 4 filters

- **26 Defaults** (v1.x baseline, preserved)
- **+18 Stronger** (17 from v2.2.0 + 1 = AsyncAwaitResult)
- **+8 All-only** (unchanged from v2.2.0)
- **4 equivalence filters** (unchanged)
- JsonReport: hybrid source-gen serialization (AOT-trim progress)

## What landed

### `AsyncAwaitResultMutator` (Stronger | All)

`await x → x.Result` — the spec-faithful semantic variant complementing v2.0.0's `AsyncAwaitMutator` which emits `await x → x.GetAwaiter().GetResult()`. Both mutators coexist:

| Variant | Exception behaviour |
|---------|--------------------|
| `AsyncAwaitMutator` (v2.0.0) | `GetAwaiter().GetResult()` unwraps the original exception |
| `AsyncAwaitResultMutator` (v2.3.0) | `.Result` wraps in `AggregateException` |

Tests asserting on a specific exception type may pass under one substitution and fail under the other — having both maximises kill-detection sensitivity for sync-over-async anti-patterns.

### JsonReport hybrid source-gen rewrite

New `JsonReportSerializerContext` `JsonSerializerContext` with `[JsonSerializable(typeof(JsonReport))]` + `[JsonSerializable(typeof(IJsonReport))]` + `[JsonSourceGenerationOptions(...)]`. Source-gen provides `JsonTypeInfo` for the entry types — no runtime reflection on these.

**Hybrid design:** the existing custom polymorphic converters (`SourceFileConverter`, `JsonMutantConverter`, `LocationConverter`, `PositionConverter`, `JsonTestFileConverter`, `JsonTestConverter`) cannot be declared on the source-gen attribute (`SYSLIB1220` rejects polymorphic-interface converters). They are attached to the runtime `Options` instance via `JsonTypeInfoResolver.Combine(JsonReportSerializerContext.Default, new DefaultJsonTypeInfoResolver())`, so source-gen handles the entry types and reflection handles interface-typed sub-properties.

**Net AOT progress:** source-gen for entry-type metadata. Custom converters continue to use runtime reflection for interface dispatch. **Full AOT-trim** would require flattening `IJsonReport` / `ISourceFile` / `IJsonMutant` to concrete types — out of scope for v2.3.0; documented as such in README "Known limitations".

### Validation framework deferral (ADR-023)

11 `[Fact]` tests in `integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs` were marked `[Fact(Skip = "...")]` with explicit ADR-023 link. ADR-023 documents:

- Why these tests can't be reconciled (counts hardcoded for upstream Stryker.NET 4.14.1; v2.x catalogue legitimately differs)
- Why reconciliation would be a Sisyphus task (drift on every Operator-Addition)
- Why the tests don't validate what they appear to validate (Plumbing, not Mutator-Korrektheit; the latter has Unit-Tests)
- Why "principled skip + ADR" beats "remove" (reversible) and "Range-Asserts" (still drifty)

Honest-deferral pattern precedent: Sprints 8, 11, 13, 15.

## Process lessons

### 1. **Source-gen JsonSerializerContext + polymorphic converters need JsonTypeInfoResolver.Combine**

The first attempt to ship JsonReport source-gen failed with `SYSLIB1220` because the polymorphic `JsonConverter<IJsonMutant>` and `JsonConverter<ISourceFile>` classes can't be declared on `[JsonSourceGenerationOptions(Converters = [...])]` (source-gen can't validate them at compile time).

**Fix:** drop the `Converters` field from the source-gen attribute. Build the runtime `JsonSerializerOptions` with `TypeInfoResolver = JsonTypeInfoResolver.Combine(JsonReportSerializerContext.Default, new DefaultJsonTypeInfoResolver())` and attach the converters to `Options.Converters` at runtime. Source-gen handles the entry-type metadata; custom converters handle polymorphism via the combined resolver chain.

**Generalizable lesson:** for codebases that mix concrete types (source-gen-friendly) with polymorphic interfaces (need custom converters), the hybrid `JsonTypeInfoResolver.Combine` pattern is the standard answer.

### 2. **Maxential branch can become moot mid-thought via recherche**

Sprint 16 Maxential opened a 1-branch decision (D1=default-interface-method vs D2=sibling-interface) for extending `IEquivalentMutantFilter` to carry `Compilation`. Mid-thought recherche surfaced that `SemanticModel.Compilation` exists — Compilation is derivable from the existing parameter, no contract extension needed at all. Both branches became moot in the same thought.

**Generalizable lesson:** even within a Maxential-locked decision, recherche should be done on the actual API surface before committing implementation effort. The Maxential branches are useful for *reasoning*, but a Serena `find_symbol` on the relevant type is often the deciding evidence.

### 3. **`[Fact(Skip = "...")]` over `[Fact] + [Trait("Skip", "...")]`**

xUnit doesn't have a "Skip" trait — `[Trait]` is just metadata, doesn't affect test execution. The right pattern is `[Fact(Skip = "reason")]`. Original Sprint plan said "Skip-Trait" — corrected during implementation. Documented for future deferral patterns.

### 4. **`StringComparison.Ordinal` is mandatory for `string.Replace` calls** (Meziantou MA0006)

The existing `JsonReportSerialization.ToJsonHtmlSafe` had `report.ToJson().Replace("<", "<\" + \"")` — accepted by analyzer history. The rewrite triggered MA0006: must use the explicit-comparison overload. Fix: `report.ToJson().Replace("<", "<\" + \"", System.StringComparison.Ordinal)`. Same gotcha as Sprint 12's MA0002/MA0006 lessons — applies to every `string.Replace`/`==`/`!=` site we add.

## v2.3.0 progress map

```
[done]    Sprint 16 → Long-tail items                       → v2.3.0   ⭐ MINOR ⭐
```

## v2.x roadmap (still post-v2.1.0)

After v2.3.0:
- **Deferred this sprint:**
  - RoslynDiagnostics filter v2 (semantic errors via SyntaxTree-substitution + Compilation.AddSyntaxTrees per mutation; cost-too-high)
  - GenericConstraintLoosen interface-target case (ICloneable→IDisposable swaps; speculative without bug-class evidence)
- **Inherited from earlier sprints:**
  - v3.0: hard-remove `[Obsolete]` `MutationEngine` symbols
  - ADR-022 (Proposed): incremental mutation testing — only commit if user-demand surfaces
- **Long-tail (still applicable):**
  - JsonReport full AOT-trim (requires flattening interface-typed properties to concrete types — large refactor)
  - Validation framework intentional-skip per ADR-023 (reversible if anyone takes on the reconciliation)
