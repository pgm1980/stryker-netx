---
current_sprint: "36"
sprint_goal: "MTP TestingPlatformClientTests → v2.23.0 (closes MTP track)"
branch: "feature/36-mtp-testing-platform-client-port"
started_at: "2026-05-02"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Sprint 36 — 22/22 grün; MTP track komplett (Sprints 30-36)

## Outcome
- MTP-project total: 136 grün + 6 skip = 142 tests
- 1 build-fix-cycle (Linq-import + CA1822 on JSON-RPC reflection-target)
- Production API drift (Sprint 33 pattern repeats): ctor `(JsonRpc, TcpClient, IProcessHandle, bool)` upstream → `(JsonRpc, TcpClient, IProcessHandle, ILogger, string? rpcLogFilePath = null)` — 9 call-sites updated
