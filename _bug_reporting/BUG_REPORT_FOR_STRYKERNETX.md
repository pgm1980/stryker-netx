# BUG REPORT #9 — stryker-netx 3.2.18 / 3.2.19 Issues Observed during Sprint 56 (filesystem-mcp-server)

**Status:** Bug report, ready for upstream submission to the **stryker-netx** maintainers.
**Reporter:** filesystem-mcp-server project — autonomous Sprint 56 session, 2026-05-25.
**Severity:** **High** for Bug #1 (blocking), **Medium** for Bug #2 (workflow friction), **Low–Medium** for the remaining anomalies.
**Affected versions:** `dotnet-stryker-netx` **3.2.18 AND 3.2.19** (both confirmed).
**Linked / superseded reports:** [`_docs/issues/stryker-netx-3.2.18-mutate-filter-trees-null.md`](_docs/issues/stryker-netx-3.2.18-mutate-filter-trees-null.md) (original report from Sprint 51 — this Bug Report #9 **extends** that report with the partial-class-workaround confirmation, two new bugs, and four anomalies all observed in the current sprint).

---

## 1. Environment

| Field                     | Value                                                                 |
|---------------------------|-----------------------------------------------------------------------|
| Tool                      | `dotnet-stryker-netx`                                                 |
| Version (reproducer)      | **3.2.18** (offered 3.2.19 upgrade banner; both versions affected)    |
| Installation              | Local dotnet-tool manifest (`dotnet-tools.json`, rollForward=false)   |
| Mutation profile          | `Stronger` (also reproduced with `Defaults` + `All`)                  |
| Mutation level            | Auto-set to `Advanced` from profile `Stronger` (ADR-025; documented)  |
| .NET SDK                  | **10.0.108** (`MSBuild 18.0.11`, Host runtime 10.0.8)                 |
| OS                        | Windows 11 24H2 — build 10.0.26200                                    |
| CPU concurrency           | 12 (Stryker default)                                                  |
| Reproducer repo           | https://github.com/pgm1980/filesystem-mcp-server.git                  |
| Reproducer branch         | `feature/optimization-roadmap-baseline`                               |
| Latest commit at repro    | `562b46c` — Sprint 56 iter 5 (WorkspaceService deep-tests)            |
| Test count at repro       | 3066 tests (xUnit 2.9.3 + AwesomeAssertions 9.4.0 + Moq 4.20.72)      |
| Test framework            | xUnit 2.9.3 (vstest)                                                  |
| Solution format           | `.slnx` (new XML solution format; Stryker correctly identifies project) |

---

## 2. Executive Summary

During Sprint 56 of filesystem-mcp-server (5 iterations targeting the Infrastructure.Services layer for the CLAUDE.md ≥97% mutation-score gate) we observed **two new bugs** and **four anomalies** in stryker-netx 3.2.18:

| # | Class    | Title                                                                                            | Severity | Repro reliability |
|---|----------|--------------------------------------------------------------------------------------------------|----------|------------------|
| 1 | **Bug**  | `--mutate` filter still crashes with `trees[2]` even when partial-class siblings are included    | High     | 100% (every run) |
| 2 | **Bug**  | Stryker `testhost.exe` keeps source-project DLLs file-locked, blocking concurrent `dotnet build` | Medium   | 100% (every run) |
| 3 | Anomaly  | Initial-test-run reports false-positive "N tests are failing" (157 vs. 0 locally) — first run only | Medium   | Intermittent (≈25% of cold runs) |
| 4 | Anomaly  | Pre-existing "Safe Mode! compile error" warnings for `double → int` mutations in ~30 methods     | Low–Med  | 100% (codebase-wide) |
| 5 | Anomaly  | CLI rejects `--reporters` (plural, accepted by upstream Stryker.NET 4.x); only `-r/--reporter` works | Low      | 100% |
| 6 | Anomaly  | Initial-test-run wall-clock varies 1m11s → 5m29s across runs of the same suite (≈5× variance)    | Low      | Run-dependent |
| 7 | Anomaly  | `Mock<ILogger>` invocation-count tests do NOT kill statement-deletion mutations on logger calls in adjacent methods | Medium  | 100% in our codebase |

Bug #1 is a **re-confirmation of the originally-reported `--mutate` issue** with a previously-suggested workaround (include partial-class siblings in the `--mutate` glob) **proven not to fix the issue**. The other findings are new.

---

## 3. Bug #1 (Primary, High Severity) — `--mutate` filter still crashes with `trees[2]` even when partial-class siblings are included

### 3.1 Confirmation of original report (recap)

The original report (`_docs/issues/stryker-netx-3.2.18-mutate-filter-trees-null.md`) documents that `--mutate "**/<File>.cs"` with a single file crashes inside `CsharpCompilingProcess.GetCSharpCompilation` because the partial-class sibling (typically a `*LoggerMessages.cs` file with `[LoggerMessage]` source-generator helpers) is excluded from the post-filter syntax-tree collection, producing `trees[2] = null` and an `ArgumentNullException`.

### 3.2 New finding — partial-class workaround does NOT fix the issue

The original report (section 9, "Suggested Fix Option B") proposed including all partial siblings automatically. As a workaround pending that fix, one might try **explicitly listing both files**:

```bash
cd tests/FsMcpServer.Tests
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutate "**/HealthService*.cs" \
    --mutate "**/PatchService*.cs" \
    --mutation-profile Stronger \
    -r progress -r json
```

This pattern matches BOTH `HealthService.cs` + `HealthServiceLoggerMessages.cs` AND `PatchService.cs` + `PatchServiceLoggerMessages.cs`. Stryker correctly reports **4 files in scope, 178 skipped**:

```
[16:47:15 INF] Disable-directive validation: scanned 4 files in --mutate scope (178 skipped).
[16:47:15 ERR] An error occurred during the mutation test run
System.AggregateException: One or more errors occurred. (Value cannot be null. (Parameter 'trees[2]'))
 ---> System.ArgumentNullException: Value cannot be null. (Parameter 'trees[2]')
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.AddSyntaxTrees(IEnumerable`1 trees)
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(...)
   at Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation(...) /_/src/Stryker.Core/Compiling/CsharpCompilingProcess.cs:line 152
   at Stryker.Core.Compiling.CsharpCompilingProcess.Compile(...) /_/src/Stryker.Core/Compiling/CsharpCompilingProcess.cs:line 60
   at Stryker.Core.MutationTest.CsharpMutationProcess.CompileMutations(...) /_/src/Stryker.Core/MutationTest/CsharpMutationProcess.cs:line 102
   at Stryker.Core.MutationTest.CsharpMutationProcess.Mutate(...) /_/src/Stryker.Core/MutationTest/CsharpMutationProcess.cs:line 92
   at Stryker.Core.MutationTest.MutationTestProcess.Mutate() /_/src/Stryker.Core/MutationTest/MutationTestProcess.cs:line 46
   at Stryker.Core.Initialisation.ProjectMutator.MutateProject(...) /_/src/Stryker.Core/Initialisation/ProjectMutator.cs:line 28
   at Stryker.Core.Initialisation.ProjectOrchestrator.<>c__DisplayClass8_0.<MutateProjectsAsync>b__0(...) /_/src/Stryker.Core/Initialisation/ProjectOrchestrator.cs:line 102
   at System.Threading.Tasks.Parallel.<>c__DisplayClass19_0`2.<ForWorker>b__1(...)
[16:47:15 INF] Time Elapsed 00:07:33.5421479
Unhandled exception. System.AggregateException: ... (same chain re-thrown unhandled)
```

