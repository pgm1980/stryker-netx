---
current_sprint: "84"
sprint_goal: "Block B Initialisation pair (ProjectMutator + InitialBuildProcess, 7 green + 4 skip) → v2.70.0"
branch: "feature/84-initialisation-batch-b"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 84 — Initialisation batch B (7 grün + 4 skip, Block B)

## Outcome
- ProjectMutatorTests (1 fact) — uses ProjectAnalysisMockBuilder (Sprint 61) for first time in production!
- InitialBuildProcessTests (6 facts + 4 Windows-only skipped) — IProcessExecutor mock-based dotnet/MSBuild verification
- TargetFrameworkResolutionTests SKIPPED (upstream file is /* commented out */)
- Total: 7 green + 4 skip
- Dogfood-project: 726 + 25 skip = 751
- 1 build-fix-cycle (CS0234 IStrykerOptions namespace + S1186 empty-method skip placeholders + CA1859 IFileSystem field)

## Lessons (NEW)
- **xUnit lacks IgnoreIf**: Upstream MSTest's `[TestMethodWithIgnoreIfSupport] [IgnoreIf(nameof(Is.Unix))]` pattern has no clean xUnit equivalent. Convert to `[Fact(Skip = "Windows-only ...")]` with reason. Add `// S1186` placeholder comment in empty methods.
- **ProjectAnalysisMockBuilder finally productized**: First Initialisation test using Sprint 61's fluent builder. Replaces `TestHelper.SetupProjectAnalyzerResult` for any new Init port — much cleaner.
