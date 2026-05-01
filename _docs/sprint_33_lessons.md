# Sprint 33 — AssemblyTestServerTests Port

**Tag:** v2.20.0 | 26 [Fact]s grün

## Lessons
- **`InternalsVisibleTo` für Test-Project** muss in production csproj ergänzt werden weil Tests auf `internal AssemblyTestServer` + `internal ITestServer*`-interfaces zugreifen. Sprint 1 cleanup hat upstream-namespace `Stryker.TestRunner.MicrosoftTestPlatform.UnitTest` als IVT, unser Test-Project hat aber `.Tests` Suffix → zweite IVT-Annotation nötig.
- **`CreateClient` API drift**: upstream `(Stream, IProcessHandle, bool)` → unser `(Stream, IProcessHandle, ILogger, string? rpcLogFilePath)`. 4 call-site updates mit `It.IsAny<ILogger>()` + `null` für log path.
- **xUnit1031 + MA0004 file-level suppression** für lange test files mit vielen async pattern.
- **`var (results, timedOut) = ...`** mit unused tuple-element → `var (_, timedOut) = ...` (S1481).

## Roadmap
- Sprint 34: SingleMicrosoftTestPlatformRunnerCoverageTests (488)
- Sprint 35: SingleMicrosoftTestPlatformRunnerTests (1107)
- Sprint 36: TestingPlatformClientTests (640)
