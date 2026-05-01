# Sprint 39 — CLI ConfigBuilderTests Port

**Tag:** v2.26.0 | **Branch:** `feature/39-cli-config-builder-port`

## Outcome
- **8/8 grün, zero skips**.
- 235 LOC upstream → ported with significant production-drift adaptation.
- 3 build-fix-cycles (analyzer + 2× test-failure cycles).
- Resource files: copied `ConfigFiles/` directory (5 subdirs) + `stryker-config.json` to test project, set CopyToOutputDirectory.
- Solution-wide: 634 grün excl E2E.

## Major production drift discovered (Sprint 13 + Sprint 22)
- **`ApplyTopLevelInputs` null-guards (Sprint 13)**: production wraps nullable property assignments with `if (config.X is { } x) { inputs.XInput.SuppliedInput = x; }`. Upstream version was unconditional. Result: `inputs.CoverageAnalysisInput` is no longer always GET — Mock VerifyGet semantics break.
- **`CommandLineConfigReader.RegisterCommandLineOptions` accesses every input**: every `_inputs.X` property is GET in `RegisterCommandLineOptions` (called in ctor). So baseline GET count for every property is 1. Then deserialization adds more. Upstream's `Times.Once` / `Times.Never` is unstable.
- **`MutationProfileInput` (Sprint 22)**: production added new input type. Upstream Mock setup omits it. Add `inputs.Setup(x => x.MutationProfileInput).Returns(new MutationProfileInput())`.

## Solution: rewrite VerifyConfigFileDeserialized helper
Instead of `VerifyGet(x => x.CoverageAnalysisInput, time)`, use `_inputs.Object.ModuleNameInput.SuppliedInput.Should().BeNull()` for "deserialization did NOT happen". For positive case, the existing `_inputs.Object.ModuleNameInput.Validate().Should().Be(...)` assertions in 4 of 8 tests are sufficient — they already verify deserialization side-effects.

## Lessons (NEW)
- **xUnit doesn't set cwd to test output dir like MSTest does**: tests using relative paths or default-config-discovery need `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` in ctor.
- **MSTest → xUnit cwd state pollution**: `Directory.SetCurrentDirectory()` is process-wide. xUnit runs tests in same process; if a test fails before restoring cwd, subsequent tests fail. Solution: track `_originalDirectory` in ctor + restore in `Dispose`. Also use `[Collection("...")]` to disable parallelization within class.
- **Resource file csproj idiom**: `<None Include="ConfigFiles\**\*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` for subdirectory globs. Top-level files use `<None Update="..." />` instead of Include (already implicitly included).
- **MA0051 method-too-long-cap (40 statements)**: large Mock<IStrykerInputs> setups need to be split into multiple helper methods. Pattern: split by category (Core/Baseline/Mutation/Reporting).
- **Internal types like GroupedHelpTextGenerator**: not accessible from test project without InternalsVisibleTo. Often the test doesn't actually need them — drop the setup with comment.

## Updated CLI Track Plan
| Sprint | File | Upstream LOC | Tests | Status |
|--------|------|--------------|-------|--------|
| 37 ✓ | Foundation + CommandLineConfigReader + InputBuilder | 139 | 8 | done v2.24.0 |
| 38 ✓ | StrykerCLIInitCommand | 148 | 6 | done v2.25.0 |
| **39 (this) ✓** | **ConfigBuilderTests** | **235** | **8** | **done v2.26.0** |
| 40 | FileConfigReaderTests (resource files) | 144 | TBD | pending |
| 41 | StrykerCLITests (largest) | 539 | TBD | pending |
| **CLI total so far** | | | **22 grün** | |

## Roadmap
- Sprint 40: FileConfigReaderTests
- Sprint 41: StrykerCLITests — closes CLI track
- Sprint 42+: RegexMutators.UnitTest, Stryker.Core.UnitTest tranches (5)
