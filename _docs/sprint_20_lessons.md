# Sprint 20 — Integration Tests Across 6 Layers: Lessons Learned

**Sprint:** 20 (2026-05-01, autonomous run)
**Branch:** `feature/20-integration-tests`
**Base:** v2.6.0 (Sprint 19 closed)
**Final Tag:** `v2.7.0`
**Type:** Test-only release. Zero production-code change.
**Maxential:** 10 thoughts on architecture (D1-A: same project) + 3 thoughts on per-layer scoping. **ToT:** 7-node evaluation of 6 candidate L1–L6 layer designs.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| L1 Orchestrator + Mutator-Pipeline tests | ✅ 17 tests |
| L2 Profile-Filter + Mutator-Liste tests | ✅ 14 tests |
| L3 EquivalentMutantFilterPipeline + Orchestrator tests | ✅ 12 tests |
| L4 MutantPlacer + Mutation injection tests | ✅ 8 tests |
| L5 Reporter-Pipeline tests | ✅ 12 tests |
| L6 Configuration → Options pipeline tests | ✅ 15 tests |
| **78 new integration tests** in `tests/Stryker.Core.Tests/Integration/` | ✅ all green |
| **386 tests** in Stryker.Core.Tests (308 + 78) | ✅ all green |
| **413 tests solution-wide** (386 + 17 Sample + 10 Architecture) | ✅ |
| Test-suite execution | < 3s wall-clock |
| Sample E2E (default profile) | ✅ 100% mutation score (5/5 mutants killed) |
| Semgrep | ✅ 0 findings on integration sources |
| Tag | `v2.7.0` |

## What landed

### Phase 0 — `IntegrationTestBase` (replaces Sprint 18 stub)

`tests/Stryker.Core.Tests/Integration/IntegrationTestBase.cs` extends the
Sprint 18 `MutatorTestBase` with three orchestrator-level helpers:

- `BuildStrykerOptions(profile, level)` — direct concrete-class init of `StrykerOptions`. Replaces Sprint 18's brittle `IStrykerOptions` stub by sidestepping the reflection surface entirely. Sprint 19 deferred this as P7; Sprint 20 closes it as a side-effect of the integration architecture.
- `BuildOrchestrator(profile, customMutators?)` — wires a real `CsharpMutantOrchestrator` with a fresh `MutantPlacer(new CodeInjection())`.
- `RunOrchestratorOnSource(source, profile)` — end-to-end pipeline run on a source string: parses, builds Compilation+SemanticModel, runs `Mutate`, returns `(Mutants, MutatedTree)`.
- `GetActiveMutators(orchestrator)` — reflection helper for the L2 Profile-Filter integration tests.

Static initialiser seeds `ApplicationLogging.LoggerFactory ??= NullLoggerFactory.Instance` because `RegexMutator`'s constructor calls `CreateLogger<T>()` eagerly and the CLI bootstrap that normally seeds the factory doesn't run in tests.

### L6 — Configuration → Options pipeline tests (15)

Smallest layer, run first to validate `IntegrationTestBase`. Covers: profile flag round-trip, MutationLevel round-trip, default LanguageVersion/Concurrency/Thresholds/Reporters, default profile is `Defaults`, default engine is `Recompile`.

### L1 — Orchestrator + Mutator-Pipeline tests (17)

End-to-end orchestrator on real C# snippets. Covers: simple add → at least one mutant; mutated tree round-trip parses cleanly; unique IDs across mutants; original/replacement nodes populated; all mutants start `Pending`; const-field skipped via DoNotMutate; expression-bodied property is mutable (counterpart); attribute-list contents not mutated; static-constructor → `IsStaticValue=true`; instance-method → `IsStaticValue=false`; BooleanMutator fires on `true`; StringMutator fires on `"hello"`; empty class → no mutants; two methods → both produce mutants; All-profile ⊇ Defaults-profile; Stronger-profile > Defaults-profile count; null-coalescing operator produces mutants.

### L2 — Profile-Filter + Mutator-Liste tests (14)

Combines reflection over every concrete mutator type, the `MutatorProfileFilter` membership check, and the orchestrator's constructor-time filter (Sprint 6 ADR-018). Catches drift between declared `[MutationProfileMembership]` attribute and orchestrator-loaded set. Per-profile `Theory` (×3 each) checks: every active mutator's membership overlaps the active profile (no false positives), and every catalog mutator whose membership includes the profile is active (no false negatives). Subset invariants Defaults ⊆ Stronger ⊆ All. Direct contract check on `MutatorProfileFilter.IsInProfile` via `TypeDrivenReturnMutator` (Sprint 9 Stronger|All-only).

### L3 — EquivalentMutantFilterPipeline + Orchestrator tests (12)

Default pipeline composition (5 filters: IdentityArithmetic + IdempotentBoolean + ConservativeDefaultsEquality + RoslynDiagnostics + RoslynSemanticDiagnostics), filter ID uniqueness (used in diagnostics), and end-to-end RoslynDiagnostics filter on a constructed Mutation whose replacement carries a parser-error diagnostic (must short-circuit). Orchestrator-level integration: simple arithmetic and boolean literal must NOT be over-suppressed.

### L4 — MutantPlacer + Mutation injection tests (8)

Trimmed because most `MutantPlacer` paths are transitively covered by L1+L3. L4 stays focused on the placer's distinguishing behaviour: every mutant leaves an `Injector` annotation in the tree, `MutantPlacer.FindAnnotations` round-trips ID/Engine/Type, the `MutantControl.IsActive` runtime selector appears in the mutated tree, mutated tree is parser-clean, multiple mutants on one statement chain control flow, mutation IDs are invariant-culture-parseable ints, public `MutationMarkers` surface contains the expected markers.

