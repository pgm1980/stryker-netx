---
current_sprint: "99"
sprint_goal: "MSBuild -version space + multi-line nuget-restore version parse → v2.85.0"
branch: "feature/99-nuget-restore-msbuild-version-multiline"
started_at: "2026-05-02"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Sprint 99 — NugetRestoreProcess MSBuild -version space + multi-line parse

## Spawned from Sprint 98 closing note
Sprint 98 (`.sprint/state.md` previous content): "Spawned task: fix production bug + update test mock back to spaced."

## Outcome — 2 coupled production bugs + 1 new test + 2 mock-fixups
1. **`MsBuildHelper.GetVersion()`** built `dotnet msbuild-version /nologo` (no space) → dotnet driver treats `msbuild-version` as a tool name → "command not found" → ExitCode != Success → `GetVersion` silently returned `string.Empty`. Production never got a version string; `RestorePackages` skipped the `-MsBuildVersion` flag entirely. **Silent malfunction.**
2. **Once #1 is fixed**, .NET-SDK MSBuild emits a two-line response (locale-dependent banner + numeric version). Previous `FindMsBuildShortVersion` `.Trim()`-ed the whole blob and passed it to `nuget.exe -MsBuildVersion`, failing the first restore and recovering via the no-version fallback while emitting a misleading `LogFailedNugetRestore` error.

Both fixed in single commit. Sprint 98 mocks updated to assert the corrected `"msbuild -version /nologo"` form. New test mocks the real two-line SDK output and asserts `nuget.exe` receives ONLY the numeric version.

## Verification
- Build: 0 warnings, 0 errors
- Targeted tests: 3/3 green (HappyFlow, ShouldThrowOnNugetNotInstalled, HappyFlow_WithMultiLineMsBuildVersionOutput_ExtractsNumericVersionForNugetRestore)
- Initialisation namespace sanity sweep: 15 green, 0 fail (16 pre-existing dogfood-skips)
- Semgrep on all 3 changed files: 0 findings, 80 rules
- SonarAnalyzer S2971 caught initial `.Where().LastOrDefault()` draft — refactored to `LastOrDefault(predicate)` per TreatWarningsAsErrors

## Files
- `src/Stryker.Core/Helpers/MsBuildHelper.cs` (line 51: spacing fix + `TrimEnd()` for pre-existing trailing-space case in non-.exe configured MSBuild path)
- `src/Stryker.Core/Initialisation/NugetRestoreProcess.cs` (`FindMsBuildShortVersion`: `Split(['\r','\n'], RemoveEmptyEntries) → Select(Trim) → LastOrDefault(predicate)`)
- `tests/Stryker.Core.Dogfood.Tests/Initialisation/NugetRestoreProcessTests.cs` (full rewrite: docstring + 2 existing mock-fixups + new multi-line test)
