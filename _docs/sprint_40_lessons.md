# Sprint 40 — CLI FileConfigReaderTests Port

**Tag:** v2.27.0 | **Branch:** `feature/40-cli-file-config-reader-port`

## Outcome
- **4/4 grün, zero skips** (1 [Fact] + 1 [Theory]×2 + 1 [Fact]).
- 144 LOC upstream → ported.
- **1-shot port** (zero build-fix-cycles) — predicted by accumulating mechanical-fix knowledge from Sprints 26-39.
- Solution-wide: 638 grün excl E2E.

## Resource files
- `filled-stryker-config.json` (test fixture for JSON config parsing)
- `filled-stryker-config.yaml` (test fixture for YAML config parsing)
- Both copied to test project + csproj `<None Update CopyToOutputDirectory>` idiom.

## Lessons
- **`Should().NotBeNull().And.Be(true/false)`**: combined null-check + value-check pattern for nullable bool. Replaces Shouldly's `.ShouldNotBeNull().ShouldBeFalse()`.
- **`Should().ContainSingle().Which.Should().Be(...)`**: single-element-and-equality pattern (Sprint 38 lesson) reused.
- **xUnit cwd handling**: `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` in ctor + restore in Dispose (Sprint 39 lesson) reused.
- **`[Collection("ConfigBuilderSequential")]`**: shared with ConfigBuilderTests since both mutate cwd.

## Updated CLI Track Plan
| Sprint | File | Upstream LOC | Tests | Status |
|--------|------|--------------|-------|--------|
| 37 ✓ | Foundation + CommandLineConfigReader + InputBuilder | 139 | 8 | done v2.24.0 |
| 38 ✓ | StrykerCLIInitCommand | 148 | 6 | done v2.25.0 |
| 39 ✓ | ConfigBuilderTests | 235 | 8 | done v2.26.0 |
| **40 (this) ✓** | **FileConfigReaderTests** | **144** | **4** | **done v2.27.0** |
| 41 | StrykerCLITests (largest) | 539 | TBD | pending |
| **CLI total so far** | **5/6 files** | | **26 grün** | |

## Roadmap
- **Sprint 41**: StrykerCLITests — closes CLI track (final CLI file)
- **Sprint 42+**: RegexMutators.UnitTest, Stryker.Core.UnitTest tranches (5)
- **Investigation Sprint TBD**: 17 cross-sprint behaviour-delta skips
