# Sprint 21 — Automated E2E in CI: Lessons Learned

**Sprint:** 21 (2026-05-01, autonomous run)
**Branch:** `feature/21-automated-e2e`
**Base:** v2.7.0 (Sprint 20 closed)
**Final Tag:** `v2.8.0`
**Type:** Test-only release. Zero production-code change.
**Maxential:** 15 thoughts (1 branch project-location + 1 revision build-strategy). **ToT:** 13-candidate test ranking → 10 selected.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| 4 fast smoke/error tests (Help, Help+core options, missing solution, unknown reporter) | ✅ ~5s |
| 6 cached Defaults-profile tests (score+totals, JSON parses, reports-dir, file-map, status vocabulary, multi-reporter) | ✅ ~51s |
| **10 new E2E tests** in `tests/Stryker.E2E.Tests/` | ✅ all green |
| **423 tests solution-wide** (386 Stryker.Core.Tests + 17 Sample + 10 Architecture + 10 E2E) | ✅ |
| Sample E2E (manual sanity) | ✅ 100% mutation score (5/5 mutants killed) |
| Semgrep | ✅ 0 findings on 14 E2E source files |
| Tag | `v2.8.0` |

## What landed

### Project — `tests/Stryker.E2E.Tests/`

New project entry in `stryker-netx.slnx`. References `Stryker.CLI` with `ReferenceOutputAssembly=false` so the build chain produces the CLI binary on disk without compile-time API coupling — tests spawn the binary as a subprocess, they don't link against its types.

### Infrastructure (one file per type per Meziantou MA0048)

- **`RepoRoot`** — walks parent dirs from `AppContext.BaseDirectory` until `stryker-netx.slnx` is found, then exposes `SamplesDir`, `SampleSlnx`, `StrykerCliBuildOutput` paths.
- **`MutationReport` + `ReportThresholds` + `FileReport` + `MutantEntry` + `MutantTotals`** — minimal projection of the mutation-testing-elements-schema-v1 emitted by Stryker's JsonReporter. Five files, one class each.
- **`StrykerRunResult`** — record carrying `(ExitCode, StdOut, StdErr, StrykerOutputRunDir, JsonReportPath, Report)`.
- **`ProcessSpawnHelper`** — `RunCli(args, workDir, timeout)` and `RunStrykerAgainstSample(extraArgs, timeout)`. Spawns `dotnet exec Stryker.CLI.dll`, captures streams concurrently, snapshots StrykerOutput dirs before the run to identify the new run-dir, parses the JSON report. Refactored into 5 small methods to satisfy MA0051 (≤ 60 lines each).
- **`BuildFixture`** — defensive fallback: if `Stryker.CLI.dll` is missing on first test, runs `dotnet build src/Stryker.CLI -c Debug --nologo`. Normally a no-op because the test project's `ProjectReference ReferenceOutputAssembly=false` already establishes the build dependency.
- **`StrykerRunCacheFixture`** — argument-tuple-keyed cache so multiple `[Fact]`s can share one Stryker run. Two canonical accessors: `GetDefaultsRunWithJsonReporter()` and `GetDefaultsRunWithJsonAndHtmlReporters()`.
- **`E2ETestCollection`** — shared collection definition that wires `BuildFixture` + `StrykerRunCacheFixture` once across ALL test classes (without it, each `IClassFixture` would get its own fixture and re-run Stryker per class).
- **`AssemblyInfo.cs`** — assembly-wide `[CollectionBehavior(DisableTestParallelization = true)]` because every Stryker subprocess writes into the shared `samples/StrykerOutput/` tree; parallel runs would race.

### Tests

**`SmokeAndErrorTests` (4 fast tests, ~5s total)**:
- `HelpFlag_ExitsZeroAndPrintsUsage` — `--help` returns 0 and emits "Usage: Stryker"
- `HelpFlag_ListsCoreOptions` — help text mentions `--reporter`, `--mutation-level`, `--solution`
- `NonExistentSolutionPath_ExitsNonZero` — explicit bad path short-circuits
- `UnknownReporter_ExitsNonZeroWithDiagnostic` — bad reporter name echoed back to user

**`SampleE2EProfileTests` (6 cached-slow tests, ~51s total via 2 cached runs)**:
- `Defaults_ProducesExpectedTotalAndScore` — Total=5, Killed=5, Survived=0, Score=100%
- `Defaults_ProducesParseableJsonReport` — JSON file exists, deserialises, has `schemaVersion`
- `Defaults_JsonReportLandsUnderStrykerOutputReportsDir` — `<run>/reports/` path discipline
- `Defaults_FileMapContainsCalculatorAndItsMutants` — Calculator.cs present with non-empty mutants, language="cs"
- `Defaults_EveryReportedMutantHasKnownStatus` — mutant.Status is one of the 8 mutation-testing-elements vocabulary strings
- `DefaultsWithJsonAndHtml_BothReportsLand` — multi-reporter validation (catches the clear-text vs cleartext class of bug)

## Process lessons

### 1. **`--mutation-profile` is not wired to the CLI surface (architectural gap, deferred to v2.9.0+)**

