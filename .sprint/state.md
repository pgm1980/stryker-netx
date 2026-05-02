---
current_sprint: "101"
sprint_goal: "AzureFileShareBaselineProviderTests full upstream port (3 placeholder skips → 7 real green) → v2.87.0"
branch: "feature/101-azurefileshare-full-port"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 101 — AzureFileShareBaselineProviderTests full upstream port

## Outcome — 3 skips → 7 real green
Sprint 93 placeholder had 3 [Fact(Skip)] stubs (misdiagnosed as "Heavy HttpMessageHandler mock").
Real production uses Azure.Storage.Files.Shares.ShareClient/ShareDirectoryClient/ShareFileClient
mocked via Mock.Of pattern — same as upstream. Full port with 4 [Fact]s + 1 [Theory ×3] = 7.
- Net: +7 green, -3 skip, +4 new tests
- Dogfood-project: 949 + 76 skip = 1025

## Production drift (1 mock-method-name)
v2.x `AzureFileShareBaselineProvider.Load` line 45 uses `fileClient.DownloadAsync()`,
upstream test mocked sync `Download(null, default)`. Fixed by mocking `DownloadAsync()`
with `Task.FromResult(...)`.

## Files
- `tests/Stryker.Core.Dogfood.Tests/Baseline/Providers/AzureFileShareBaselineProviderTests.cs`
  (full port, 7 tests, MA0051 suppress on Save_Report multi-mock setup)
