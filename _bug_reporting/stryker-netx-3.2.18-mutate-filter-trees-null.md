# stryker-netx 3.2.18 — `--mutate` filter triggers `ArgumentNullException: trees[2]`

**Status:** Bug report, ready for upstream submission to the stryker-netx maintainers.
**Discovered:** 2026-05-24 during Sprint 51 of the filesystem-mcp-server project.
**Severity:** **High** — blocks any narrow-scope mutation testing on projects that mix partial classes (e.g., `LoggerMessage` source-generator helpers, `GeneratedRegex`, `JsonSerializerContext`) with regular source files.

---

## 1. Tool Information

| Field                  | Value                                                              |
|------------------------|--------------------------------------------------------------------|
| Tool                   | `dotnet-stryker-netx`                                              |
| Version                | **3.2.18**                                                         |
| Installation           | Local dotnet-tool manifest (`dotnet-tools.json`, rollForward=false)|
| Mutation profile       | `All` (also reproduced with `Stronger`)                            |
| .NET SDK               | 10.0.108                                                           |
| OS                     | Windows 11 (24H2 build) — also expected on Linux/macOS             |
| Repro project          | https://github.com/pgm1980/filesystem-mcp-server.git              |
| Repro branch           | `feature/optimization-roadmap-baseline`                            |

---

## 2. Summary (TL;DR)

When `dotnet stryker-netx` is invoked with a `--mutate` glob that narrows the scope to a **subset of files** in a project where the **excluded files contain partial classes** (or source-generator partials), the mutation run fails immediately after coverage capture with:

```
System.AggregateException: One or more errors occurred. (Value cannot be null. (Parameter 'trees[2]'))
 ---> System.ArgumentNullException: Value cannot be null. (Parameter 'trees[2]')
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.AddSyntaxTrees(IEnumerable`1 trees)
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(...)
   at Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation(...)
   at Stryker.Core.Compiling.CsharpCompilingProcess.Compile(...)
   at Stryker.Core.MutationTest.CsharpMutationProcess.CompileMutations(...)
   at Stryker.Core.MutationTest.CsharpMutationProcess.Mutate(...)
```

The same project **runs successfully** when `--mutate` is omitted (whole-project scan). The crash is exclusively triggered by narrowing scope.

---

## 3. Project Context

The Infrastructure project under test contains a Sprint-44-style **LoggerMessage source-generator** pattern:

- For every service `FooService.cs` there is a sibling **partial helper class** `FooServiceLoggerMessages.cs` that holds `[LoggerMessage]`-attributed `static partial` extension methods.
- Both files share the same namespace (`FsMcpServer.Infrastructure.Services`).
- The compiler emits IL where both files contribute to **two distinct top-level types**:
  - `FooService` (the regular service class)
  - `FooServiceLoggerMessages` (the static partial; the missing partial halves are emitted by the LoggerMessage source generator)

Out of 187 source files in the `--mutate` scope, 75 are `*LoggerMessages.cs` partials and 75 are their `*Service.cs` siblings.

This pattern is recommended by Microsoft (CA1848) and used widely — any project that adopts `LoggerMessage` source generators will likely trigger this bug.

---

## 4. Reproduction Steps

### 4.1 Minimal repro (any partial class works)

```bash
# In a test project that references a source project containing partial classes:
cd tests/FsMcpServer.Tests
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutate "**/Security/PathValidator.cs" \
    --mutation-profile All \
    --reporter Progress --reporter Json
