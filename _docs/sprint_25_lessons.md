# Sprint 25 — VsTest UnitTest Project Setup + TestHelpers Expansion: Lessons Learned

**Sprint:** 25 (2026-05-01, autonomous run)
**Branch:** `feature/25-vstest-unittest-port`
**Base:** v2.11.0 (Sprint 24 closed)
**Final Tag:** `v2.12.0`
**Type:** Test-only release. Foundation expansion + project setup. **Scope-reduced** mid-sprint after recherche.

## Sprint Outcome

| Sub-Task | Result |
|---|---|
| Setup (Issue / Branch / state.md) | ✅ |
| Recherche cross-references + IProjectAnalysis surface | ✅ Discovered Buildalyzer.IAnalyzerResult removal + scope-explosion |
| TestHelpers Expansion: `TestHelper.cs` rewritten for `IProjectAnalysis` | ✅ |
| TestHelpers Expansion: `TestLoggerFactory.cs` ported | ✅ |
| `tests/Stryker.TestRunner.VsTest.Tests/` project + slnx-Eintrag | ✅ |
| `CoverageCaptureTests.cs` smoke port (1 test) | ✅ green |
| `VsTestMockingHelper.cs` port | ⏸ deferred — own Sprint 26 |
| `CoverageCollectorTests.cs` port | ⏸ deferred — Sprint 27 |
| `VsTextContextInformationTests.cs` port | ⏸ deferred — Sprint 28 |
| `VsTestRunnerPoolTests.cs` port | ⏸ deferred — Sprint 29 |
| Solution-wide tests (excl E2E) | ✅ **431 green** (388 Core + 17 Sample + 10 Architecture + 15 Solutions.Tests + 1 VsTest.Tests) |
| Semgrep | ✅ 0 findings on 3 changed source files |
| Tag | `v2.12.0` |

## Major Discovery (User-confirmed scope-reduction within sprint)

**Original Sprint 25 scope:** Port all 5 files of upstream
`Stryker.TestRunner.VsTest.UnitTest` (1726 LOC total) to
`tests/Stryker.TestRunner.VsTest.Tests/`.

**Discovery during recherche** (Sprint-15-Lesson live: pre-implementation
recherche surfaces showstoppers before they cost):

1. **All 5 files reference `using Stryker.Core.UnitTest;`** — that's the
   upstream-helper namespace which Sprint 24 only partially covered (4 of 6
   helpers) — `TestHelper` and `TestLoggerFactory` had to be added (this sprint).

2. **Sprint 1 Phase 9 replaced Buildalyzer entirely** with our native
   `Stryker.Abstractions.Analysis.IProjectAnalysis`. Upstream's
   `TestHelper.SetupProjectAnalyzerResult` returns `Mock<IAnalyzerResult>`;
   upstream's `VsTestMockingHelper` (574 LOC) builds its entire test infrastructure
   on `analyzerResultMock.Object` typed assignments. Adapting that is **architecture
   migration**, not "framework conversion MSTest→xUnit".

3. **VsTestMockingHelper.cs draft attempt produced ~22 analyzer errors** in the
   first build — most are mechanical CA/IDE/MA/CS style fixes (collection
   simplifications, nullability, ArgumentException-paramName, etc.), not
   semantic problems. But **22 fixes × 4 files = ~90 mechanical iterations**,
   which doesn't fit a single working session.