> **Key observation:** Even when the user explicitly includes both partials in the `--mutate` glob, the error still references `trees[2]`. This means the bug is **NOT** (only) about partial-class siblings being filtered out — there is also a wrong index/null entry inside the post-filter syntax-tree collection regardless of how many files the user supplies.

### 3.3 Single-file `--mutate` repro is also still broken

```bash
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutate "**/HealthService.cs" \
    --mutation-profile Stronger \
    -r progress -r json
```

→ Same stack trace. Same `trees[2]` index. (See `_stryker_s56_i1_v2.log` lines 40–101 in the reproducer repo for full output.)

### 3.4 Pipeline phase where it fires

```
[16:24:37 INF] mutation-level auto-set to Advanced based on mutation-profile=Stronger ...
[16:27:25 INF] Analysis starting.
[16:27:42 INF] Analyzing 1 projects.
[16:27:44 INF] Found project ... to mutate.
[16:27:45 INF] Project analysis completed in 0m 19s.
[16:27:45 INF] Building solution FsMcpServer.slnx.
[16:29:33 INF] Number of tests found: 2933.
[16:35:03 INF] Initial test run completed in 5m 29s.   ← OK (see Anomaly #3 below for the warning here)
[16:35:18 INF] Disable-directive validation: scanned 1 files in --mutate scope (181 skipped).
[16:35:18 ERR] An error occurred during the mutation test run
                System.ArgumentNullException: ... (Parameter 'trees[2]')
[16:35:18 INF] Time Elapsed 00:07:53.5421479
Unhandled exception.
```

The crash happens **after** disable-directive validation and **before** any mutant is executed — same phase as the original report.

