---
current_sprint: "61"
sprint_goal: "IProjectAnalysis Mock Builder (unblock Initialisation/Buildalyzer ports) → v2.47.0"
branch: "feature/61-iprojectanalysis-mock-builder"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 61 — IProjectAnalysis Mock Builder

## Outcome
- New `tests/Stryker.TestHelpers/ProjectAnalysisMockBuilder.cs` — 18 fluent
  methods, 17/17 IProjectAnalysis members covered + composables
  (WithProperty / WithItemPaths / WithReferenceAlias)
- 11 builder unit tests + 14 IProjectAnalysisExtensions integration tests
  (validation port: real production extension methods consume the builder)
- Dogfood-project total: 411 grün + 9 skip = 420
- Solution-wide: 1227 grün + 27 skip ohne E2E
- Existing `TestHelper.SetupProjectAnalyzerResult` (Sprint 25-26 helper)
  unchanged — back-compat preserved

## Lessons (NEW)
- **Maxential branches A vs B for design decisions**: param-bag-extend
  (Branch A) vs fluent-builder (Branch B) — Branch B won on composability
  of WithProperty/WithItemPaths and avoids 12+ optional-parameter signatures
- **NuGet.Frameworks runtime override pattern for dogfood tests**: 
  Directory.Build.props pins `<PackageReference Include="NuGet.Frameworks"
  PrivateAssets="all" ExcludeAssets="runtime" />` globally for MSBuildLocator
  transitive guard; tests calling `IProjectAnalysisExtensions.TargetsFullFramework`
  / `GetNuGetFramework` need the assembly at runtime → re-declare in test
  csproj with `<PackageReference Update="NuGet.Frameworks" PrivateAssets="all"
  ExcludeAssets="" />` (Update + empty ExcludeAssets re-enables runtime flow
  without changing PrivateAssets semantics for downstream).
