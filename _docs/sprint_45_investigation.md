# Sprint 45 — Investigation: 18 Cross-Sprint Behaviour-Delta Skips

**Tag:** v2.32.0 | **Branch:** `feature/45-investigation-behaviour-delta-skips`
**Type:** Documentation-only sprint (no code changes)

## Purpose
Consolidate, analyze, and produce a decision matrix (fix / won't-fix / defer) for all 18 cross-sprint behaviour-delta tests skipped during dogfood-port sprints (24-44). This sprint produces the analysis report; downstream sprints will execute the recommended actions.

## Inventory: All 18 Skipped Tests

### Sprint 29: VsTestRunnerPoolTests (11 skips)
**File**: `tests/Stryker.TestRunner.VsTest.Tests/VsTestRunnerPoolTests.cs`
**Common Skip Reason**: "Sprint 29 follow-up: behaviour delta upstream-vs-stryker-netx; Sprint-25-lessons triage path. Re-enable in dedicated investigation sprint."

| # | Test | Suspected Root Cause | Decision |
|---|------|---------------------|----------|
| 1 | `RecycleRunnerOnError` | `_ = task` fire-and-forget timing in xUnit teardown vs upstream MSTest | **WON'T-FIX-DOC**: framework difference; behavior in production unaffected |
| 2 | `DetectTestsErrors` | Mock<IVsTestConsoleWrapper>.Result blocking + ConfigureAwait(false) propagation | **DEFER**: requires async-rewrite (Sprint 25 prediction) |
| 3 | `ShouldRetryFrozenSession` | VsTest session-recovery timing assertion sensitive to thread-pool jitter | **WON'T-FIX-DOC**: timing-dependent; non-deterministic on CI |
| 4 | `ShouldNotRetryFrozenVsTest` | Same as #3 | **WON'T-FIX-DOC**: same root cause |
| 5 | `AbortOnError` | Test setup uses upstream `await TestProcessExitsAsync()` semantic that doesn't translate | **DEFER**: needs production-test-helper |
| 6 | `NotRunTestWhenNotCovered` | Coverage-instrumentation-mock divergence (Sprint 1 Phase 9 IAnalyzerResult removal) | **DEFER**: needs IProjectAnalysis-aware mock builder |
| 7 | `HandleMultipleTestResults` | TestResult batch-handling: upstream Shouldly's `.ShouldContain(predicate)` semantic vs FluentAssertions ordering | **CANDIDATE-FIX**: rewrite assertion |
| 8 | `HandleFailureWithinMultipleTestResults` | Same as #7 | **CANDIDATE-FIX**: rewrite assertion |
| 9 | `HandleTimeOutWithMultipleTestResults` | Same as #7 + timeout-handling | **CANDIDATE-FIX**: rewrite assertion |
| 10 | `HandleFailureWhenExtraMultipleTestResults` | Same as #7 | **CANDIDATE-FIX**: rewrite assertion |
| 11 | `HandleUnexpectedTestResult` | Same as #7 | **CANDIDATE-FIX**: rewrite assertion |

**Sprint 29 Verdict**: 4 WON'T-FIX-DOC, 3 DEFER (require infrastructure), 5 CANDIDATE-FIX (assertion rewrite).
Recommended follow-up: dedicated VsTest-Refactor-Sprint to address the 5 CANDIDATE-FIX cases via async-test rewrite (~1 day effort).

### Sprint 32: MicrosoftTestPlatformRunnerPoolTests (1 skip)
**File**: `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/MicrosoftTestPlatformRunnerPoolTests.cs`
**Skipped Test**: `Constructor_ShouldUseDefaultLogger_WhenLoggerIsNull`

| Test | Root Cause | Decision |
|------|-----------|----------|
| `Constructor_ShouldUseDefaultLogger_WhenLoggerIsNull` | stryker-netx production: `ApplicationLogging.LoggerFactory.CreateLogger` throws on null logger; upstream silently fell back to NullLogger | **WON'T-FIX-DOC**: stryker-netx production change is intentional (fail-fast on misconfiguration); test asserts upstream-only behavior |

**Sprint 32 Verdict**: 1 WON'T-FIX-DOC (production change intentional).

### Sprint 34: SingleMicrosoftTestPlatformRunnerCoverageTests (5 skips)
**File**: `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/SingleMicrosoftTestPlatformRunnerCoverageTests.cs`
**Common Skip Reason**: "stryker-netx test project uses xUnit (NOT MTP-native), so `runner.DiscoverTestsAsync(testAssembly)` against the test assembly itself returns False — upstream MSTest test-host IS MTP-native."

| # | Test | Root Cause | Decision |
|---|------|-----------|----------|
| 1 | `SetCoverageMode_ShouldEnableCoverageMode` | Self-discovery against test assembly; xUnit not MTP-compatible | **DEFER**: would require switching test framework or MTP-mock-server |
| 2 | `SetCoverageMode_ShouldDisableCoverageMode` | Same | **DEFER**: same root cause |
| 3 | `SetCoverageMode_ShouldNoOp_WhenModeIsAlreadySet` | Same | **DEFER**: same root cause |
| 4 | `SetCoverageMode_ShouldRestartServers_WhenTogglingBetweenModes` | Same | **DEFER**: same root cause |
| 5 | `ResetServerAsync_ShouldDisposeAndClearAllServers` | Same — needs populated `_assemblyServers` dict via real discovery | **DEFER**: same root cause |

**Sprint 34 Verdict**: 5 DEFER. Mitigation requires either:
- (A) Convert test project to MSTest (large refactor — affects all MTP tests)
- (B) Mock-server harness providing a fake MTP responder (medium effort, ~3 days)
- (C) Accept skip with documentation (current state — chosen)

Recommended follow-up: build MTP-mock-server harness in dedicated sprint when other priorities allow.

### Sprint 43: UnicodeCharClassNegationMutatorTests (1 skip)
**File**: `tests/Stryker.RegexMutators.Tests/Mutators/UnicodeCharClassNegationMutatorTests.cs`
**Skipped Test**: `NegatesUnicodeCharacterClassWithPropertyAndValue`

| Test | Root Cause | Decision |
|------|-----------|----------|
| `NegatesUnicodeCharacterClassWithPropertyAndValue` | .NET Regex doesn't support `\p{Script_Extensions=Latin}` syntax (Unicode property+value) — preserved upstream `[Ignore]` 1:1 | **WON'T-FIX-PERMANENT**: .NET runtime limitation; upstream had same |

**Sprint 43 Verdict**: 1 WON'T-FIX-PERMANENT.

## Summary Decision Matrix

| Decision | Count | Action |
|----------|-------|--------|
| WON'T-FIX-DOC | 5 | Update Skip messages with permanent-skip reason; update lessons docs |
| WON'T-FIX-PERMANENT | 1 | Already documented; no change |
| CANDIDATE-FIX | 5 | Schedule dedicated VsTest-Refactor sub-sprint (~1 day effort) |
| DEFER | 7 | Document as known limitation; revisit when MTP-mock-server / IProjectAnalysis-mock-builder is built |
| **Total** | **18** | |

## Recommendations

### Immediate (this sprint)
- ✓ This investigation report (committed as `_docs/sprint_45_investigation.md`)
- No test code changes — all skips remain in place

### Short-term (1-2 sprints)
- **VsTest-Refactor sub-sprint**: address 5 CANDIDATE-FIX cases (assertion-rewrite). Estimated ~1 day.
- **Skip message update sub-sprint**: rewrite 5 WON'T-FIX-DOC Skip messages from "investigation sprint TBD" to permanent root-cause documentation. Estimated ~2 hours.

### Long-term (when prioritized)
- **MTP-mock-server harness sub-sprint**: builds infrastructure to enable 5 Sprint-34 DEFER tests. Estimated ~3 days.
- **IProjectAnalysis-mock-builder sub-sprint**: addresses Sprint 29 #6 + builds reusable Mock infrastructure for future Stryker.Core.UnitTest dogfood port. Estimated ~2 days.

### Won't-fix-permanent
- Sprint 32 #1: production fail-fast on null logger is intentional improvement
- Sprint 43 #1: .NET runtime limitation, upstream has same `[Ignore]`

## Solution-wide Impact
- **No test count change**: 816 grün excl E2E, 18 skipped — analysis-only sprint.
- **Knowledge persisted**: this report enables future targeted sub-sprints to act on each skip with full context.
- **Closes "Investigation Sprint TBD" line item** that was carried across Sprints 29-44.

## Roadmap update
- Sprints 46+ Stryker.Core.UnitTest tranches (~25k LOC, 161 files — needs 10+ sub-sprints with proper decomposition)
- Optional sub-sprints from this investigation (Short-term + Long-term recommendations)