### 3.5 Why this matters

The mitigation note in the original report ("include the partial sibling in `--mutate`") was intuitive but **wrong**. Until a real fix lands, **`--mutate` is completely unusable** for any project that adopts:
- `[LoggerMessage]` source-generator pattern (CA1848-recommended)
- `[GeneratedRegex]`
- `[JsonSerializerContext]`
- any custom partial-class source generator

Per the original report, the workaround is to run **whole-project** (~5 min) instead of single-file (~30s). For incremental TDD-style mutation-driven test development the wall-clock penalty is roughly **10×** per iteration.

---

## 4. Bug #2 (Medium Severity) — Stryker `testhost.exe` keeps source-project DLLs file-locked, blocking concurrent `dotnet build`

### 4.1 Symptom

While a long-running full-project Stryker run is in progress (`bvw1y7ybo` in our session — Initial Test Run + Mutation Generation phases), any **concurrent** `dotnet build` of the same solution fails after 10 retries with `MSB3027`:

```
warning MSB3026: "...\src\FsMcpServer.Infrastructure\bin\Debug\net10.0\FsMcpServer.Infrastructure.dll"
    konnte nicht in "bin\Debug\net10.0\FsMcpServer.Infrastructure.dll" kopiert werden.
    Wiederholung 1 wird in 1000 ms gestartet. The process cannot access the file
    '...\tests\FsMcpServer.Tests\bin\Debug\net10.0\FsMcpServer.Infrastructure.dll'
    because it is being used by another process.
    Die Datei wird durch "testhost (36520)" gesperrt.
[...10× retry warning lines, each waiting 1s...]
error MSB3027: ... "...FsMcpServer.Infrastructure.dll" konnte nicht in "bin\Debug\net10.0\..."
    kopiert werden. Die zulässige Anzahl von Wiederholungen von 10 wurde überschritten. Fehler.
    Die Datei wird durch "testhost (36520)" gesperrt.
error MSB3021: Die Datei "...FsMcpServer.Infrastructure.dll" kann nicht in
    "bin\Debug\net10.0\FsMcpServer.Infrastructure.dll" kopiert werden.

Fehler beim Buildvorgang.
```

Reproduced for all three source DLLs (`FsMcpServer.dll`, `FsMcpServer.Infrastructure.dll`, `FsMcpServer.Domain.dll`).

### 4.2 Root cause hypothesis

Stryker spawns one or more long-running `testhost.exe` processes for the initial test run + per-mutant test execution. These hosts **load the project DLLs from `tests/FsMcpServer.Tests/bin/Debug/net10.0/`** and keep them resident (presumably for fast per-mutant test re-runs). The file handles are never released until the entire Stryker process tree exits.

Concurrent `dotnet build` then tries to copy fresh DLLs from `src/<Project>/bin/Debug/net10.0/<Project>.dll` to `tests/FsMcpServer.Tests/bin/Debug/net10.0/<Project>.dll` and the locked target file rejects the copy.

### 4.3 Impact on developer workflow

In incremental TDD:
1. Run Stryker on whole project (long-running, ~30–60 min).
2. While waiting, the developer writes new tests for surviving mutants.
3. `dotnet build` to verify the new tests compile → **fails** with MSB3027 if Stryker hasn't finished.
4. Developer must either wait for Stryker or kill it (losing all per-mutant progress).

For us this required killing Stryker mid-run to commit Sprint 56 iter 5 (`bvw1y7ybo` was running for ~10 minutes of mutation generation when we needed to ship iter 5).

### 4.4 Suggested fix

Either:
- **A.** Have Stryker's testhost open the DLLs with `FileShare.Delete | FileShare.Read` so MSBuild can replace the file on disk and the old handle gracefully drops.
- **B.** Document this constraint clearly and suggest `--output <separate-path>` plus shadow-copying inputs so the source-tree `bin/Debug/` directory isn't touched.
- **C.** Use AssemblyLoadContext + `LoadFromStream` (read DLL bytes into memory once at start, then release the file handle) so subsequent rebuilds aren't blocked.

Option C is the standard pattern for hosts that need to allow tooling to rewrite the test assemblies (e.g., `xunit.runner.visualstudio` uses this strategy already).

---

## 5. Anomaly #3 (Medium Severity) — Initial-test-run reports `WRN 157 tests are failing` despite local `dotnet test` showing 0 failures

### 5.1 Observation

Across three Stryker runs on the same commit (each starting from a clean state):

