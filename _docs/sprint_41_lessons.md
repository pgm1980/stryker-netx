# Sprint 41 — CLI StrykerCLITests Port (closes CLI track)

**Tag:** v2.28.0 | **Branch:** `feature/41-cli-stryker-cli-tests-port`

## Outcome
- **51/51 grün, zero skips**.
- 539 LOC upstream → largest single CLI test file portiert.
- 1 build-fix-cycle (1 trivial S2925 — Thread.Sleep replaced with Task.Delay).
- **Closes CLI dogfood track** (Sprints 37-41 = 5 sprints, 77 grün across 6 files).
- Solution-wide: 689 grün excl E2E.

## CLI Track Complete (Sprints 37-41)
| Sprint | File | Upstream LOC | Tests | Tag |
|--------|------|--------------|-------|-----|
| 37 | Foundation + CommandLineConfigReader + InputBuilder | 139 | 8 | v2.24.0 |
| 38 | StrykerCLIInitCommand | 148 | 6 | v2.25.0 |
| 39 | ConfigBuilderTests | 235 | 8 | v2.26.0 |
| 40 | FileConfigReaderTests | 144 | 4 | v2.27.0 |
| **41 (this)** | **StrykerCLITests (largest)** | **539** | **51** | **v2.28.0** |
| **Track total** | **6 files** | **1205** | **77** | **CLOSED** |

## Lessons (NEW)
- **S2925 forbids Thread.Sleep in tests**: replace `Thread.Sleep(20)` with `await Task.Delay(20)`. Trivial mechanical fix.
- **Multi-arg `params string[] argName` → `string arg, string value`**: upstream MSTest `[DataRow("--key", "val")]` with `params string[]` → xUnit `[InlineData("--key", "val")]` requires explicit positional parameters. Trivial conversion.
- **Test logo string brittleness**: upstream tests assert exact "A new version of Stryker.NET..." string. stryker-netx may emit different copy. Use stable substring like the version number ("10.0.0") instead.

## Solution-wide totals after CLI track close
- Solution-wide tests: 689 grün excl E2E
- Dogfood tracks complete:
  - VsTest (Sprints 25-29): 46 grün + 11 skip
  - MTP (Sprints 30-36): 136 grün + 6 skip
  - **CLI (Sprints 37-41): 77 grün, 0 skip**
  - + Stryker.Solutions.Tests (Sprint 24): 15 grün
- Behaviour-delta-skip total: 18 cross-sprint skips (all documented for future investigation sprint)

## Roadmap
- **Sprint 42+**: RegexMutators.UnitTest (size TBD)
- **Sprint 43+**: Stryker.Core.UnitTest tranches (5 — largest module of all)
- **Investigation Sprint TBD**: 17 cross-sprint behaviour-delta skips (from VsTest + MTP tracks, none from CLI track)
