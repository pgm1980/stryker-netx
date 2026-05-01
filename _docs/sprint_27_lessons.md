# Sprint 27 — CoverageCollectorTests Port: Lessons Learned

**Sprint:** 27 (2026-05-01, autonomous run)
**Branch:** `feature/27-coverage-collector-tests-port`
**Base:** v2.13.0 (Sprint 26 closed)
**Final Tag:** `v2.14.0`
**Type:** Test-only release. 198 LOC + companion `MutantControl` mock-class extracted to its own file.

## Sprint Outcome

| Sub-Task | Result |
|---|---|
| Setup (Issue / Branch / state.md) | ✅ |
| Port `CoverageCollectorTests.cs` (198 LOC, 7 tests) | ✅ |
| Extract inline `MutantControl` mock to own file | ✅ (MA0048 fix) |
| Build cycle 1 → 13 errors discovered (7× CS8604, 1× MA0048, 4× CA2211/MA0069) | ✅ |
| Build cycle 2 (after `?? string.Empty` + extract + suppress trio) | ✅ build clean |
| 7 new tests grün | ✅ 8/8 in VsTest.Tests assembly |
| Solution-wide tests | ✅ 438 grün excl E2E (388 + 17 + 10 + 15 + 8) |
| Semgrep | ✅ 0 findings on 2 new source files |
| Tag | `v2.14.0` |

## What landed

### `tests/Stryker.TestRunner.VsTest.Tests/CoverageCollectorTests.cs`
Port of upstream's same-named file. 7 `[Fact]` test methods covering:
- `ProperlyCaptureParams` — flags coverage capture on test-case start
- `RedirectDebugAssert` — `Debug.Fail` rerouting
- `ProperlySelectMutant` — per-test-case mutant selection from mutantMap
- `SelectMutantEarlyIfSingle` — single-mutant fast-path
- `ProperlyCaptureCoverage` — normal + static coverage capture
- `ProperlyReportNoCoverage` — empty-coverage report shape
- `ProperlyReportLeakedMutations` — out-of-test mutation reporting

### `tests/Stryker.TestRunner.VsTest.Tests/MutantControl.cs` (new file)
Extracted from upstream's inline `class MutantControl` at end of `CoverageCollectorTests.cs`. The CoverageCollector talks to this type by name via reflection, so the public-state surface MUST mirror upstream's MutantControl: public static fields (not properties). Triple-suppression (CA1051 + CA2211 + MA0069) with reflection-binding rationale.

## Process lessons

### 1. **Sprint 26 mechanical-fix-categories accuracy improved with each port**

Sprint 26 lessons documented 6 categories (collection-expressions, ArgumentException, nullability, async-discard, NotImplemented, code-quality). Sprint 27 added 2 new categories specific to test-files (vs the helper file):
- **CS8604 on `GetType().Namespace`**: production parameter is `string`, but `Namespace` returns `string?` for global-namespace types. Fix: `?? string.Empty` (semantic — we never run tests in global namespace, but the analyzer can't prove it).
- **CA2211/MA0069 visible non-constant static fields**: triple-suppression needed for reflection-mock types (CA1051 alone is insufficient because the field isn't an instance field; CA2211 + MA0069 catch the static-mutable variant).

Adjusted prediction model for Sprint 28+: per-file analyzer-error count grows with reflection-mock companion types.

### 2. **`MA0048` forces inline test-helper classes to own files**

Upstream's pattern of putting `class MutantControl` inline at the end of `CoverageCollectorTests.cs` violates Meziantou's MA0048 (file-name must match type-name). Extraction is mechanical and improves discoverability anyway — the mock counterpart is now findable as `MutantControl.cs`.

### 3. **Compile-driven port works fastest on data-collector-style tests**

Sprint 26 (574 LOC helper) needed 4 build-fix cycles. Sprint 27 (198 LOC tests + 45 LOC mock) needed 2 build-fix cycles. The pattern: tests-of-pure-functions port faster than tests-of-mocked-orchestration, even at smaller LOC ratios.

## v2.14.0 progress map

```
[done]    Sprint 27 → CoverageCollectorTests port → v2.14.0   ⭐ MINOR ⭐
[next]    Sprint 28 → VsTextContextInformationTests port (202 LOC) → v2.15.0
[planned] Sprint 29 → VsTestRunnerPoolTests port (727 LOC) → v2.16.0
[planned] Sprint 30+ → MTP / CLI / RegexMutators / Core.UnitTest tranches → v2.17.0–v2.24.0
```

## Recommended next sprint (v2.15.0 candidate)

**Sprint 28: VsTextContextInformationTests port (202 LOC)**
- Inherits from VsTestMockingHelper (Sprint 26 foundation)
- Has 1 direct `IAnalyzerResult` reference (Sprint 25 recherche found this) — needs adapter via `TestHelper.SetupProjectAnalyzerResult` like the helper itself.
- Expected ~10-15 mechanical fixes (similar volume to CoverageCollectorTests).