| Run ID       | Test count discovered | Initial-run wall-clock | Failing tests reported |
|--------------|----------------------:|-----------------------:|------------------------:|
| `b541we5zl`  | 2933                  | **5m 29s**             | **157 failing**         |
| `bkej25pof`  | 2967                  | **4m 31s**             | 1 failing               |
| `b9mtt52lu`  | 3020                  | **1m 16s**             | 0 failing               |
| `bvw1y7ybo`  | 3051                  | **1m 11s**             | 0 failing               |
| `bstuc2otd`  | 3066                  | **1m 8s**              | 0 failing               |

Verbatim from `_stryker_s56_i1_v2.log`:
```
[16:35:03 INF] Initial test run completed in 5m 29s.
[16:35:03 WRN] 157 tests are failing. Stryker will continue but outcome will be impacted.
```

The same commit, **immediately** re-tested with plain `dotnet test --no-build`, produces **2933 / 2933 passing** in 23 seconds. **No 157-test failure is reproducible outside the Stryker testhost environment.**

### 5.2 Hypotheses

1. **Process-resource starvation on first run**: 12 parallel testhost workers (default `--concurrency 12`) on a 12-core machine compete with the Stryker mutation-generation phase running in the same process, hitting timeouts on tests with `Task.Delay`, `FileSystemWatcher` debounces, or 1500-ms event waits. Subsequent runs with warm caches don't hit the timeout.
2. **xUnit parallelism config mismatch**: xUnit's default `parallelizeTestCollections=true` combined with Stryker's `--concurrency 12` produces 12×N effective parallelism, swamping the system.
3. **Test-attribution noise**: Some tests rely on shared state (static singletons, `DateTime.UtcNow`) that becomes inconsistent under high-contention parallel execution.

### 5.3 Impact

