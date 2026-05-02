---
current_sprint: "83"
sprint_goal: "Block B start: Baseline batch B (BaselineMutantHelper + S3BaselineProvider, 5 green + 4 skip) → v2.69.0"
branch: "feature/83-baseline-batch-b"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 83 — Baseline batch B (5 grün + 4 skip, Block B start)

## Outcome
- BaselineMutantHelperTests (3 facts) — JsonMutant Location-based source extraction
- S3BaselineProviderTests (2 facts + 4 skipped) — AWS S3 baseline provider; 4 logger-Verify skipped due to [LoggerMessage] source-gen drift (Sprint 72 lesson)
- Total: 5 green + 4 skip
- Dogfood-project: 719 + 21 skip = 740
- 1 build-fix-cycle (line-ending drift in TestResources/ExampleSourceFile.cs — runtime-detect CRLF vs LF + format expected with $-interpolated newline)

## Lessons (NEW)
- **TestResources line-ending drift**: copy-to-output preserves git checkout EOL (Windows CRLF, Linux LF). Tests asserting verbatim multi-line strings need runtime-detect: `var nl = source.Contains("\r\n", Ordinal) ? "\r\n" : "\n"` then interpolate.
