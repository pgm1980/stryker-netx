# Sprint 14 — v2.1.0 Filter Pipeline + Operator Completion: Lessons Learned

**Sprint:** 14 (2026-05-01, autonomous run)
**Branch:** `feature/14-v2.1-filter-pipeline`
**Base:** v2.0.1 (Sprint 13 closed)
**Final Tag:** `v2.1.0`
**Source:** v2.1 roadmap from Sprint 13 lessons, refined via Sprint-14 Maxential session (15 thoughts, 1 branch on HotSwap scope-decision).

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `ConstantReplacementMutator` (PIT CRCR full matrix) | ✅ Stronger \| All |
| `GenericConstraintLoosenMutator` (per-clause loosening) | ✅ Stronger \| All |
| `SpanReadOnlySpanDeclarationMutator` (declaration-site swap) | ✅ All only |
| `RoslynDiagnosticsEquivalenceFilter` (mutmut-style pre-filter) | ✅ pipeline-active |
| All 3 mutators wired into `DefaultMutatorList.V2OperatorBatches` | ✅ |
| Filter wired into `EquivalentMutantFilterPipeline.Default` | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (after fixing the filter regression — see lesson 1) |
| Semgrep | ✅ clean (0 findings on 4 new files) |
| README + MIGRATION updated to 51-mutator state | ✅ |
| ADR-019 (v2.2 HotSwap focused-release deferral decision) | ✅ |
| Tag | `v2.1.0` |

## Final v2.1.0 catalogue: 51 mutators + 4 filters

- **26 Defaults** (v1.x baseline, preserved bit-for-bit)
- **+17 Stronger additions** (15 from v2.0.x + 2 from v2.1.0 = ConstantReplacement, GenericConstraintLoosen)
- **+8 All-only additions** (7 from v2.0.x + 1 from v2.1.0 = SpanReadOnlySpanDeclaration)
- **4 equivalence filters**: IdentityArithmetic + IdempotentBoolean + ConservativeDefaultsEquality + RoslynDiagnostics (v2.1)

## What landed

### `ConstantReplacementMutator` (PIT CRCR)

PIT-spec `c → 0, 1, -1, c+1, c-1, -c`. v2.0.0's `InlineConstantsMutator` already covers `c+1` / `c-1`; this mutator adds the remaining four axes (`→ 0`, `→ 1`, `→ -1`, `→ -c`). For int / long, the per-axis no-op skip avoids emitting `0 → 0` etc. For float / double, all four axes always emit (floating-point equality skips would have hit Sonar S1244 — see lesson 2). Output: 1–4 mutations per literal.

### `GenericConstraintLoosenMutator` (v2.1, complement to v2.0.0 drop-all)

Per-clause loosening: `where T : class → where T : new()` / `→ struct`; `where T : new() → where T : class`; `where T : SomeInterface/SomeBaseClass → where T : class` (weakest-of-references). Targets `TypeParameterConstraintClauseSyntax`. Compile-failure-may-occur (e.g. body relies on `new T()` requires `: new()`) — runner classifies non-compiling mutants as killed. Profile: Stronger | All — less aggressive than v2.0.0's drop-all variant.

### `SpanReadOnlySpanDeclarationMutator` (v2.1, complement to v2.0.1 invocation-site)

Declaration-site `Span ↔ ReadOnlySpan` and `Memory ↔ ReadOnlyMemory` swap. Targets `GenericNameSyntax` in TypeSyntax positions (parameter type, variable declaration, return type, etc.) — explicitly excluded from value contexts where `AsSpanAsMemoryMutator` (v2.0.1) handles the invocation form. Compile-failure rate is high: `Span<T>` is `ref struct`, `Memory<T>` is heap-allocatable; `ReadOnlySpan<T>` doesn't allow assignment-into. Runner classifies non-compiling mutants as killed. Profile: All only.

### `RoslynDiagnosticsEquivalenceFilter` (v2.1, mutmut mypy-equivalent)