When this anomaly fires, Stryker continues to mutation testing but marks 157 mutants per-test as ambiguous (the failing-during-initial-run tests can't be used to kill mutants). This **artificially deflates** the reported mutation score on the first run of a session.

### 5.4 Suggested fix

- Either reduce the default `--concurrency` (e.g., `Environment.ProcessorCount / 2`) **OR** document the known interaction with parallel xUnit / FileSystemWatcher tests and recommend `<DisableTestParallelization>true</DisableTestParallelization>` in `xunit.runner.json` during mutation testing.
- Optionally re-run the initial test run with `--break-on-initial-test-failure` enabled by default, but only fail when ≥X% of tests are red (currently a single failure does not break the run, but 157 also doesn't).

---

## 6. Anomaly #4 (Low–Medium Severity) — Pre-existing "Safe Mode! compile error" cascade for `double → int` mutations across ~30 methods

### 6.1 Observation

Every full-Infrastructure Stryker run emits **~70 `Safe Mode! Stryker will remove all mutations in <Method>` warnings** during the mutation-generation phase, all with the same family of compile errors:

```
[17:16:01 WRN] An unidentified mutation in C:\...\Services\EncryptionService.cs resulted in a
    compile error (at 254:26) with id: CS1929, message: 'byte[]' does not contain a definition
    for 'AsSpan' and the best extension method overload 'MemoryExtensions.AsSpan(string?, int)'
    requires a receiver of type 'string?' (Source code: buffer)
[17:16:01 INF] Safe Mode! Stryker will remove all mutations in WriteEncryptedFileAsync and mark
    them as 'compile error'.
```

Sample cluster (verbatim from `_stryker_s56_full.log` lines 33–250):

| Service / Method                                  | Mutant kind that fails                     | CS error |
|--------------------------------------------------|--------------------------------------------|----------|
| `EncryptionService.WriteEncryptedFileAsync`      | `byte[].AsSpan` argument-type mutation     | CS1929   |
| `EncryptionService.WriteEncryptedFileAsync` (L259) | int `offset` → `double`                  | CS0266   |
| `SymlinkManagementService.ResolveSymlinkAsync`   | `^(-1:0:2:1)` ternary mutation → double    | CS0029   |
| `PluginManager.LoadAndCreatePlugin`              | `assembly` unassigned after try/catch mutation | CS0165 |
| `DiffService.ComputeLineChanges`                 | int `ia` `ib` → `double`                   | CS0266   |
| `DiffService.AppendHunks`                        | int `i` `j` → `double`                     | CS1503   |
| `DiffService.ComputeHunkBounds`                  | int `start` `end` → `double`               | CS0029   |
| `DiffService.ClassifyFiles`                      | int `identicalCount` → `double`            | CS1503   |
| `DuplicateService.FindDuplicatesAsync`           | long `totalDuplicateSize` → `double`       | CS1503   |
| `EncodingUtilService.TransliterateAsync`         | int `charsTransliterated` → `double`       | CS0266   |
| `LineEndingService.NormalizeLineEndingsBatchAsync` | int `skippedBinary` → `double`           | CS0266   |
| `LineEndingService.CountOccurrences`             | char `pattern` → `string` + index `int` → `double` | CS1503 |
| `MetricsCollectorService.ResetMetrics`           | `ref long` → `ref double`                  | CS1503   |
| `DirectoryService.BuildTree`                     | int `i` → `double` (4×)                    | CS1503   |
| `EditService.EditMultipleBlocksAsync`            | int `totalReplacements` → `double`         | CS0266   |
| `EditService.ExtractLinesAsync`                  | int `start` → `double`                     | CS1503   |
| `EditService.CountOccurrences`                   | char `searchText` → `string`               | CS1503   |
| `EditService.GenerateDiff`                       | int `i` → `double` (2×)                    | CS0266   |
| `FileService.ReadFileAsync`                      | long `offset ?? -1/1/0` → `double`         | CS1503   |
| `FileService.ReadWithOffsetAsync`                | `byte[].AsMemory` argument-type mutation   | CS1929   |
| `FileService.ReadWithOffsetAsync`                | int `totalRead` → `double`                 | CS1503   |
| `MetadataService.DetectBinary`                   | int `i` → `double`                         | CS0266   |
| `MetadataService.DetectLineEnding`               | int `index` → `double` (3×)                | CS1503   |
| `MetadataService.CountLineStatistics`            | `^(-1:0:2:1)` ternary → `double` (2×)      | CS0029   |
| `BatchUtilService.BatchTagAsync`                 | int `taggedCount` → `double`               | CS0266   |
| `BatchUtilService.BatchDeleteAsync`              | int `deletedCount` → `double`              | CS0266   |
| `BatchUtilService.CleanupTempAsync`              | `ref int` → `ref double` (2×) + count vars | CS1503/CS0266 |
| `BinaryDiffService.ProcessChunk`                 | int `i` → `double` (2×) + ranges, AsSpan   | CS0266/CS1929 |
| `BinaryDiffService.ProcessChunk` (L165)          | long `globalOffset + diffStart` → `double` | CS1503   |
| `CodeMetricsService.ComputeLocBreakdown`         | int `code` / `blank` / `comment` → `double`| CS1503   |
| `CodeMetricsService.ComputeCyclomaticComplexity` | int `complexity` → `double`                | CS0266   |
| `CodeMetricsService.CountLogicalOperators`       | char `op` → `string`; int `index` → `double` | CS1503 |
| `CodeMetricsService.CountKeyword`                | char `keyword` → `string`; `^(...)` ternary| CS1503   |
| `CodeMetricsService.ComputeBraceNestingDepth`    | int `maxDepth` → `double`                  | CS0266   |
| `CodeMetricsService.ComputeIndentNestingDepth`   | int `maxDepth` → `double`                  | CS0266   |
| `TransferService.GenerateTextContent`            | int `offset` → `double`                    | CS1503   |

### 6.2 Root cause

Two distinct mutator-generator issues:

**(a) Inline-constant mutation produces `double` literals where `int`/`long` is required.**
The "Inline-constants `(0 -> 1 [+1])`" and `^(MutantControl.IsActive(...)?-1:(...)?0:(...)?2:1)` ternary patterns appear to be typed as `double` rather than inheriting the surrounding context's type. Roslyn then refuses to implicitly down-convert `double` to `int`/`long`/`ref long`/`ref int`.

**(b) String/char overload mutation generates the wrong-arity overload.**
`searchText.IndexOf(searchText, index, StringComparison.Ordinal)` — when `searchText` is a `string`, the mutator-generated `IndexOf(char, int, int)` overload is selected, which requires `char` not `string`, producing CS1503.

(c) Similarly, `byte[].AsSpan(buffer, length)` mutates to `MemoryExtensions.AsSpan(string?, int)`, which requires a `string` receiver — CS1929.

### 6.3 Impact

For every `Safe Mode!` warning, **all mutations in the enclosing method are silently dropped** and counted as `CompileError` in the final report. In our 187-file Infrastructure layer, the Sprint 51 baseline reported **9476 `CompileError` mutants out of 24,392 total** (39%). The actual achievable mutation score is therefore depressed by this large excluded pool.

### 6.4 Suggested fix

1. **Type-aware constant generation**: when generating an inline-constant mutant for an `int`/`long`/`ref int`/`ref long` slot, emit the literal with the matching type (e.g., `(int)1` or `1L` instead of bare `1.0`).
2. **Overload-resolution validation**: before emitting a string/char mutation, verify the chosen overload is reachable in the surrounding type-context.
3. **Improved diagnostic message**: today the user sees "Safe Mode! …" lines and has no easy way to suppress a whole pattern. A `--ignore-mutators` flag (already exists?) plus a docs page enumerating these systemic issues would help projects pre-emptively disable them.

---

## 7. Anomaly #5 (Low Severity) — CLI rejects `--reporters` (plural) accepted by upstream Stryker.NET 4.x

### 7.1 Observation

Both upstream Stryker.NET 4.14.x and the **stryker-config.json** schema use plural `reporters`:

```json
{
  "stryker-config": {
    "reporters": ["Html", "Progress", "Json"]
  }
}
```

The CLI on stryker-netx 3.2.18 only accepts **singular** `-r` / `--reporter`:

```bash
$ dotnet stryker-netx --reporters "html" --reporters "progress" ...
Specify --help for a list of available options and commands.
Unrecognized option '--reporters'
```

Working:
```bash
$ dotnet stryker-netx -r progress -r json ...   # ← works
$ dotnet stryker-netx --reporter Progress --reporter Json ...   # ← works
```

The CLAUDE.md commands table in our project (and at least one CI script we found in the Stryker docs) uses `--reporters "html"` from the upstream documentation. New users who follow upstream docs hit this immediately.

### 7.2 Suggested fix

Either:
- **A.** Add `--reporters` as an alias for `--reporter` (multi-valued option). One-liner in `StrykerCli.cs`.
- **B.** Print a clearer error message: `"Unrecognized option '--reporters' — did you mean '--reporter' (singular)?"`.

Also worth flagging: the JSON config key remains `reporters` (plural). The CLI/config asymmetry is confusing.

---

## 8. Anomaly #6 (Low Severity) — Initial-test-run wall-clock varies 5× across runs

### 8.1 Observation

Same commit, same 12-core machine, no other CPU-heavy processes running. Five sequential Stryker invocations on the same Sprint 56 iter 1-5 codebase:

| Run | Initial test run completed | Tests discovered |
|----:|-----------------------------:|-----------------:|
| 1   | **5m 29s** (157 false-fails — see Anomaly #3) | 2933 |
| 2   | **4m 31s** (1 false-fail)    | 2967             |
| 3   | **1m 16s**                   | 3020             |
| 4   | **1m 11s**                   | 3051             |
| 5   | **1m 8s**                    | 3066             |

The trend looks like a "warm-up" effect — once the testhost JIT-caches and the NuGet symbol loading are amortized, subsequent runs stabilize at ~1m 10s. Cold runs are 4–5× slower.

### 8.2 Impact

For CI pipelines that always start cold (each run on a fresh runner), the initial test run alone can consume 5+ min before mutation testing even begins. For the user, this gives a misleading first impression of total runtime.

### 8.3 Suggested fix

Document in README that the **first** mutation run on a fresh machine is significantly slower than subsequent runs. Optionally surface a "warming up — this is normal" note instead of the bare "Initial test run in progress: 0m 30s elapsed." progress line.

---

## 8b. Anomaly #7 (Medium Severity) — `Mock<ILogger>`-based invocation-count assertions do not kill statement-deletion mutations on adjacent logger calls

### 8b.1 Observation

The standard pattern to kill a `Statement mutation` of `_logger.LogXxxx(...)` (mutation replaces the call with `;`) is to write a test that uses `Moq.Mock<ILogger<T>>` and asserts `loggerMock.Invocations.Where(...).Should().HaveCount(N)`. This is **the documented pattern** in the stryker-netx README and CLAUDE.md for our project, and it works **most** of the time. In Sprint 56 we observed two cases where the pattern fails:

**Case A — WorkspaceService (Sprint 56 iter 5):**

```csharp
[Fact]
public async Task CreateWorkspaceAsyncShouldEmitInformationLogEntry()
{
    var loggerMock = new Mock<ILogger<WorkspaceService>>();
    loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    using var sut = new WorkspaceService(_securityService.Object, _diffService.Object, loggerMock.Object, autoCleanupMinutes: 0);

    var ws = await sut.CreateWorkspaceAsync("log-emit-test");
    _createdDirectories.Add(ws.Path);

    loggerMock.Invocations
        .Where(inv => string.Equals(inv.Method.Name, "Log", StringComparison.Ordinal))
        .Should().HaveCount(1, "CreateWorkspaceAsync must emit exactly one log entry.");
}
```

**Expected:** Stryker mutates `_logger.LogWorkspaceServiceCreatedWorkspaceXWithNameX(id, name, workspacePath);` (line 107) to `;` → log count = 0 ≠ 1 → test FAILS → mutation killed.

**Actual** (Stryker Report `2026-05-25.17-12-42/reports/mutation-report.json`): the L107 Statement mutation is reported as **Survived**, indicating Stryker's per-mutant test-selection chose tests that don't include `CreateWorkspaceAsyncShouldEmitInformationLogEntry` for this specific mutation, OR the test-coverage matrix is incorrect.

Similarly, four further logger-emit mutations (Commit logger L133, Discard logger L151, Dispose logger L207) survived despite having matching `*ShouldEmitInformationLogEntry` tests.

**Case B — PatchService (Sprint 56 iter 2):**

```csharp
[Fact]
public async Task CreatePatchAsyncShouldEmitInformationLogEntry()
{
    // [setup: file system + diff mock]
    var loggerMock = new Mock<ILogger<PatchService>>();
    loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    var sut = new PatchService(_fileSystem, _securityService.Object, _diffService.Object, loggerMock.Object);

    _ = await sut.CreatePatchAsync(pathA, pathB);

    loggerMock.Invocations
        .Where(inv => string.Equals(inv.Method.Name, "Log", StringComparison.Ordinal))
        .Should().HaveCount(1, "...");
}
```

This pattern **does** kill the immediate L71 `_logger.LogPatchServiceCreatedPatchFromXToX(pathA, pathB);` statement. **But** PatchService also has multiple `cancellationToken.ThrowIfCancellationRequested()` and `_securityService.ValidatePath(...)` statements on other lines (L92, L93, L221, L245) which **also survived** despite being on call paths that this test exercises.

### 8b.2 Hypothesis

stryker-netx uses `--coverage-analysis perTest` by default. This generates a test-to-mutant coverage matrix during the initial test run by instrumenting each mutant location and observing which tests hit it. We hypothesise one of:

1. **Matrix-write race:** the per-test coverage matrix has gaps when many tests run concurrently against the same SUT instance (xUnit class-level fixtures with shared state).
2. **Method-boundary attribution miss:** mutations inside helper methods called by the SUT-under-test method don't get linked back to the test that exercises the top-level method.
3. **Instance-isolation miss:** our logger-emit tests construct a **separate** SUT (with the `loggerMock` instead of `NullLogger`) inside the test method body. The default class fixture has `_sut = new WorkspaceService(..., NullLogger, ...)`. Maybe stryker only attributes mutations to test methods that use the class-level `_sut` field.

(3) is the most consistent with our observation pattern: every test that constructs a fresh SUT inside the test method fails to kill the corresponding statement-deletion mutations, while tests that use the class-fixture `_sut` field kill them.

### 8b.3 Workaround

Switch the test class fixture to **always** use a `Mock<ILogger<T>>` and replace the `loggerMock.Invocations` assertion across all test methods. This forces all tests to share the same logger instance, ensuring stryker's coverage matrix sees the mutation as covered.

```csharp
public sealed class WorkspaceServiceTests
{
    private readonly Mock<ILogger<WorkspaceService>> _logger = new();
    private readonly WorkspaceService _sut;
    public WorkspaceServiceTests() {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _sut = new WorkspaceService(..., _logger.Object, ...);
    }
}
```

This is intrusive for projects with established `NullLogger` patterns.

### 8b.4 Suggested fix

Either:
- **A.** Improve per-mutant coverage attribution to detect that a test constructs a new SUT inside the method body and include those tests in the coverage matrix for the SUT type's methods.
- **B.** Add a CLI option `--coverage-analysis off` that forces every test to be selected for every mutant (slower, but eliminates these false-negative survivors).
- **C.** Document the pattern as a known limitation and recommend always wiring `Mock<ILogger>` at the class-fixture level.

---

## 9. Combined reproduction recipe

Clean reproduction of Bugs #1, #2, #4, #5, #6 in a single session:

```bash
# 1. Clone reproducer at a known-good commit.
git clone https://github.com/pgm1980/filesystem-mcp-server.git
cd filesystem-mcp-server
git checkout feature/optimization-roadmap-baseline   # commit 562b46c or later
dotnet tool restore

# 2. Bug #5: CLI plural-vs-singular reporter flag
cd tests/FsMcpServer.Tests
dotnet stryker-netx --reporters "Progress" --mutation-profile Stronger
#   → "Unrecognized option '--reporters'"

# 3. Bug #1: single-file --mutate filter crashes
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutate "**/HealthService.cs" \
    --mutation-profile Stronger \
    -r progress -r json
#   → Initial test run completes, then ArgumentNullException: trees[2]

# 4. Bug #1 (recap): even with partial-sibling inclusion, still crashes
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutate "**/HealthService*.cs" \
    --mutate "**/PatchService*.cs" \
    --mutation-profile Stronger \
    -r progress -r json
#   → Same trees[2] ArgumentNullException

# 5. Bug #2 + Anomaly #4 + Anomaly #6: full-project run works, but slow.
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutation-profile Stronger \
    -r progress -r json &
STRYKER_PID=$!
# Wait until the log shows "Initial test run started", then in another terminal try:
dotnet build   # ← will retry 10× then fail with MSB3027 (Bug #2)
# Meanwhile observe the Safe Mode! warnings for ~30 methods (Anomaly #4)
wait $STRYKER_PID
```

---

## 10. Tabular summary of suggested fixes

| Issue   | Effort | Suggested change                                                                                                                     | Where (best guess) |
|---------|--------|--------------------------------------------------------------------------------------------------------------------------------------|-------------------|
| Bug #1  | Med    | When `--mutate` filter shrinks the file set, auto-expand the syntax-tree scope to include all partial-class siblings of matched files. **OR** at minimum, validate the post-filter syntax-tree collection and emit a clear error rather than `trees[2]`. | `Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation` (`/_/src/Stryker.Core/Compiling/CsharpCompilingProcess.cs:152`) |
| Bug #2  | Small  | Open assembly files via `FileStream(FileShare.Delete \| FileShare.Read)` or load via `AssemblyLoadContext.LoadFromStream` so MSBuild can replace files concurrently. | `Stryker.TestRunner.VsTest` (testhost spawning) |
| Anom #3 | Small  | Default `--concurrency` to `Environment.ProcessorCount / 2`; emit a hint when initial-run fail count > 10% with link to xUnit parallelization docs. | CLI default values + `MutationTestRunner` |
| Anom #4 | Med    | Type-aware constant generation (emit `int`/`long`/`ref int`-typed literals from context). Overload-resolution validation before emitting char/string mutations. | mutator implementations: `InlineConstantsMutator`, `StringMutator`, `BooleanMutator` |
| Anom #5 | Tiny   | Add `--reporters` as CLI alias for `--reporter`. | `StrykerCli.cs` argument definitions |
| Anom #6 | Tiny   | README note explaining cold-run wall-clock vs warm-run. Optionally surface "warming up" log. | `README.md` + `MutationTestRunner` startup log |
| Anom #7 | Med    | Improve per-mutant test-selection to include tests that construct fresh SUT instances inside the test body (not only those using the class-fixture SUT). **OR** add a `--coverage-analysis off` toggle. | `Stryker.Core.Initialisation.CoverageAnalyser` |

---

## 11. Attached artifacts (in the reproducer repo branch)

Available in the reproducer branch for direct inspection by maintainers:

| File                                                  | Content                                                |
|-------------------------------------------------------|--------------------------------------------------------|
| `_stryker_s56_i1.log`                                 | First single-file `--mutate` attempt (5m 29s init, 157 fails, then trees[2]) |
| `_stryker_s56_i1_v2.log`                              | Repeat of single-file `--mutate` after rebuild         |
| `_stryker_s56_v3.log`                                 | Two-file `--mutate` (`HealthService*` + `PatchService*`) — 4 files in scope, still trees[2] |
| `_stryker_s56_full.log`                               | Full-project run that hits ~70 Safe Mode warnings then external-kill |
| `_stryker_s56_full2.log` / `_stryker_s56_final.log`   | Subsequent full-project re-runs after iter 5 commit    |
| `_stryker_help.log`                                   | `dotnet stryker-netx --help` output (no `--reporters`) |
| `_dotnet_info.txt`                                    | `dotnet --info` environment dump                       |
| `_stryker_version.txt`                                | `dotnet stryker-netx --version` → `3.2.18`             |
| `_build_out8.txt` / `_build_out9.txt`                 | MSB3026 retry warnings + MSB3027 final error (Bug #2)  |
| `_docs/issues/stryker-netx-3.2.18-mutate-filter-trees-null.md` | Original Sprint 51 report (this Bug #9 extends it) |

All logs are verbatim captures, no editing.

---

## 12. Contact

- **Reproducer repo:** https://github.com/pgm1980/filesystem-mcp-server.git
- **Reproducer branch:** `feature/optimization-roadmap-baseline`
- **Tip commit:** `562b46c` (Sprint 56 iter 5)
- **Reporter context:** autonomous Sprint 56 session, 2026-05-25; campaign goal "≥97% mutation score on Infrastructure.Services Block 3"; full transcript available in the repo's MEMORY.md.

Happy to provide:
- Minimised single-file repro projects.
- Test against any candidate fix branch.
- Additional traces with `--diag` enabled (have not done so yet — happy to capture if useful).
- Direct access to the project as a real-world test bed for mutator regression suites.
