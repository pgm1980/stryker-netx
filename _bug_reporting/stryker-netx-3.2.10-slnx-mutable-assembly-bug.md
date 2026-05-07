# stryker-netx 3.2.10 — `.slnx` solution: project analysis succeeds but mutable-assembly resolution returns 0 projects

> **Project**: Aisess Platform (multi-tenant maturity-assessment platform)
> **Reporter**: pgm1980 / Aisess team
> **Date observed**: 2026-05-07
> **Tool affected**: `dotnet-stryker-netx` 3.2.10 (the C# 14 / .NET 10 compatible fork of `dotnet-stryker`)
> **Severity**: Blocking — mutation testing cannot run against a `.slnx`-based solution
> **Status**: Open — no workaround found short of falling back to legacy `.sln` solution format

---

## 1. Summary

When running `dotnet stryker-netx` against a multi-project solution that uses
the modern XML-based `.slnx` solution format (introduced in .NET 9 SDK,
default-recommended in .NET 10 SDK), the tool's project-analysis phase
**successfully discovers and parses every project in the dependency graph**
(test project + 4 source projects) yet ultimately reports

```
[INF] Could not find an assembly reference to a mutable assembly for project
      "…\tests\Aisess.Tests\Aisess.Tests.csproj". Will look into project
      references.
[DBG] Analyzing 0 projects.
[WRN] No project found, check settings and ensure project file is not corrupted.
```

and exits with `Failed to analyze project builds. Stryker cannot continue.`

The interesting bit is **`Analyzing 0 projects`** *after* the diagnostic log
shows Stryker has already analyzed the test project plus all four referenced
source projects in the preceding seconds. The analysis pipeline succeeds; the
subsequent **mutable-assembly resolution step** (which decides which of the
analyzed projects are eligible to mutate, based on assembly references from
the test project) yields an empty set.

This appears to be a regression / oversight specific to the `.slnx` solution
format and not to `.csproj` parsing in general — every individual `.csproj`
is analyzed cleanly with `Succeeded: True`.

---

## 2. Environment

| Component | Version |
|-----------|---------|
| OS | Windows 11 (24H2 build) |
| .NET SDK | 10.0.107 |
| Solution format | `.slnx` (XML-based, no legacy `.sln` exists alongside) |
| `dotnet-stryker-netx` | 3.2.10 (global tool, installed via `dotnet tool install -g dotnet-stryker-netx`) |
| Test project SDK | `Microsoft.NET.Sdk` (xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1) |
| Source project SDKs | `Microsoft.NET.Sdk` (3 of 4 layers) + `Microsoft.NET.Sdk.Web` (1 of 4 layers, the API project) |
| Target framework | `net10.0` (all projects, no multi-targeting) |
| `Directory.Build.props` properties | `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisMode=All`, `Nullable=enable`, `ImplicitUsings=enable`, `Deterministic=true`, `NuGetAudit=true`, `LangVersion=latest` |
| Code analyzers | Roslynator 4.15.0, SonarAnalyzer.CSharp 10.25.0.139117, Meziantou.Analyzer 3.0.50, Microsoft.VisualStudio.Threading.Analyzers 17.14.15, AsyncFixer 2.1.0 |

---

## 3. Repository structure

The project follows a 4-layer DDD-Onion architecture. All source projects
referenced by the tests are listed in the `.slnx` and live under `src/`:

```
aisess-platform.slnx          ← the solution file
src/
├── Aisess.Domain/            (Microsoft.NET.Sdk, no project refs)
├── Aisess.Application/       (Microsoft.NET.Sdk, refs Domain)
├── Aisess.Infrastructure/    (Microsoft.NET.Sdk, refs Domain)
└── Aisess.Api/               (Microsoft.NET.Sdk.Web, refs Application + Infrastructure)
tests/
└── Aisess.Tests/             (Microsoft.NET.Sdk, refs all 4 src projects)
benchmarks/
└── Aisess.Benchmarks/        (Microsoft.NET.Sdk Console)
tools/
└── Aisess.AppHost/           (Aspire.AppHost.Sdk/13.2.0)
```

### `aisess-platform.slnx`

```xml
<Solution>
  <Folder Name="/benchmarks/">
    <Project Path="benchmarks/Aisess.Benchmarks/Aisess.Benchmarks.csproj" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/Aisess.Api/Aisess.Api.csproj" />
    <Project Path="src/Aisess.Application/Aisess.Application.csproj" />
    <Project Path="src/Aisess.Domain/Aisess.Domain.csproj" />
    <Project Path="src/Aisess.Infrastructure/Aisess.Infrastructure.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Aisess.Tests/Aisess.Tests.csproj" />
  </Folder>
  <Folder Name="/tools/">
    <Project Path="tools/Aisess.AppHost/Aisess.AppHost.csproj" />
  </Folder>
</Solution>
```

### `tests/Aisess.Tests/Aisess.Tests.csproj` (excerpt)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <AssemblyName>Aisess.Tests</AssemblyName>
    <RootNamespace>Aisess.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AwesomeAssertions" Version="9.4.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FsCheck.Xunit" Version="3.3.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.11.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="TngTech.ArchUnitNET.xUnit" Version="0.13.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Aisess.Domain\Aisess.Domain.csproj" />
    <ProjectReference Include="..\..\src\Aisess.Application\Aisess.Application.csproj" />
    <ProjectReference Include="..\..\src\Aisess.Infrastructure\Aisess.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Aisess.Api\Aisess.Api.csproj" />
  </ItemGroup>
</Project>
```

### `tests/Aisess.Tests/stryker-config.json`

```json
{
  "$schema": "https://raw.githubusercontent.com/stryker-mutator/stryker-net/master/docs/stryker-config.json",
  "stryker-config": {
    "project": "Aisess.Tests.csproj",
    "solution": "../../aisess-platform.slnx",
    "test-projects": ["Aisess.Tests.csproj"],
    "mutation-profile": "Stronger",
    "mutation-level": "Complete",
    "coverage-analysis": "perTest",
    "concurrency": 4,
    "thresholds": { "high": 97, "low": 80, "break": 80 },
    "reporters": ["html", "progress", "cleartext", "json"],
    "ignore-mutations": ["string"],
    "ignore-methods": ["ToString", "GetHashCode", "*Logger.Log*", "*Console.WriteLine*"],
    "mutate": [
      "../../src/Aisess.Domain/**/*.cs",
      "../../src/Aisess.Application/**/*.cs",
      "../../src/Aisess.Infrastructure/**/*.cs",
      "!../../src/**/Program.cs",
      "!../../src/**/AssemblyMarker.cs",
      "!../../src/**/Class1.cs",
      "!../../src/**/Migrations/**/*.cs",
      "!../../src/**/*.Designer.cs"
    ],
    "language-version": "latest",
    "verbosity": "info"
  }
}
```

### Build sanity check

`dotnet build aisess-platform.slnx --configuration Debug` succeeds with
**0 warnings, 0 errors** under `TreatWarningsAsErrors=true` for all 7
projects, including the test project. `dotnet test` runs **17/17 tests
green** including 6 ArchUnitNET architecture tests that load the assemblies
under test from disk via `typeof(...).Assembly`. So:

- The `.slnx` is well-formed
- All `.csproj` files are well-formed
- All assemblies build correctly to their `bin\Debug\net10.0\` outputs
- Project references are correctly resolved by the standard MSBuild pipeline

The problem is local to Stryker's solution / project-reference handling.

---

## 4. Reproduction commands

### 4.1 Default invocation (config-file mode)

```bash
cd tests/Aisess.Tests
dotnet stryker-netx --config-file stryker-config.json
```

**Result:** fails with `Failed to analyze project builds. Stryker cannot continue.`

### 4.2 Explicit `--project` (target Infrastructure layer directly)

```bash
cd tests/Aisess.Tests
dotnet stryker-netx \
    --project ../../src/Aisess.Infrastructure/Aisess.Infrastructure.csproj \
    --mutation-profile Defaults \
    --reporter "progress" \
    --reporter "cleartext"
```

**Result:** identical failure.

### 4.3 Explicit `--solution` pointing at the `.slnx`

```bash
cd tests/Aisess.Tests
dotnet stryker-netx \
    --solution ../../aisess-platform.slnx \
    --project ../../src/Aisess.Infrastructure/Aisess.Infrastructure.csproj
```

**Result:** identical failure. So both auto-discovery from `stryker-config.json`
and explicit `--solution` produce the same outcome.

### 4.4 Diagnostic mode

```bash
cd tests/Aisess.Tests
dotnet stryker-netx --config-file stryker-config.json --diag
```

**Result:** identical failure but writes a 155 KB log to
`StrykerOutput/<timestamp>/logs/log-YYYYMMDD.txt`. Annotated extracts of
that log are reproduced in §6 below.

---

## 5. Expected vs. actual behavior

**Expected:** Stryker discovers the four source projects referenced from
`Aisess.Tests.csproj` (Domain, Application, Infrastructure, Api), filters
out non-mutable assemblies (test SDK / xUnit / NuGet packages), retains the
four Aisess source projects as mutation candidates, and proceeds to mutate
every `.cs` file matching the `mutate` glob list.

**Actual:** Stryker's analysis pipeline succeeds for *every* discovered
project — see the `Succeeded: True` markers in the diagnostic log — but the
subsequent step that resolves "mutable assembly references" against the test
project reports `Analyzing 0 projects` and exits with the
`No project found, check settings and ensure project file is not corrupted`
warning even though the project file is well-formed and the `dotnet build`
of the same solution is green.

---

## 6. Annotated diagnostic log extracts

The `--diag` log is 155 KB so only the structurally significant lines are
quoted here. The log was produced from a clean run (`StrykerOutput`
directory deleted before invocation). All five projects in the dependency
graph are analyzed in sequence and each individual analysis succeeds — the
filtering happens *after* analysis.

```text
[INF] Analysis starting.
[DBG] Using "...\tests\Aisess.Tests\Aisess.Tests.csproj" as test project
[INF] Analyzing 1 test project(s).
[DBG] Analyzing "Aisess.Tests.csproj"
[VRB] **** Project analysis result ****
      Project:        ...\tests\Aisess.Tests\Aisess.Tests.csproj
      TargetFramework: net10.0
      Succeeded:      True
      Property        Configuration=Debug
      Property        Platform=AnyCPU
      Property        AssemblyName=Aisess.Tests
      Property        Configurations=Debug;Release
      SourceFile      ...\tests\Aisess.Tests\EnvironmentVariableSecretProviderTests.cs
      SourceFile      ...\tests\Aisess.Tests\LayerArchitectureTests.cs
      SourceFile      ...\tests\Aisess.Tests\ManifestRoundtripPropertyTests.cs
      SourceFile      ...\tests\Aisess.Tests\SmokeTests.cs
      SourceFile      ...\tests\Aisess.Tests\UnitTest1.cs
      SourceFile      ...\Microsoft.NET.Test.Sdk.Program.cs
      SourceFile      ...\obj\Debug\net10.0\Aisess.Tests.GlobalUsings.g.cs
      ... (~360 NuGet + framework references) ...
      References:     Aisess.Application.dll (in ...\src\Aisess.Application\bin\Debug\net10.0)
      References:     Aisess.Infrastructure.dll (in ...\src\Aisess.Infrastructure\bin\Debug\net10.0)
      References:     Aisess.Domain.dll (in ...\src\Aisess.Domain\bin\Debug\net10.0)
      References:     Aisess.Api.dll (in ...\src\Aisess.Api\bin\Debug\net10.0)
      **** End project analysis result ****

[DBG] Analyzing "..\..\src\Aisess.Domain\Aisess.Domain.csproj"
[VRB] **** Project analysis result ****
      Project:        ...\src\Aisess.Domain\Aisess.Domain.csproj
      TargetFramework: net10.0
      Succeeded:      True
      Property        AssemblyName=Aisess.Domain
      SourceFile      ...\src\Aisess.Domain\AssemblyMarker.cs
      SourceFile      ...\src\Aisess.Domain\Configuration\IVaultSecretProvider.cs
      ... (framework refs) ...
      **** End project analysis result ****

[DBG] Analyzing "..\..\src\Aisess.Application\Aisess.Application.csproj"
      Succeeded:      True
      Property        AssemblyName=Aisess.Application
      References:     Aisess.Domain.dll (in ...\src\Aisess.Domain\bin\Debug\net10.0)
      ... (framework refs) ...

[DBG] Analyzing "..\..\src\Aisess.Infrastructure\Aisess.Infrastructure.csproj"
      Succeeded:      True
      Property        AssemblyName=Aisess.Infrastructure
      References:     Aisess.Domain.dll (in ...\src\Aisess.Domain\bin\Debug\net10.0)
      ... (framework refs) ...

[DBG] Analyzing "..\..\src\Aisess.Api\Aisess.Api.csproj"
      Succeeded:      True
      Property        AssemblyName=Aisess.Api
      References:     Aisess.Application.dll, Aisess.Infrastructure.dll, Aisess.Domain.dll
      ... (ASP.NET Core framework refs) ...

[INF] Could not find an assembly reference to a mutable assembly
      for project "...\tests\Aisess.Tests\Aisess.Tests.csproj".
      Will look into project references.
[DBG] Analyzing 0 projects.
[WRN] No project found, check settings and ensure project file is not corrupted.
[INF] Analysis complete.
[INF] Time Elapsed 00:00:06.6473144
[ERR] Failed to analyze project builds. Stryker cannot continue.
```

### Key observations from the log

1. **Project analysis succeeds for all 5 projects.** Each project's Buildalyzer
   pass returns `Succeeded: True` with full property + source-file +
   reference enumeration.

2. **The test project's reference list contains all four Aisess source DLLs**
   resolved to their on-disk `bin\Debug\net10.0\Aisess.{Domain,Application,
   Infrastructure,Api}.dll` paths. So the project does have references to
   what should be mutable assemblies.

3. **Despite (1) and (2), Stryker concludes "Could not find an assembly
   reference to a mutable assembly"** and then "Will look into project
   references" — but the very next line is `Analyzing 0 projects`.

4. **`Analyzing 0 projects` is the smoking gun.** The fall-back path that
   should iterate `<ProjectReference>` items from the test `.csproj`
   apparently retrieves an empty list, even though the test `.csproj`
   contains four such items and the standard MSBuild pipeline (used by
   `dotnet build` and `dotnet test`) resolves them correctly.

---

## 7. Hypothesis

We suspect Stryker's mutable-assembly resolution heuristic relies on
project-reference graph data that is normally surfaced by Buildalyzer when
the *solution* file is parsed. With the legacy `.sln` format, Buildalyzer
populates the `ProjectInSolution` collection that allows Stryker to map
output-DLL paths back to source projects. With the modern `.slnx` format
the same data flow may not be wired, so:

- `Buildalyzer.AnalyzerManager(solutionPath)` over a `.slnx` may either
  return an empty solution model or a model without the project-to-output
  mapping needed for the mutable-assembly filter.
- The per-project `IAnalyzerResult` objects *are* obtained correctly (since
  every project shows up in the diagnostic log with `Succeeded: True`), so
  the issue is specifically in the **solution-level project-graph
  resolution** path, not in the per-project parsing path.
- The DLL-reference list of the test project does name all four mutable
  assemblies by file name, but the path-based reverse lookup that should
  match each DLL back to its source project is not finding matches — most
  likely because the candidate-project list ("the projects in the solution")
  is empty, hence the `Analyzing 0 projects` message.

This hypothesis is consistent with `--solution …\.slnx` producing the same
result as the auto-discovered case: the path is parsed, but the resulting
project-collection is empty downstream.

---

## 8. Workarounds tried — and their results

| # | Approach | Outcome |
|---|----------|---------|
| 1 | `dotnet stryker-netx --config-file stryker-config.json` (default) | ❌ `Failed to analyze project builds` |
| 2 | Run from solution root instead of test-project dir | ❌ same error |
| 3 | `--project ../../src/Aisess.Infrastructure/...csproj` (target a single mutable project explicitly) | ❌ same error |
| 4 | `--solution ../../aisess-platform.slnx` (explicit) | ❌ same error |
| 5 | `--mutation-profile Defaults` instead of `Stronger` (rule out profile-related logic) | ❌ same error (failure is upstream of mutator selection) |
| 6 | `--diag` flag to capture full Buildalyzer trace | ✅ produces logs (referenced in §6); confirms hypothesis |

A **`.slnx` → `.sln` regeneration** workaround was *not* attempted because
.NET 10 SDK only ships `dotnet sln migrate` in the *forward* direction
(`.sln` → `.slnx`); there is no built-in reverse migration. Hand-rolling a
legacy `.sln` to test the hypothesis is feasible but invasive for a project
that has standardized on `.slnx`.

---

## 9. Suggested investigation areas for the maintainers

1. **Buildalyzer `.slnx` support boundary.** Verify that
   `AnalyzerManager` over a `.slnx` populates the same project-graph data
   structures that the `.sln` code path provides. If Buildalyzer's `.slnx`
   support is partial (e.g., it parses the file but doesn't expose
   `ProjectInSolution`), Stryker's downstream filter would silently see an
   empty project list — exactly what `Analyzing 0 projects` indicates.

2. **Mutable-assembly resolution code path.** The `[INF] Could not find an
   assembly reference to a mutable assembly … Will look into project
   references.` message implies a two-stage resolver:
    - Stage 1: match test-project DLL references against an in-memory
      list of "buildable" projects from the solution.
    - Stage 2: if Stage 1 fails, fall back to walking `<ProjectReference>`
      items from the test `.csproj`.
   With `.slnx` it appears Stage 1 returns nothing (empty solution
   project-list), and Stage 2 also reports `Analyzing 0 projects`. Stage 2
   should ideally not depend on the same project-graph model as Stage 1; it
   should be able to read `<ProjectReference>` items directly from the
   already-analyzed test-project `IAnalyzerResult` (Stryker has those —
   they show up in the diag log).

3. **Telemetry on `--solution`.** When the user passes `--solution
   path/to/file.slnx`, Stryker could log "Loaded N projects from solution"
   immediately after parsing, which would have made this issue
   self-diagnostic. The current log silently drops past the solution-load
   phase and only emits `Analyzing 1 test project(s)` for the test project,
   leaving users to guess whether the source projects were loaded at all.

4. **CI breakage in mixed-tooling environments.** Many .NET 10 projects
   are migrating to `.slnx` because `dotnet sln migrate` defaults to it
   and `Microsoft.NET.Sdk.Web` templates produce it. A regression that
   makes Stryker silently incompatible with the new default solution
   format is a fairly broad CI-impact issue.

---

## 10. Minimal repro

A minimal repro can be built in <5 minutes:

```bash
mkdir stryker-slnx-repro && cd stryker-slnx-repro

dotnet new sln --format slnx -n repro          # creates repro.slnx
dotnet new classlib -n LibA -f net10.0
dotnet new classlib -n LibB -f net10.0
dotnet new xunit    -n LibA.Tests -f net10.0

dotnet add LibB/LibB.csproj reference LibA/LibA.csproj
dotnet add LibA.Tests/LibA.Tests.csproj reference LibA/LibA.csproj LibB/LibB.csproj
dotnet add LibA.Tests/LibA.Tests.csproj package coverlet.collector

dotnet sln repro.slnx add LibA/LibA.csproj LibB/LibB.csproj LibA.Tests/LibA.Tests.csproj

# Add a trivial mutable method to LibA/Class1.cs and a unit test that asserts it.
# Then:
dotnet build repro.slnx
dotnet test  repro.slnx        # expect green

cd LibA.Tests
dotnet stryker-netx \
    --project ../LibA/LibA.csproj \
    --solution ../repro.slnx \
    --reporter cleartext
```

Expected: mutation report with at least one killed mutant.
Observed:  same `Analyzing 0 projects` / `No project found` failure.

---

## 11. Notes for downstream users

While this issue is open, projects that need to run stryker-netx against a
.NET 10 SDK solution should keep a legacy `.sln` solution file alongside
`.slnx` (since `dotnet sln migrate` is one-way, that means manually
maintaining the `.sln`), and point Stryker explicitly at the `.sln` via
`--solution`. This is the workaround we will deploy in our CI in Sprint 2
if the upstream fix is not available by then.

---

## 12. Reproducing the diagnostic log used in this report

```bash
cd <project-root>/tests/Aisess.Tests
rm -rf StrykerOutput/
dotnet stryker-netx --config-file stryker-config.json --diag
ls StrykerOutput/*/logs/log-*.txt   # full 155 KB Buildalyzer trace
```

The full log we observed is preserved at
`StrykerOutput/2026-05-07.14-00-46/logs/log-20260507.txt` in the Aisess
repository (private, but available on request to the Stryker maintainers).

---

## 13. Contact / cross-references

- Aisess platform CLAUDE.md mandates stryker-netx 3.0.24+ as the
  .NET 10–compatible mutation-testing tool of record.
- Sprint 1 wrap-up commit: `760516b chore(sprint-1): wrap-up — quality
  gates, CI hotfix, sprint review`.
- Sprint 1 PR: https://github.com/pgm1980/aisess-platform/pull/142
- Issue documented in Aisess `MEMORY.md` "Bekannte offene Punkte" #1.
