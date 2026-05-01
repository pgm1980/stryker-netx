# Sprint 19 — Filter Tests + FsCheck Properties: Lessons Learned

**Sprint:** 19 (2026-05-01, autonomous run)
**Branch:** `feature/19-filter-tests-fscheck-properties`
**Base:** v2.5.0 (Sprint 18 closed)
**Final Tag:** `v2.6.0`
**Type:** Test-only release. Zero production-code change.
**Maxential:** 4 thoughts. **ToT:** 9 nodes evaluating 7 candidate properties; 5 chosen.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| Item B — 6 filter test files | ✅ 5 filters + pipeline |
| Item C — 3 FsCheck property test files | ✅ 5 properties chosen via ToT |
| `MutatorTestBase` extended with `BuildMutation` helper | ✅ |
| `FsCheck.Xunit` package added to test-project | ✅ |
| **308 tests** in Stryker.Core.Tests | ✅ all green |
| **335 tests solution-wide** (308 + 17 Sample + 10 Architecture) | ✅ |
| Test-suite execution | < 2s (with FsCheck-generated case multiplexing) |
| Sample E2E (default profile) | ✅ 100% (zero production change) |
| Semgrep | ✅ clean |
| Tag | `v2.6.0` |

## What landed

### Item B — Filter Unit Tests (~30 new tests)

6 test files in `tests/Stryker.Core.Tests/Mutants/Filters/`:

- **IdentityArithmeticFilterTests** — literal-zero/one identity catches + abstain-on-non-binary + abstain-on-real-mutation
- **IdempotentBooleanFilterTests** — double-negation collapse + logical-identity preservation
- **ConservativeDefaultsEqualityFilterTests** — uint==0 → uint<0 (real CSharpCompilation), signed-int abstain, null-SemanticModel
- **RoslynDiagnosticsEquivalenceFilterTests** — null-replacement + valid-syntax + parser-error short-circuit
- **RoslynSemanticDiagnosticsEquivalenceFilterTests** — null-SemanticModel + null-replacement + non-expression scope-skip
- **EquivalentMutantFilterPipelineTests** — Default-content + OR-semantics with Moq mocks + empty-pipeline + FindEquivalentFilter-returns-first-match

`MutatorTestBase.BuildMutation(original, replacement, type, displayName)` helper added — filters consume Mutation objects, not SyntaxNodes directly.

### Item C — FsCheck Property Tests (5 properties)

ToT 9-node evaluation chose 5 of 7 candidate properties (P1, P2, P4, P5, P6); rejected P3 (string-generator complexity, score 0.30) and P7 (IStrykerOptions stub brittleness from Sprint 18, score 0.40).

3 property test files in `tests/Stryker.Core.Tests/Properties/`:

- **MutationProfileProperties** (P1 + P5):
  - `Profile_BitwiseSelfOr_IsIdentity` (Theory ×8 — every valid profile combination)
  - `Profile_BitwiseSelfAnd_IsIdentity` (Theory ×4)
  - `MembershipAttribute_RoundtripsProfileValue` (`[Property(MaxTest=50)]`)
  - `OredProfile_HasFlagOfBothComponents` (`[Property(MaxTest=50)]`)

- **MutatorReflectionProperties** (P2):
  - `AllConcreteMutators_HaveProfileMembershipAttribute` — reflection over all 52 mutators
  - `AllConcreteMutators_HaveNonNoneProfile`
  - `AllConcreteMutators_ImplementIMutator`
  - `RandomMutatorIndex_AlwaysReturnsValidProfile` (`[Property(MaxTest=50)]` with int → modulo)

- **FilterPipelineProperties** (P4 + P6):
  - `Pipeline_IsEquivalent_EqualsAnyOfMockedFilters` (`[Property(MaxTest=30)]` over `bool[]` arrays of mocked filter results)
  - 3 idempotence Facts (one per concrete filter)

**Total generated cases**: ~50 + ~50 + ~50 + ~30 = ~180 randomized property cases across the 5 [Property] methods, plus 12 Theory cases over MutationProfile combinations.

## Process lessons

### 1. **FsCheck.Xunit v3 has different API than v2 — direct typed parameters trump `Prop.ForAll`**

First-pass attempts used FsCheck v2-style:
```csharp
[Property]
public Property Foo() => Prop.ForAll(Gen.Elements<...>(...).ToArbitrary(), x => ...);
```

This failed compilation because `Gen<T>`, `Prop`, `Arb` are no longer in the C# API surface in FsCheck v3. The v3 idiom for FsCheck.Xunit is **direct typed parameters**:
```csharp
[Property(MaxTest = 50)]
public bool MyProperty(int rawValue, MutationProfile profile) { ... }
```

FsCheck.Xunit auto-generates random values for any typed parameter and shrinks failures. Much cleaner. Documented for future `[Property]` work.

### 2. **MaxTest tuning balances coverage vs. wall-clock**

Default `MaxTest = 100` cases per Property × 5 properties × 50ms each = 25s wall-clock. Reduced to `MaxTest = 30-50` per property to keep total test-suite under 2s. For pure-CPU enum/reflection properties, 30-50 is more than enough — the deterministic invariants don't benefit from larger sample sizes.

### 3. **Pipeline-OR property catches mocking framework limitations**

`Pipeline_IsEquivalent_EqualsAnyOfMockedFilters` over arbitrary `bool[]` correctly exercises the empty-pipeline case + single-mock case + many-mock case — would have caught a regression in pipeline-OR-implementation. The Moq `Setup(...).Returns(value)` pattern integrates cleanly with FsCheck's per-iteration mock construction.

### 4. **`AllConcreteMutators_HaveProfileMembershipAttribute` is the highest-value reflection property**

This property catches the single most common future-bug-class: a developer adds a new Mutator class to `src/Stryker.Core/Mutators/` and forgets the `[MutationProfileMembership(...)]` attribute. The orchestrator's `MutatorProfileFilter` would then default the new mutator to "all profiles", silently violating the v1.x parity contract. Property-test catches this immediately.

## v2.6.0 progress map

```
[done]    Sprint 19 → Filter Tests + FsCheck Properties → v2.6.0   ⭐ MINOR ⭐
```

## Out of scope (deferred)

- **Property P3** (SyntaxFactory roundtrip on random strings) — string-generator complexity not worth it for v2.6.0
- **Property P7** (MutatorBase.Mutate level dispatch) — IStrykerOptions stub brittleness; revisit when stub is replaced by real configuration helper
- **Coverage report generation** — coverlet file-lock issue from Sprint 18 still open
- **Smoke→detail upgrades** for Sprint-18 mutator tests — when bug-classes emerge

## v2.x roadmap (still applicable)

- **v3.0 batched**: Hard-remove `[Obsolete]` MutationEngine; JsonReport full AOT-trim; Sprint 13 semantic deviations
- **ADR-022 (Proposed)**: Incremental mutation testing
- **Sprint-20+ candidates**: P3/P7 properties; coverage report; smoke→detail upgrades