The Sprint 6 (ADR-018) MutationProfile property exists at `StrykerOptions.MutationProfile` and is validated by `MutationProfileInput`, but the CLI's McMaster command-line parser doesn't expose `--mutation-profile`, AND the `FileBasedInput` JSON config schema doesn't carry `mutation-profile` either. So today **the only way to set a non-Defaults profile is in-process via `new StrykerOptions { MutationProfile = ... }`** — which is fine for Sprint 18+19+20's unit/integration tests but invisible to E2E users.

This sprint discovered the gap during the cache-fixture run (`--mutation-profile All` returned exit 1 with "Unrecognized option"). The original Sprint 21 plan had per-profile cached runs (Defaults / Stronger / All); we replaced it with two Defaults-profile cached runs (json-only, json+html) and **deferred profile-comparison E2E tests to v2.9.0+** — once the CLI/config surface for MutationProfile lands.

### 2. **Stryker `--version` is the dashboard project-version flag, not a print-Stryker-version**

The McMaster help output declares `-v|--version <project-version>` as "Project version used in dashboard reporter". There's no print-Stryker-version equivalent on the CLI; the version banner is printed at startup of every actual run. Smoke-test plan dropped the `--version` test in favour of `--help`-based assertions (which DO succeed with exit 0).

### 3. **Stryker auto-discovers `samples/Sample.slnx` from the working directory**

A `MissingSolution_ExitsNonZero` test (no `--solution` argument, working dir = samples/) returned exit 0 because Stryker found the solution via discovery. Replaced with `NonExistentSolutionPath_ExitsNonZero` — passing an explicit, non-existent `--solution` path correctly fails fast (a more meaningful contract).

### 4. **`ProjectReference ReferenceOutputAssembly=false` is the right way to depend on a CLI binary**

Tests need the CLI binary on disk (for `dotnet exec`), but compile-time linking would create unwanted coupling. `ProjectReference Include="../../src/Stryker.CLI/Stryker.CLI.csproj" ReferenceOutputAssembly="false"` gives us the build-ordering guarantee without any compile-time `using Stryker.CLI;` possibility. Bonus: the test project compiles even if the CLI's public API churns.

### 5. **`[CollectionBehavior(DisableTestParallelization = true)]` is essential for shared-FS E2E**

Every Stryker subprocess writes into `samples/StrykerOutput/<timestamp>/`. With xUnit's default `ParallelizeTestCollections = true`, two classes' Stryker runs can race on Sample.Library bin/obj during compile. Assembly-wide `DisableTestParallelization = true` is the simplest fix; per-class `[Collection("Name")]` alone wouldn't work because xUnit runs *different* collections in parallel.

### 6. **`ICollectionFixture<T>` shares fixtures across classes; `IClassFixture<T>` doesn't**

The first cut used `IClassFixture<StrykerRunCacheFixture>` on each test class — but xUnit's `IClassFixture` instantiates per-class. Two cached-Stryker-run tests in different classes would have triggered two cold runs. Wrapped both fixtures in a shared `[CollectionDefinition("E2E-Sequential")]` + `ICollectionFixture<BuildFixture>, ICollectionFixture<StrykerRunCacheFixture>`, then `[Collection("E2E-Sequential")]` on every test class. Now every test in the assembly shares the same instances.

### 7. **Meziantou MA0048 forces one type per file (even nested DTOs)**

The minimal `MutationReport.cs` started as a single file with 4 nested DTO classes (`Thresholds`, `FileReport`, `MutantEntry`, `MutantTotals`). MA0048 rejected it ("File name must match type name"). Split into 5 files. The schema-record name `Thresholds` collided with `Stryker.Configuration.Options.Thresholds`, so renamed to `ReportThresholds` to keep the namespaces independent.

### 8. **Capture-during-WaitForExit needs `BeginOutputReadLine()` not `StandardOutput.ReadToEnd()`**

Reading both streams synchronously after `WaitForExit` deadlocks if either stream's pipe fills up before the process exits. The pattern that works: register `OutputDataReceived` / `ErrorDataReceived` event handlers (with `lock` around the `StringBuilder` because xUnit may run them on threadpool callbacks), call `BeginOutputReadLine()` / `BeginErrorReadLine()` before `WaitForExit`, then `WaitForExit()` (no overload) one final time after the timed wait to drain remaining buffered data.

## v2.8.0 progress map

```
[done]    Sprint 21 → Automated E2E in CI → v2.8.0   ⭐ MINOR ⭐
```

## Out of scope (deferred)

- **Profile-comparison E2E tests** — blocked by the `--mutation-profile` CLI/config gap. Deferred to v2.9.0+ as a follow-up sprint that first wires the missing flag.
- **CI workflow update** (`.github/workflows/*.yml` to run E2E as a separate job) — kept out of v2.8.0 to keep the sprint scoped to test-infrastructure-creation. Run manually via `dotnet test tests/Stryker.E2E.Tests/`.
- **Multi-target sample projects** — single Sample.slnx is sufficient for v2.8.0; richer scenarios deferred to v2.x.
- **Coverage report generation** — coverlet file-lock issue from Sprint 18 still open across all test projects.

## Recommended next sprint (v2.9.0 candidate)

**Sprint 22: `--mutation-profile` CLI surface + profile-comparison E2E**
- Add `--mutation-profile` to `StrykerCli` and `FileBasedInput`
- Re-enable per-profile cached fixture in `StrykerRunCacheFixture`
- Add the deferred Defaults vs Stronger vs All comparison tests
