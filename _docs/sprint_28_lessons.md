# Sprint 28 — VsTestContextInformationTests Port: Lessons Learned

**Sprint:** 28 (2026-05-01, autonomous run)
**Branch:** `feature/28-vstest-context-information-tests-port`
**Base:** v2.14.0 (Sprint 27 closed)
**Final Tag:** `v2.15.0`
**Type:** Test-only release. 202 LOC + TestResources/ExampleSourceFile.cs (read-only sample data, NOT compiled).

## Sprint Outcome

| Sub-Task | Result |
|---|---|
| Port `VsTestContextInformationTests.cs` (4 Facts + 1 Theory ×7 = 11 tests) | ✅ |
| Add TestResources/ExampleSourceFile.cs | ✅ (copied from upstream Stryker.Core.UnitTest) |
| csproj: \<Compile Remove="TestResources\\\*\*\*.cs"/\> + \<None Include\> CopyToOutputDirectory | ✅ |
| Build cycle 1: 2 errors (MA0016, CA1859) | ✅ |
| Build cycle 2: clean | ✅ |
| 11 new tests grün | ✅ 19/19 in VsTest.Tests assembly |
| Solution-wide tests | ✅ 449 grün excl E2E |
| Semgrep | ✅ 0 findings |
| Tag | `v2.15.0` |

## What landed

### `tests/Stryker.TestRunner.VsTest.Tests/VsTestContextInformationTests.cs`
11 tests covering `VsTestContextInformation` discovery / cleanup / param-setup / log-level mapping:
- `InitializeAndDiscoverTests` — discovers 2 mock test cases
- `CleanupProperly` — verifies `EndSession` on dispose
- `InitializeAndSetParameters` — default trace level + log file path
- `InitializeAndSetParametersAccordingToOptions` — `LogToFile=true` produces info-level + log file path
- `InitializeAndSetProperLogLevel` Theory ×7 — Serilog → TraceLevel mapping (Debug/Verbose/Information/Warning/Error/Fatal/-1)

### `tests/Stryker.TestRunner.VsTest.Tests/TestResources/ExampleSourceFile.cs`
Sample C# file used as text input by the upstream tests via `File.ReadAllText`. Excluded from `<Compile>` (analyzers would flag the intentional code smells) and copied to output via `<None Include>`. The exclusion-pattern is the new csproj idiom for upstream test-resources.

## Process lessons

### 1. **Test-resources need explicit `<Compile Remove>` exclusion**

Default SDK behaviour is to compile ALL `.cs` files in the project tree. `ExampleSourceFile.cs` is intentionally analyzer-violating sample code (unused methods, missing static, unscoped namespace) — meant to be read as text, not compiled. Sprint 28 csproj idiom:
```xml
<Compile Remove="TestResources\**\*.cs" />
<None Include="TestResources\**\*.*" CopyToOutputDirectory="PreserveNewest" />
```
This pattern carries forward to every upstream-test-port that uses `File.ReadAllText` on samples.

### 2. **CA1859 (concrete-for-perf) and MA0016 (prefer-abstraction) are mutually exclusive**

Same parameter signature satisfies neither. CA1859 wants `List<T>`; MA0016 wants `IList<T>`/`ICollection<T>`/`IEnumerable<T>`. Targeted `#pragma warning disable CA1859` with a "perf is not the concern in test code" justification is the cleanest fix; the abstraction-preference (MA0016) wins because it documents test-code intent better than performance.

### 3. **Compile-driven port keeps accelerating: Sprint 27 = 2 cycles, Sprint 28 = 2 cycles + resource setup**

The mechanical-fix-categories knowledge base from Sprint 26 + 27 covered ~100% of the analyzer errors in this file at first build. The only "new" adjustment was the test-resource csproj-idiom, which is a one-time investment per test-project (further file-ports in the same project inherit it).

### 4. **`Task.Run(...).ContinueWith(...)` discard requires explicit TaskScheduler**

Upstream's:
```csharp
Task.Run(() => handler.HandleDiscoveredTests(tests))
    .ContinueWith((_, u) => handler.HandleDiscoveryComplete((int)u, null, aborted), tests.Count);
```
Triggered MA0140 (avoid implicit task scheduler). Fix: append `, TaskScheduler.Default` argument. Plus `_ = Task.Run(...)` discard for MA0134.

## v2.15.0 progress map

```
[done]    Sprint 28 → VsTestContextInformationTests port → v2.15.0   ⭐ MINOR ⭐
[next]    Sprint 29 → VsTestRunnerPoolTests port (727 LOC, größtes file) → v2.16.0
[planned] Sprint 30+ → MTP / CLI / RegexMutators / Core.UnitTest tranches → v2.17.0–v2.24.0
```

## Recommended next sprint (v2.16.0 candidate)

**Sprint 29: VsTestRunnerPoolTests port (727 LOC, größtes file)**
- Inherits VsTestMockingHelper (Sprint 26)
- Probably the highest analyzer-error volume so far
- TestResources csproj-idiom is already in place
- Expected ~3-5 build-fix cycles based on Sprint-26-Pattern-extrapolation
