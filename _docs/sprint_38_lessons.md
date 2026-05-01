# Sprint 38 — CLI StrykerCLIInitCommandTests Port

**Tag:** v2.25.0 | **Branch:** `feature/38-cli-medium-tests-port`

## Outcome
- **6/6 grün, zero skips** (4 [Fact]s + 2 [Theory] cases on `InitCustomPath`).
- Scope mid-sprint reduced from "3 medium files" to "1 medium file" (StrykerCLIInitCommand only) — see below.
- 1 build-fix-cycle (~10 trivial errors all in mechanical-fix knowledge base).
- 148 LOC upstream → port complete.
- Solution-wide: 626 grün excl E2E.

## Scope decision (mid-sprint)
Initial Sprint 38 scope: ConfigBuilder + FileConfigReader + StrykerCLIInitCommand. After reading all 3 upstream files I narrowed to **StrykerCLIInitCommand only** because:
- ConfigBuilderTests has extensive `Mock<IStrykerInputs>` cascade (~50 input types) — risk of missing v2.x types like `MutationProfileInput`. Defer to Sprint 39 for focused work.
- FileConfigReaderTests requires resource files (filled-stryker-config.json/yaml) copied to test output via csproj idiom — defer to Sprint 40.
- StrykerCLIInitCommand uses MockFileSystem (no on-disk resources) and is pattern-clean.
- Honest scope-reduction beats ambitious-fail (Sprint 25 lesson).

## Lessons (NEW)
- **`Spectre.Console.Testing` package needed**: missed in Sprint 37 csproj because tests didn't use TestConsole. Added now. Always verify upstream `using` directives map to existing csproj package refs.
- **S6966 (Await ReadAllTextAsync/WriteAllTextAsync)**: file-level suppress with "perf-not-test-concern" — tests use sync File ops on MockFileSystem (in-memory, no real I/O).
- **CA1859 on `IFileSystem _fileSystemMock = new MockFileSystem()` field**: same Sprint 28 + Sprint 37 perf-not-test-concern pattern.
- **S6608 `args.Last()` → `args[^1]`**: range-indexer pattern enforced on indexed collections. Trivial mechanical fix.
- **`config.Reporters.Should().BeEquivalentTo(new ReportersInput().Default)` for collection equality**: when comparing IEnumerable defaults, BeEquivalentTo does element-wise comparison (Sprint 24 lesson — char-deep-equivalence is for IEnumerable<string>).
- **`Should().ContainSingle().Which.Should().Be(...)`**: replaces Shouldly's `.ShouldHaveSingleItem().ShouldBe(...)`. The `.Which` accessor exposes the single element for further assertions.

## Updated CLI Track Plan
| Sprint | File | Upstream LOC | Tests | Status |
|--------|------|--------------|-------|--------|
| 37 ✓ | Foundation + CommandLineConfigReader + InputBuilder | 139 | 8 | done v2.24.0 |
| **38 (this) ✓** | **StrykerCLIInitCommand** | **148** | **6** | **done v2.25.0** |
| 39 | ConfigBuilderTests (Mock<IStrykerInputs> cascade) | 235 | TBD | pending |
| 40 | FileConfigReaderTests (resource files) | 144 | TBD | pending |
| 41 | StrykerCLITests (largest) | 539 | TBD | pending |
| **Track total** | **6 files** | **~1205** | TBD | |

## Roadmap
- **Sprint 39**: ConfigBuilderTests
- **Sprint 40**: FileConfigReaderTests (with resource files)
- **Sprint 41**: StrykerCLITests — closes CLI track
- **Sprint 42+**: RegexMutators.UnitTest, Stryker.Core.UnitTest tranches (5)
- **Investigation Sprint TBD**: 17 cross-sprint behaviour-delta skips