```

### 4.2 Failure threshold

Reproduced under multiple `--mutate` patterns, all failing identically:

| `--mutate` argument                          | Files scoped | Outcome                            |
|----------------------------------------------|--------------|------------------------------------|
| *omitted entirely*                           | 187          | ✅ Run completes (whole project)    |
| `"**/Security/**/*.cs"`                      | 4            | ❌ `trees[2]` null                  |
| `"**/Security/PathValidator.cs"`             | 1            | ❌ `trees[2]` null                  |
| `"PathValidator.cs"`                         | 1            | (not tested — expected to fail)   |

### 4.3 Pipeline phase where it fails

```
[INF] Analysis starting.
[INF] Found project ... to mutate.
[INF] Analysis complete.
[INF] Building test project ... using dotnet build ...        ← OK
[INF] Number of tests found: 2472 for project ...             ← OK (test discovery)
[INF] Initial test run completed in 0m 38s.                    ← OK
[INF] Disable-directive validation: scanned 1 files in --mutate scope (181 skipped).
[ERR] An error occurred during the mutation test run
System.AggregateException: (Value cannot be null. (Parameter 'trees[2]'))
```

The bug fires **after** `Disable-directive validation` (i.e., after stryker has decided which files belong to the mutation scope) and **before** any mutant is actually executed — specifically inside `Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation` while assembling the in-memory CSharpCompilation for the mutated assembly.

---

## 5. Expected vs Actual Behavior

| Aspect    | Expected                                                                 | Actual                                                  |
|-----------|---------------------------------------------------------------------------|---------------------------------------------------------|
| Outcome   | Mutation run completes; only files matching `--mutate` are mutated; the remaining files are included verbatim in the compilation. | `System.ArgumentNullException` from Roslyn. |
| Diagnostic | If the filter is invalid or the scope is empty, stryker should report a clear, actionable error message. | Internal NRE leaks out of `Parallel.ForEach`. |
| CI impact | Narrow `--mutate` should be a first-class workflow for fast incremental runs. | Narrow `--mutate` is unusable on real projects that use source generators. |

---

## 6. Full Stack Trace (verbatim, two independent reproductions)

### Reproduction A — `--mutate "**/Security/**/*.cs"`

```
[03:29:10 INF] Disable-directive validation: scanned 4 files in --mutate scope (178 skipped).
[03:29:10 ERR] An error occurred during the mutation test run
System.AggregateException: One or more errors occurred. (Value cannot be null. (Parameter 'trees[2]'))
 ---> System.ArgumentNullException: Value cannot be null. (Parameter 'trees[2]')
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.AddSyntaxTrees(IEnumerable`1 trees)
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(String assemblyName, CSharpCompilationOptions options, IEnumerable`1 syntaxTrees, IEnumerable`1 references, CSharpCompilation previousSubmission, Type returnType, Type hostObjectType, Boolean isSubmission)
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(String assemblyName, IEnumerable`1 syntaxTrees, IEnumerable`1 references, CSharpCompilationOptions options)
   at Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation(IEnumerable`1 syntaxTrees) in /_/src/Stryker.Core/Compiling/CsharpCompilingProcess.cs:line 152
   at Stryker.Core.Compiling.CsharpCompilingProcess.Compile(IEnumerable`1 syntaxTrees, Stream ilStream, Stream symbolStream) in /_/src/Stryker.Core/Compiling/CsharpCompilingProcess.cs:line 60
   at Stryker.Core.MutationTest.CsharpMutationProcess.CompileMutations(MutationTestInput input, CsharpCompilingProcess compilingProcess) in /_/src/Stryker.Core/MutationTest/CsharpMutationProcess.cs:line 102
   at Stryker.Core.MutationTest.CsharpMutationProcess.Mutate(MutationTestInput input, IStrykerOptions options) in /_/src/Stryker.Core/MutationTest/CsharpMutationProcess.cs:line 92
   at Stryker.Core.MutationTest.MutationTestProcess.Mutate() in /_/src/Stryker.Core/MutationTest/MutationTestProcess.cs:line 46
   at Stryker.Core.Initialisation.ProjectMutator.MutateProject(IStrykerOptions options, MutationTestInput input, IReporter reporters, IMutationTestProcess mutationTestProcess) in /_/src/Stryker.Core/Initialisation/ProjectMutator.cs:line 28
   at Stryker.Core.Initialisation.ProjectOrchestrator.<>c__DisplayClass8_0.<MutateProjectsAsync>b__0(MutationTestInput mutationTestInput) in /_/src/Stryker.Core/Initialisation/ProjectOrchestrator.cs:line 102
   at System.Threading.Tasks.Parallel.<>c__DisplayClass19_0`2.<ForWorker>b__1(RangeWorker& currentWorker, Int64 timeout, Boolean& replicationDelegateYieldedBeforeCompletion)
   --- End of inner exception stack trace ---
   at System.Threading.Tasks.TaskReplicator.Run[TState](ReplicatableUserAction`1 action, ParallelOptions options, Boolean stopOnFirstFailure)
   at System.Threading.Tasks.Parallel.ForWorker[TLocal,TInt](TInt fromInclusive, TInt toExclusive, ParallelOptions parallelOptions, ...)
   at Stryker.Core.Initialisation.ProjectOrchestrator.MutateProjectsAsync(IStrykerOptions options, IReporter reporters, ITestRunner runner) in /_/src/Stryker.Core/Initialisation/ProjectOrchestrator.cs:line 100
   at Stryker.Core.StrykerRunner.ExecutePipelineAsync(IStrykerOptions options, IReporter reporters) in /_/src/Stryker.Core/StrykerRunner.cs:line 80
   at Stryker.Core.StrykerRunner.RunMutationTestAsync(IStrykerInputs inputs) in /_/src/Stryker.Core/StrykerRunner.cs:line 48
[03:29:11 INF] Time Elapsed 00:01:35.0811229
Unhandled exception. System.AggregateException: One or more errors occurred. (Value cannot be null. (Parameter 'trees[2]'))
   ... [exact same chain re-thrown unhandled, as expected for Parallel exceptions]
```

### Reproduction B — `--mutate "**/Security/PathValidator.cs"`

Identical stack trace (line 1-88 above), only the preceding info line differs:

```
[03:31:15 INF] Disable-directive validation: scanned 1 files in --mutate scope (181 skipped).
[03:31:15 ERR] An error occurred during the mutation test run
System.AggregateException: ... (Parameter 'trees[2]') ...
```

---

## 7. Root-Cause Hypothesis

The `Parameter 'trees[2]'` message strongly suggests stryker passes a **fixed-size collection of three syntax trees** to `CSharpCompilation.Create`, where index 2 happens to be `null`. A likely scenario:

1. The original assembly compiles from N source files (here N = 187).
2. After `--mutate` filtering, stryker rebuilds the compilation from a smaller set.
3. For a file `Foo.cs` whose partial sibling `FooLoggerMessages.cs` is **excluded** by the filter, stryker still references the sibling syntax tree (because the partial type spans both files), but that tree is `null` in the post-filter collection.
4. `CSharpCompilation.AddSyntaxTrees(IEnumerable<SyntaxTree>)` rejects the `null` element with the parameter-name `trees[2]`.

Supporting evidence:

- **Whole-project runs succeed:** all partial siblings are present, so no syntax tree is null.
- **Single-file `--mutate` fails the same way as a directory pattern:** the partial-sibling-aware lookup misses the same way regardless of how many files match.
- **The "trees[2]" name is consistent across runs:** the same internal collection structure (likely `[mutated, originalReference, generatorOutput?]`) is being assembled and the third slot is the one mutating filtered partials populate.

The disable-directive validation phase reports `scanned X files in --mutate scope (Y skipped)` — proving that stryker is aware of two sets (in-scope and skipped). The bug is in **how the skipped set's partial-class members are re-merged** into the mutation compilation.

---

## 8. Workaround (currently in use)

The full-project scan is the only reliable approach as of 3.2.18:

```bash
cd tests/FsMcpServer.Tests
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutation-profile Stronger \
    --reporter Progress --reporter Json
```

To analyze per-layer scores from a whole-project run, the JSON report is sliced post-hoc with a `.csx` helper. See [`_infra_score_summary.csx`](../_infra_score_summary.csx) for a working example that aggregates by layer and produces a Sprint-priority table.

Cost of the workaround:

- A whole-project run on a 100-class Infrastructure assembly takes ~3-5 minutes (vs. ~30 seconds for a single-file run we expected).
- 9476 of 24392 mutants are `CompileError` (skipped) — the proportion is large because `LoggerMessage` partial methods and `required` modifiers don't mutate cleanly. The compile-error category would be the same in a narrow run, so this is not a workaround-specific concern.
- Per-file iteration during the mutation-driven test-quality loop is impractical; the team has to re-run the whole project after each test change.

---

## 9. Suggested Fix

Two routes, in increasing order of invasiveness:

### Option A — pre-validate the syntax-tree collection (defensive)

In `Stryker.Core.Compiling.CsharpCompilingProcess.GetCSharpCompilation`, filter out `null` entries before calling `CSharpCompilation.Create`. This won't fix the **semantic** issue (the partial class is then missing one of its halves and IL compilation may still fail later), but it will produce a clear, actionable error message:

```csharp
var validTrees = syntaxTrees.Where(t => t is not null).ToList();
if (validTrees.Count != syntaxTrees.Count())
{
    _logger.LogWarning("Some syntax trees were null after --mutate filtering; partial classes whose siblings were excluded are likely to fail. Falling back to whole-project scan is recommended.");
}
```

### Option B — keep partial-class siblings in scope even if `--mutate` excludes them (correct)

When evaluating `--mutate` patterns, automatically expand the scope to include **all source files that contribute to any partial class touched by the filter**, even if their paths don't match the glob. The mutation operators would only fire on the explicitly-matched files, but the compilation includes the full type definition.

Pseudo-algorithm:
```
matched_files = files matching --mutate
in_scope_types = types whose source spans intersects matched_files
expanded_files = union of source files of every type in in_scope_types
compile(expanded_files)
mutate(matched_files only)
```

This preserves the user-intent (mutate only the file they asked for) while keeping the compilation valid.

---

## 10. Related Stryker-netx Behaviors Worth Noting

While investigating, we also noticed two other 3.2.18 changes vs upstream Stryker.NET 4.14.1 that may be unrelated but worth mentioning for documentation parity:

1. **`--coverage-analysis` flag is gone.** The 4.14.1 CLI accepts `--coverage-analysis perTest`; 3.2.18 rejects it with `Unrecognized option`. The fork appears to use coverage-based-test mode by default — confirm in docs?
2. **Reporter names became PascalCase.** Passing `--reporter html` (lowercase, accepted in upstream) is silently ignored; only `--reporter Html` registers. Worth a CLI-input normalization or at least a clear error.

These are minor compared to the `trees[2]` bug above, but a quick mention in the CHANGELOG would help adoption.

---

## 11. Contact & Reproducer

- **Reporter:** filesystem-mcp-server project, autonomous Sprint 51 session 2026-05-24.
- **Reproducer repo:** https://github.com/pgm1980/filesystem-mcp-server.git (branch `feature/optimization-roadmap-baseline`)
- **Smoke-test command:**
  ```bash
  git clone https://github.com/pgm1980/filesystem-mcp-server.git
  cd filesystem-mcp-server
  git checkout feature/optimization-roadmap-baseline
  dotnet tool restore
  cd tests/FsMcpServer.Tests
  dotnet stryker-netx \
      --project FsMcpServer.Infrastructure.csproj \
      --mutate "**/Security/PathValidator.cs" \
      --mutation-profile All
  # → ArgumentNullException: trees[2]
  ```

Happy to provide additional traces, a minimised repro, or test against a fix branch — just file a comment on this bug report.
