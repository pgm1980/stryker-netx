# Sprint 36 — TestingPlatformClientTests Port (closes MTP track)

**Tag:** v2.23.0 | **Branch:** `feature/36-mtp-testing-platform-client-port`

## Outcome
- **22/22 grün, zero skips**
- Closes the MTP dogfood track (Sprints 30-36 = 7 sprints).
- 640 LOC upstream → port complete.
- MTP-project total: 136 grün + 6 skip = 142 tests.
- Solution-wide: 612 grün excl E2E (likely; aggregated count varies by run).
- 1 build-fix-cycle (3 errors all trivial: `using System.Linq` + CA1822 suppress on JSON-RPC reflection-target method).

## Major Production API drift
**TestingPlatformClient ctor signature** (Sprint 33 lesson surfaces again on a different class):
- Upstream: `(JsonRpc, TcpClient, IProcessHandle, bool)` — 4 args, last bool unknown semantics
- stryker-netx: `(JsonRpc, TcpClient, IProcessHandle, ILogger, string? rpcLogFilePath = null)` — 5 args, ILogger required, optional rpc-log-file path

Mechanical fix: 9 call sites updated to use `NullLogger.Instance` for the logger arg, drop the `false` bool entirely. The `string? rpcLogFilePath` defaults to `null` so no change there.

## Lessons (NEW)
- **JSON-RPC reflection-target methods need explicit CA1822 suppress**: methods like `FakeTestServer.Initialize(InitializeRequest)` are called via JSON-RPC reflection. They appear unused (no `this.` access) so CA1822 fires. They MUST be instance methods because StreamJsonRpc binds them on the target instance — but the analyzer has no way to know that. Suppress with `[SuppressMessage("Performance", "CA1822", Justification = "JSON-RPC reflection requires instance method")]`.
- **`TelemetryPayload(...)` needs `StringComparer.Ordinal`**: same MA0002 trip. The constructor takes `Dictionary<string, object>`, so the inner dictionary must use `StringComparer.Ordinal`.
- **`using System.Linq` for `.First()` on `ConcurrentBag<T>`**: ConcurrentBag inherits from IEnumerable<T> but `First()` is the LINQ extension. Easy miss after MSTest port.

## MTP track complete (Sprints 30-36)
| Sprint | File | Tests grün | Tests skipped | Tag |
|--------|------|-----------|---------------|-----|
| 30 | RpcJsonSerializerOptionsTests + TestableRunner foundation | 2 | 0 | v2.17.0 |
| 31 | DefaultRunnerFactoryTests + ResponseListenerTests | 11 | 0 | v2.18.0 |
| 32 | MicrosoftTestPlatformRunnerPoolTests | 16 | 1 | v2.19.0 |
| 33 | AssemblyTestServerTests | 26 | 0 | v2.20.0 |
| 34 | SingleMicrosoftTestPlatformRunnerCoverageTests | 8 | 5 | v2.21.0 |
| 35 | SingleMicrosoftTestPlatformRunnerTests | 51 | 0 | v2.22.0 |
| 36 | TestingPlatformClientTests | 22 | 0 | v2.23.0 |
| **Total** | **all 7 MTP files** | **136** | **6** | **closing** |

## Roadmap
- **Sprint 37**: CLI.UnitTest (size TBD, recherche needed)
- **Sprint 38**: RegexMutators.UnitTest (size TBD)
- **Sprint 39+**: Stryker.Core.UnitTest tranches (5 — largest module)
- Investigation Sprint TBD: 17 cross-sprint behaviour-delta skips