**Decision** (User-confirmed Option β + intra-sprint scope reduction): Sprint 25
ships the foundation + project setup + smoke (everything that's *clean*); each
remaining file becomes its own sprint.

## What landed

### `tests/Stryker.TestHelpers/` expansion (Sprint 24 foundation grows)

- **`TestHelper.cs` (architecture-adapted)**: upstream's `Mock<IAnalyzerResult>`
  with the `Properties`-dictionary-keyed surface re-shaped to
  `Mock<IProjectAnalysis>` with strongly-typed property setups (TargetDir /
  TargetFileName / Language / etc.). Method signature kept upstream-compatible
  (`SetupProjectAnalyzerResult(properties, projectFilePath, ...)`) so per-module
  test ports stay close to the upstream call site even though the internal
  routing differs.
- **`TestLoggerFactory.cs`**: Moq-backed `ILogger<T>` factory + `Mock<ILogger<T>>`
  shape — straightforward port (no API drift).

### `tests/Stryker.TestRunner.VsTest.Tests/`

- **`Stryker.TestRunner.VsTest.Tests.csproj`**: ProjectReference on
  `Stryker.TestHelpers` (Sprint 24 foundation), `Stryker.Core`,
  `Stryker.TestRunner.VsTest`, `Stryker.DataCollector`. PackageReferences
  `xunit + FluentAssertions + Moq + TestableIO.System.IO.Abstractions.TestingHelpers`.
- **`CoverageCaptureTests.cs`** (1 test, port of upstream's same-named file,
  25 LOC → 30 LOC after framework conversion). Smoke-test that validates the
  pipeline end-to-end.

## Process lessons

### 1. **Pre-implementation recherche > LOC-counting**

Sprint 24 estimated each module port by raw LOC. Sprint 25 recherche showed
upstream's `Stryker.Core.UnitTest` cross-references ARE the dependency graph,
not the upstream file size. `wc -l` lied; `grep "using Stryker.Core.UnitTest"`
told the truth.

### 2. **Sprint 1 "Workspaces.MSBuild port" is a foundational API break**

Buildalyzer's `IAnalyzerResult` is gone — replaced with our
`IProjectAnalysis`. Every upstream test that synthesizes a project-analysis
result needs the helper adapter approach (Sprint 25 supplies one) PLUS the
test itself needs `AnalyzerResult` → `Analysis` rename at every consumer
call site (e.g. `SourceProjectInfo.AnalyzerResult` → `.Analysis`). That's
several call sites per test method.

### 3. **The .NET 10 + C# 14 analyzer pile is LOUD on raw upstream code**

Roslynator + SonarAnalyzer + Meziantou with `TreatWarningsAsErrors` rejects
upstream code on dozens of style-rules (collection-init simplifications,
nullability, ArgumentException-paramName, etc.). Each upstream file produces
~20–25 errors on first build. None are semantic — but each is a manual fix.
Multiplied across 4 files this is genuinely a session-budget problem.

### 4. **Honest scope-reduction beats ambitious-scope-failure**

CLAUDE.md's "extrem lange dauert" budget the User authorized covers
multi-sprint timeline, NOT multi-day single sessions. Sprint 25 reduces
honestly: foundation done + smoke green + each remaining file individually
roadmapped. The user gets visible progress + reliable next-sprint scope.

## v2.12.0 progress map

```
[done]    Sprint 25 → VsTest UnitTest project setup + TestHelpers expansion → v2.12.0   ⭐ MINOR ⭐
[planned] Sprint 26 → VsTestMockingHelper full port (574 LOC, ~22 analyzer fixes) → v2.13.0
[planned] Sprint 27 → CoverageCollectorTests port (198 LOC) → v2.14.0
[planned] Sprint 28 → VsTextContextInformationTests port (202 LOC) → v2.15.0
[planned] Sprint 29 → VsTestRunnerPoolTests port (727 LOC) → v2.16.0
[planned] Sprint 30 → MTP.UnitTest port (10 files) → v2.17.0
[planned] Sprint 31 → CLI.UnitTest port (6 files + Spectre.Console) → v2.18.0
[planned] Sprint 32 → RegexMutators.UnitTest port (18 files) → v2.19.0
[planned] Sprint 33–37 → Stryker.Core.UnitTest in 5 tranches → v2.20.0–v2.24.0
```

## Out of scope (deferred — explicit Sprint 26+)

VsTestMockingHelper + 3 test files. Each becomes its own sprint per the v2.12.0
roadmap above.

## Recommended next sprint (v2.13.0 candidate)

**Sprint 26: VsTestMockingHelper full port**
- 574 LOC helper class (architecture migration AnalyzerResult→Analysis already
  done in TestHelper; this sprint applies the rename pattern to consumer call sites)
- Expected ~22 mechanical analyzer-fixes (collection-init, nullability,
  ArgumentException-paramName)
- ProjectReference: existing Sprint 25 csproj
- No tests run as part of this sprint (helper has no [Fact]s); validation is
  build-clean
- Sprint 27 first lights up tests once the helper is ready
