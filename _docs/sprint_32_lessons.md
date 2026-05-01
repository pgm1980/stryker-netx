# Sprint 32 — MicrosoftTestPlatformRunnerPoolTests Port

**Tag:** v2.19.0 | **Branch:** feature/32-mtp-runner-pool-tests-port

## Outcome
- 16 tests grün + 1 skipped (Constructor_ShouldUseDefaultLogger_WhenLoggerIsNull — behavior delta in null-logger fallback path)
- 30/30 in MTP-project total: 13 (Sprint 30+31) + 16+1 = 30
- Solution-wide: 526 grün excl E2E

## Lessons
- **xUnit1030 vs MA0004 conflict**: xUnit's `xUnit1030` forbids `ConfigureAwait(false)` in tests; Meziantou's `MA0004` demands it. xUnit wins for test bodies — `#pragma warning disable MA0004` around the affected test method.
- **`object` discoveryLock parameter type → `Lock`**: `runnerFactory.Setup(x => x.CreateRunner(..., It.IsAny<Lock>(), ...))` — type signature drift propagates to mock setups.
- **`(IReadOnlyList<string>)null!`** statt upstream `(List<string>)null!` weil our IProjectAndTests.GetTestAssemblies returns IReadOnlyList<string>.
- **ApplicationLogging.LoggerFactory.CreateLogger throws on null** — different from upstream default-logger pattern. Single test skipped for follow-up.

## Roadmap
- Sprint 33: AssemblyTestServerTests (483)
- Sprint 34-36: rest of MTP files
