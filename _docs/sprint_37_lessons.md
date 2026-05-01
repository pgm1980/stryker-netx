# Sprint 37 — CLI Foundation + 2 Smallest Test Files

**Tag:** v2.24.0 | **Branch:** `feature/37-cli-tests-foundation`

## Outcome
- New `tests/Stryker.CLI.Tests/` project (foundation, slnx-registered).
- Port: CommandLineConfigReaderTests (5 [Fact]s) + Logging/InputBuilderTests (3 [Fact]s).
- 8/8 grün, zero skips.
- Solution-wide: 620 grün excl E2E.
- 1 build-fix-cycle (4 trivial analyzer errors).

## CLI Track Decomposition (post-Sprint 37 plan)
| Sprint | File | Upstream LOC | Tests |
|--------|------|--------------|-------|
| 37 (this) | Foundation + CommandLineConfigReaderTests + Logging/InputBuilderTests | 139 | 8 |
| 38 | ConfigBuilderTests + FileConfigReaderTests + StrykerCLIInitCommandTests | 527 | TBD |
| 39 | StrykerCLITests (largest) | 539 | TBD |
| **Track total** | **6 files** | **~1205** | TBD |

## Lessons (NEW)
- **`DateTime.TryParse` triggers MA0011/S6580** (CultureInfo missing). Mechanical fix: `using System.Globalization;` + `DateTime.TryParse(str, CultureInfo.InvariantCulture, out _)`.
- **CA1001 on test class with `CommandLineApplication` field**: McMaster's CommandLineApplication is `IDisposable`. Test class must implement IDisposable + dispose the field. Trivial fix: `public sealed class T : IDisposable { public void Dispose() => _app.Dispose(); }`.
- **CA1859 on `IStrykerInputs` field type**: analyzer wants concrete `StrykerInputs` for performance. Suppress with Sprint 28 pattern: "perf-not-test-concern" — test asserts behaviour of the interface directly.

## Mechanical-fix knowledge base (cumulative through Sprint 37)
DateTime/CultureInfo trio added. Test-class IDisposable for McMaster pattern.

## Roadmap
- Sprint 38: ConfigBuilderTests (235 LOC) + FileConfigReaderTests (144 LOC) + StrykerCLIInitCommandTests (148 LOC) = ~530 LOC, 3 files
- Sprint 39: StrykerCLITests (539 LOC, largest CLI file) — closes CLI track
- Sprint 40+: RegexMutators.UnitTest, Stryker.Core.UnitTest tranches (5)
