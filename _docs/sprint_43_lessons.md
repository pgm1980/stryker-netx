# Sprint 43 — RegexMutators 7 Medium-Tier Mutator Tests

**Tag:** v2.30.0 | **Branch:** `feature/43-regex-mutators-medium-tests`

## Outcome
- **49 new tests grün + 1 skipped** (CharClassShortAny 6 + CharClassShortNull 6 + CharClassToAny 6 + QuantReluctant 6 + QuantUnlimited 2 + QuantShort 8 + UnicodeCharClass 5+1skip = 49+1).
- RegexMutators-project total: **79 grün + 1 skip = 80** (Sprint 42: 30 + Sprint 43: 50).
- Solution-wide: 768 grün excl E2E.
- 1 build-fix-cycle (7 trivial errors).

## Files Ported (Sprint 43)
- CharacterClassShorthandAnyCharMutatorTests
- CharacterClassShorthandNullificationMutatorTests
- CharacterClassToAnyCharMutatorTests
- QuantifierReluctantAdditionMutatorTests
- QuantifierUnlimitedQuantityMutatorTest
- QuantifierShortMutatorTests
- UnicodeCharClassNegationMutatorTests (1 [Fact(Skip = "...")] preserves upstream `[Ignore]`)

## Lessons (NEW)
- **S4144 method-identical-implementations**: distinct test contracts often share same body (e.g., `DoesNotMutateNonCharacterClasses` and `DoesNotMutateToItself` both call `ParseAndMutate(...).Should().BeEmpty()`). Suppress with "preserves upstream test surface" justification — preserve test intent.
- **S3878 collection-creation-redundancy**: `Should().BeEquivalentTo([elem1, elem2])` (collection-expression) trips S3878. Use direct `Should().BeEquivalentTo(elem1, elem2)` (params overload).
- **IDE0305 collection-init**: `var x = result as RegexMutation[] ?? result.ToArray()` → just `result.ToArray()` (the cast-or-allocate pattern was upstream optimization, irrelevant for tests).
- **`[Ignore("...")]` → `[Fact(Skip = "...")]`**: upstream MSTest `[Ignore]` translates 1:1 to xUnit `[Fact(Skip = "...")]`.

## Updated RegexMutators Track Plan
| Sprint | Files | Tests | Status |
|--------|-------|-------|--------|
| 42 ✓ | Foundation + 4 smallest + Orchestrator | 30 | done v2.29.0 |
| **43 (this) ✓** | **7 medium-tier mutators** | **49+1skip** | **done v2.30.0** |
| 44 | 5 large mutators (closes track) | TBD | pending |
| **Track total so far** | **12 of 18 files** | **79+1skip** | |

## Roadmap
- **Sprint 44**: 5 large mutator tests (~1100 LOC) — closes RegexMutators track
- **Sprint 45+**: Stryker.Core.UnitTest tranches (5 — largest module of all)
- **Investigation Sprint TBD**: 18 cross-sprint behaviour-delta skips (17 + 1 Sprint-43 skip)
