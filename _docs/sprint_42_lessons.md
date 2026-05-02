# Sprint 42 — RegexMutators Foundation + 4 Smallest Mutator Tests + Orchestrator

**Tag:** v2.29.0 | **Branch:** `feature/42-regex-mutators-tests-foundation`

## Outcome
- New `tests/Stryker.RegexMutators.Tests/` project (foundation, slnx-registered).
- Port: TestHelpers + RegexMutantOrchestratorTest (10 tests) + 4 smallest mutator tests (2+8+3+3=16) = **30 tests grün, zero skips**.
- Solution-wide: 719 grün excl E2E.
- 1 build-fix-cycle (5 trivial errors).

## Files Ported (Sprint 42)
| File | Upstream LOC | Tests |
|------|--------------|-------|
| TestHelpers (helper) | 18 | — |
| RegexMutantOrchestratorTest | 185 | 10 ([Fact]+[Theory]) |
| LookAroundMutatorTest | 65 | 2 |
| GroupToNcGroupMutatorTests | 70 | 8 ([Fact]+[Theory]) |
| CharacterClassNegationMutatorTest | 77 | 3 |
| CharacterClassShortHandNegationMutatorTest | 80 | 3 |
| **Total** | **495 LOC** | **30** |

## RegexMutators Track Plan (3 sprints)
| Sprint | Files | Tests so far |
|--------|-------|--------------|
| **42 (this) ✓** | Foundation + 4 smallest + Orchestrator | 30 |
| 43 | 7 medium-tier mutators | TBD |
| 44 | 5 large mutators (closes track) | TBD |

## Lessons (NEW)
- **S1944 false-positive across assembly boundary**: `RegexMutatorBase<T>` implements `IRegexMutator` in production, but Sonar can't see the inheritance from the test assembly. Mechanical fix: `[SuppressMessage("Major Code Smell", "S1944", Justification = "...")]` with cross-assembly explanation.
- **Custom MSTest `[GroupToNcGroupAttribute : DataRowAttribute]` → `[Theory] [InlineData(string, new[] { ... })]`**: The upstream pattern of using `file class GroupToNcGroupAttribute : DataRowAttribute` to provide DisplayName doesn't translate to xUnit. Drop the custom attribute, use `[InlineData(string, new[] { ... })]` directly.
- **`new[] { ... }` for string array in InlineData**: explicit collection-expression syntax doesn't work in `[InlineData(..., [...])]` — use `new[] { ... }` instead.
- **S2971 useless ToArray()**: `result.Select(...).ToArray().Should().BeEquivalentTo(expected)` → drop the `.ToArray()` (FluentAssertions handles IEnumerable directly).

## Roadmap
- **Sprint 43**: 7 medium-tier mutator tests (~700 LOC)
- **Sprint 44**: 5 large mutator tests (~1100 LOC) — closes RegexMutators track
- **Sprint 45+**: Stryker.Core.UnitTest tranches (5 — largest module of all)
- **Investigation Sprint TBD**: 17 cross-sprint behaviour-delta skips
