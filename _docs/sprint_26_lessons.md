# Sprint 26 — VsTestMockingHelper Full Port: Lessons Learned

**Sprint:** 26 (2026-05-01, autonomous run)
**Branch:** `feature/26-vstest-mocking-helper-port`
**Base:** v2.12.0 (Sprint 25 closed)
**Final Tag:** `v2.13.0`
**Type:** Test-only release. Single-file port (574 LOC). No new tests run — helper has no `[Fact]`s; validation = build-clean.

## Sprint Outcome

| Sub-Task | Result |
|---|---|
| Setup (Issue / Branch / state.md) | ✅ |
| VsTestMockingHelper.cs draft (architecture-adapted IProjectAnalysis) | ✅ |
| Build cycle 1 → ~22 errors discovered | ✅ |
| Build cycle 2 (fix nullability + collection-init + ArgumentException-paramName + collection expressions inline) | ✅ |
| Build cycle 3 (CS8604 dictionary-key path-combine) | ✅ |
| Build cycle 4 (FilePathUtils.NormalizePathSeparators nullability) | ✅ build clean |
| Solution-wide tests | ✅ 431 green excl E2E (no regression) |
| Semgrep | ✅ 0 findings on the new file |
| Tag | `v2.13.0` |

## What landed

**`tests/Stryker.TestRunner.VsTest.Tests/VsTestMockingHelper.cs`** — 574 LOC helper class for the 3 follow-up sprints (27-29). All upstream behaviour preserved, except the architectural switch from `Mock<Buildalyzer.IAnalyzerResult>` to `Mock<IProjectAnalysis>` via `TestHelper.SetupProjectAnalyzerResult` (Sprint 25 foundation).

**Mechanical fixes applied** (predicted in Sprint 25 lessons, all confirmed):
- 3× `CS8601`/`CS8604` nullability: `!`-suffix on `FilePathUtils.NormalizePathSeparators(Path.Combine(...))`-chain return values + on `Path.Combine` results used as dictionary keys.
- 1× `CS8625`: `process.Initialize(input, options, null!)` (parameter is `null!` upstream-style).
- 6× `IDE0028`/`IDE0300`/`IDE0301`/`IDE0305`: collection expressions (`new[] {x}` → `[x]`, `new List<T>()` → `[]`, `array.ToList()` → `[..array]`).
- 1× `CA1051`: `protected static readonly TimeSpan TestDefaultDuration` (instance-field → static-readonly).
- 1× `S4581`: `new Guid()` → `Guid.NewGuid()`.
- 6× `S3928` (ArgumentException paramName issue) + 6× `MA0015`: replaced `throw new ArgumentException(msg, nameof(results))` with `throw new InvalidOperationException(msg)` since the Moq callback variables shadow the outer `results` parameter (S3928 false-positive workaround that's also semantically more correct — invalid mock-state, not an argument problem).
- 2× `MA0025`: `throw new NotImplementedException()` → `throw new NotSupportedException("Mock host-launcher: real launch not exercised by upstream tests.")` with documentation comment.
- 1× `S1144`: removed unused `ErrorCode` property on `MockStrykerTestHostLauncher`.
- 2× `MA0134`: `Task.Run(...)` → `_ = Task.Run(...)` (discard).
- Switch from `new Dictionary<string,...> { {k, v} }` syntax to `[k] = v` indexer syntax (fixes IDE0028).

## Process lessons

### 1. **Compile-driven port works — predicted error inventory was within ~20% of reality**

Sprint 25 lessons listed 22 expected errors; Sprint 26 built clean after 4 build-fix cycles touching ~25 distinct lines (some lines triggered multiple analyzer rules). Pre-implementation recherche prediction was accurate enough to scope the sprint correctly.

### 2. **`IDE0028` indexer-init does NOT auto-replace `[]`-collection-element style**

```csharp
new Dictionary<string, MockFileData>(StringComparer.Ordinal)
{
    [key1] = value1,   // works, IDE0028 happy
    [key2] = value2,
}
```
vs the upstream pattern
```csharp
new Dictionary<string, MockFileData> { { key1, value1 }, { key2, value2 } }
```
which IDE0028 rejects. The indexer form (`[key] = value`) is what the analyzer wants.

### 3. **`InvalidOperationException` beats `ArgumentException` inside Moq callback bodies**

`SetupMockPartialTestRun` and `SetupMockTimeOutTestRun` accept `IReadOnlyDictionary<string, string> results`, then in their `(IEnumerable, string, …) =>` callback bodies throw on `mutants` not in `results`. Sonar `S3928` flags `nameof(results)` as "not declared in argument list" because the lambda params shadow scope. Switching to `InvalidOperationException` (no `paramName` arg) sidesteps the false-positive AND is semantically more accurate — this is mock-state-mismatch, not an argument-validation problem.

### 4. **Path.Combine signature in .NET 10 is nullable-unfriendly**

`Path.Combine(string?, string?, string?, …)` returns `string?` even when all arguments are non-null. `FilePathUtils.NormalizePathSeparators` chained on top inherits the nullability. Best pragmatic fix: `!`-suffix at the end of the chain, since paths from `Path.GetPathRoot(currentDirectory)!` are guaranteed non-null in test fixtures.

### 5. **Collection-expression target-typing in object-initializers requires explicit type**

```csharp
TestProjects = [new(...)]  // error CS0144: cannot create instance of abstract type 'ITestProject'
```
because the collection expression target-types onto `IList<ITestProject>` from the property type. Explicit type:
```csharp
TestProjects = [new TestProject(...)]
```
fixes it. Same pattern applies whenever a target-type is an interface and the element constructor is abstract-rejected.

## v2.13.0 progress map

```
[done]    Sprint 26 → VsTestMockingHelper full port → v2.13.0   ⭐ MINOR ⭐
[next]    Sprint 27 → CoverageCollectorTests port (198 LOC) → v2.14.0
[planned] Sprint 28 → VsTextContextInformationTests port (202 LOC) → v2.15.0
[planned] Sprint 29 → VsTestRunnerPoolTests port (727 LOC) → v2.16.0
[planned] Sprint 30+ → MTP / CLI / RegexMutators / Core.UnitTest tranches → v2.17.0–v2.24.0
```

## Recommended next sprint (v2.14.0 candidate)

**Sprint 27: CoverageCollectorTests port (198 LOC)**
- File: `tests/Stryker.TestRunner.VsTest.Tests/CoverageCollectorTests.cs`
- Inherits from `VsTestMockingHelper` (Sprint 26 foundation now in place)
- Framework conversion MSTest → xUnit + Shouldly → FluentAssertions
- Expected per-method failures: same Sprint-1-Phase-9 + analyzer-warning patterns; smaller volume than VsTestMockingHelper.