### L5 — Reporter-Pipeline tests (12)

`ReporterFactory` always returns a `BroadcastReporter`. Empty reporter list → empty broadcast. `Reporter.All` returns all 10 known reporters. Single/multi-reporter selections wire only the requested reporters. `BroadcastReporter` forwards `OnMutantsCreated`, `OnStartMutantTestRun`, `OnMutantTested` to every wrapped reporter via Moq verification. Order preservation. Empty broadcast list → no-throw on every lifecycle event. Thread safety: 200-iteration `Parallel.For` on `OnMutantTested` exactly once per call (validates the `Lock` mutex). `OnAllMutantsTested` deliberately not invoked because of its 1s `Thread.Sleep` flush.

## Process lessons

### 1. **Brittle stub → concrete-class init: a Sprint 19 deferral becomes free**

Sprint 18 hardening tests built an `IStrykerOptions` reflection-stub in the unit-test base. Sprint 19's P7 property was rejected because the stub was too brittle to randomize. Sprint 20 sidesteps this by going one layer up: integration tests use `new StrykerOptions { ... }` directly (the concrete class), avoiding the reflection surface entirely. The "deferred P7" item is now obsolete.

### 2. **`ApplicationLogging.LoggerFactory` is a hidden test prerequisite**

`RegexMutator`'s constructor (and several others) call `ApplicationLogging.LoggerFactory.CreateLogger<T>()` eagerly. The CLI bootstrap normally seeds this; tests don't run that bootstrap, so any orchestrator-level test silently NREs at first construction. Solved with a static initializer in `IntegrationTestBase`: `ApplicationLogging.LoggerFactory ??= NullLoggerFactory.Instance`. **Future test-base classes touching the orchestrator must do the same.**

### 3. **`MutantPlacer` ctor signature surprise**

First attempt at `BuildOrchestrator` used `new MutantPlacer(injector: null!)` — but the actual ctor signature is `MutantPlacer(CodeInjection injection)`. CodeInjection is a small DTO that carries the helper-namespace selector expression (and a per-process random suffix to avoid name clashes). The integration test base now constructs it explicitly: `new MutantPlacer(new global::Stryker.Core.InjectedHelpers.CodeInjection())`.

### 4. **Helper namespace has a per-process random suffix**

Wrote a test asserting `mutatedTree.ToString()` contains `injection.HelperNamespace` from a fresh `CodeInjection()` instance — failed because the orchestrator's `MutantPlacer` had its OWN `CodeInjection` with a different random suffix. The fix is to assert on the stable parts of the runtime selector (`MutantControl.IsActive` and the `Stryker` prefix), not on the per-process suffix.

### 5. **FluentAssertions 8 renamed `BeGreaterOrEqualTo` → `BeGreaterThanOrEqualTo`**

Caught at first build of L1. Documented because previous mutator tests used the old name where range assertions weren't needed; the integration tests' subset-size invariants tripped this rename.

### 6. **Meziantou MA0006 trips on `e.ToString() == "?"` but tolerates `o == "x + 1"`**

Pattern detection inside MA0006 distinguishes pure-string equality (`x == y` where both sides are explicitly string-typed) from method-call result equality (`x.ToString() == y`). The latter must use `string.Equals(..., StringComparison.Ordinal)` per the rule. Affected only one test file (L3); the L1 tests with `o => o == "x + 1"` slipped under the radar by being immediately-typed string comparisons.

### 7. **`MutationContext.InStaticValue` is the only context-flow signal observable end-to-end**

Out of `MutationContext`'s flags, `InStaticValue` is the only one that reliably propagates from a parent orchestrator (`StaticConstructorOrchestrator.EnterStatic`) all the way down to `Mutant.IsStaticValue`. Other flow-sensitive flags (`FilteredMutators`, `FilterComment`) are conditionally set by orchestrator nodes that don't trigger from a top-level integration source. L1 tests `Mutate_StaticConstructorBody_MutantsMarkedStatic` + `Mutate_NonStaticMethodBody_MutantsNotMarkedStatic` are the cleanest cross-layer flow check.

### 8. **Semgrep skips test files by default; explicit `--include` needed for verification**

`semgrep scan --config auto tests/...` reports 0 files scanned and 7 "matching `.semgrepignore` patterns" — even though there's no `.semgrepignore`. The default `auto` config has implicit test-file exclusions. Workaround: `semgrep scan --config auto --include "tests/Stryker.Core.Tests/Integration/*.cs"` forces the scan and confirms 0 findings on 6 files (IntegrationTestBase.cs occasionally drops out of the include glob; not security-relevant — pure base class).

## v2.7.0 progress map

```
[done]    Sprint 20 → Integration Tests across 6 layers → v2.7.0   ⭐ MINOR ⭐
```

## Out of scope (deferred)

- **Sprint 21**: Automated E2E in CI (`tests/Stryker.E2E.Tests/`, ProcessSpawnHelper, ~10–20 tests)
- **Coverage report generation** — coverlet file-lock issue from Sprint 18 still open
- **L4 placer-rollback round-trip test** — `MutantPlacer.RemoveMutant` is exercised by the existing CSharpRollbackProcess tests; redundant integration coverage skipped

## v2.x roadmap (still applicable)

- **Sprint 21**: Automated E2E in CI
- **v3.0 batched**: Hard-remove `[Obsolete]` MutationEngine; JsonReport full AOT-trim; Sprint 13 semantic deviations
- **ADR-022 (Proposed)**: Incremental mutation testing
