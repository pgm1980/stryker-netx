# Sprint 61 — IProjectAnalysis Mock Builder (Foundation Sprint)

**Tag:** v2.47.0 | **Branch:** `feature/61-iprojectanalysis-mock-builder`

## Outcome
- New `tests/Stryker.TestHelpers/ProjectAnalysisMockBuilder.cs` — fluent
  builder covering all 17 `IProjectAnalysis` members + 3 composable
  collection-style methods (`WithProperty`, `WithItemPaths`, `WithReferenceAlias`)
- 11 builder unit tests + 14 IProjectAnalysisExtensions integration tests
  (validation: real production extension methods consume the builder)
- Dogfood-project total: 411 green + 9 skip = 420
- Solution-wide: 1227 green + 27 skip without E2E
- Sprint-25/26 `TestHelper.SetupProjectAnalyzerResult` left untouched
  (back-compat preserved — existing 6 callers continue to compile)

## Maxential Reasoning
- **Branch A "extend-existing"**: extend the param-bag helper with 5+ new
  optional parameters (AssemblyName, OutputRefFilePath, EmbeddedResourcePaths,
  AnalyzerAssemblyPaths, GetItemPaths). Concluded acceptable but
  **param-explosion** at 12+ optional parameters; readability degrades.
- **Branch B "fluent-builder"**: dedicated builder class with chainable
  `.WithX(...)` methods + `Build()`/`BuildMock()` entry points.
  Strong recommendation — composable for property/item-path bags,
  scales naturally as new IProjectAnalysis members are added.
- Merged Branch B back into main (`merge_branch` strategy: `full_integration`).

## Lessons (NEW)
- **NuGet.Frameworks runtime override**: `Directory.Build.props` pins
  `<PackageReference Include="NuGet.Frameworks" PrivateAssets="all"
  ExcludeAssets="runtime" />` globally so the MSBuildLocator transitive
  guard does not emit MSBL001 in downstream consumers. Tests that call
  `IProjectAnalysisExtensions.TargetsFullFramework` /
  `GetNuGetFramework` need the assembly at runtime → re-declare in
  the test csproj with:
  ```xml
  <PackageReference Update="NuGet.Frameworks" PrivateAssets="all" ExcludeAssets="" />
  ```
  `Update` (not `Include`) modifies the existing global reference;
  empty `ExcludeAssets` re-enables runtime flow without altering
  `PrivateAssets` (so downstream consumers of the test project — none
  in practice — still get the global semantics).
- **Builder vs param-bag tradeoff**: the existing param-bag helper
  (`SetupProjectAnalyzerResult`) is fine for ≤8 params; beyond that
  the call-site loses readability and parameter-position errors creep in.
  The builder lets every test specify ONLY what it cares about
  (e.g. `.WithReferences(...)` without naming the 7 other params).
- **Validation strategy for foundation sprints**: instead of porting a
  large upstream test (ProjectMutatorTests would have dragged in VsTest
  + MutationTestProcess), exercise the new helper through narrow,
  real production code — here `IProjectAnalysisExtensions` extension
  methods that consume `IProjectAnalysis`. 14 short tests prove the
  builder generates realistic mocks across `IsValid`, `IsValidFor`,
  `GetLanguage`, `TargetsFullFramework`, `GetReferenceAssemblyPath`,
  `TargetPlatform`, `IsSignedAssembly`, `GetWarningLevel`.
- **Sensible defaults pattern**: a builder with good defaults
  (`Succeeded=true`, `Language="C#"`, `TargetFramework="net10.0"`,
  empty collections, derived TargetFileName/TargetDir/OutputFilePath
  from ProjectFilePath) means `new ProjectAnalysisMockBuilder().Build()`
  is immediately usable. Tests opt-in to non-default behaviour
  (`AsFailed()`, `AsTestProject()`).

## Files Changed
- `tests/Stryker.TestHelpers/ProjectAnalysisMockBuilder.cs` (new, 240 LOC)
- `tests/Stryker.Core.Dogfood.Tests/TestHelpers/ProjectAnalysisMockBuilderTests.cs` (new, 11 tests)
- `tests/Stryker.Core.Dogfood.Tests/TestHelpers/ProjectAnalysisMockBuilderExtensionsIntegrationTests.cs` (new, 14 tests)
- `tests/Stryker.Core.Dogfood.Tests/Stryker.Core.Dogfood.Tests.csproj` (NuGet.Frameworks runtime override)
- `_docs/sprint_61_lessons.md` (this file)
- `.sprint/state.md` (Sprint 61 entry)