New `IEquivalentMutantFilter` implementation. Inspects the replacement node's parser-level diagnostics (already attached during Roslyn construction — no re-parsing needed) and short-circuits as "equivalent" (pipeline sense: don't schedule a test run) when severity-Error diagnostics are present. Profile: always-on (filters work across all profiles, like the existing IdentityArithmeticFilter etc.).

## Process lessons

### 1. **Re-parsing standalone expression / statement snippets misclassifies them as invalid syntax.**

The first version of `RoslynDiagnosticsEquivalenceFilter` did:
```csharp
var snippet = mutation.ReplacementNode.ToFullString();
var tree = CSharpSyntaxTree.ParseText(snippet);
return tree.GetDiagnostics().Any(d => d.Severity == Error);
```

This **filtered out 100% of mutations** in the E2E. Root cause: `CSharpSyntaxTree.ParseText` parses input as a *compilation unit* (top-level program). A bare expression like `a - b` (the typical replacement node from `BinaryExpressionMutator`) is invalid as a top-level statement, so the parser flags it as error.

**Fix:** call `mutation.ReplacementNode.GetDiagnostics()` directly on the existing node — the diagnostics are already attached during the mutator's construction step. No re-parsing needed. Detected during the regression-E2E pass; without the E2E gate this would have shipped silently and broken every customer.

**Generalizable lesson:** any future filter that needs to validate a replacement should use the node's already-attached metadata, NEVER re-parse it as a standalone syntax tree.

### 2. **Sonar S1244 forbids float-equality even when semantically correct.**

`ConstantReplacementMutator` initially had `if (v != 0.0) yield return ...` skip-optimizations for the no-op cases (e.g. don't emit `0 → 0`). Sonar S1244 ("don't check floating point inequality with exact values, use a range") rejected this at error severity. The semantically-correct interpretation here would be acceptable but Sonar can't tell.

**Fix:** drop the float-equality skip. Worst case is one no-op mutation per literal in the rare `0.0 → 0.0` case — the runner flags it as an unchanged-behavior mutant. Cleaner than `#pragma warning disable S1244` with justification, and the volume cost is negligible.

### 3. **Coverage-driven mutation skip was already implemented in v1.x — the roadmap entry was a doc gap.**

Sprint 13's lessons doc listed "coverage-driven mutation skip" as a v2.1 roadmap item under mutmut. Recherche via Serena/Grep this sprint surfaced `OptimizationModes.SkipUncoveredMutants` and `CoverageBasedTest` already implementing exactly this in v1.x via the `CoverageAnalyser`. The `--coverage-analysis` flag (default `perTest`) controls it; mutants on uncovered lines are flagged `NoCoverage` and never test-run.

**Lesson:** before scoping new infrastructure work, grep the existing codebase for the feature. The roadmap items in lessons docs are *category-level* notes (PIT/cargo-mutants/mutmut-side reference), not gap claims — they need recherche before they become sprint work.

### 4. **HotSwap-impl is a 1–3-month engineering project, not a sprint deliverable.**

ADR-019 (this sprint) documents the explicit decision to defer HotSwap engine implementation to v2.2.0 as its own focused release rather than cram it into v2.1.0. Maxential branch B1 (substantial framework expansion) vs B2 (defer cleanly) — chose B2 with three arguments: (1) honest-deferral pattern precedent (Sprint 8, Sprint 11, Sprint 13), (2) YAGNI / no dead framework code, (3) "Endstand" means ship-working-things, not touch-every-roadmap-item. Recorded as ADR-019 so future readers see the explicit rationale.

## v2.1.0 progress map

```
[done]    Sprint 14 → Filter pipeline + operator completion → v2.1.0   ⭐ MINOR ⭐
[next]    Sprint 15 → HotSwap engine focused release         → v2.2.0
```

## v2.x roadmap (post-v2.1.0)

- v2.2.0: HotSwap engine implementation per ADR-019 (own focused release)
- v2.x: documented semantic deviations (AsyncAwait `.Result` variant, GenericConstraint loosening for interface types, etc.)
- v2.x: validation framework count-reconciliation
- v2.x: JsonReport AOT-trim-friendly source-gen rewrite
