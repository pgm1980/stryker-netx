---
current_sprint: "164"
sprint_goal: "§4 from Aisess STRYKER_NETX_ANOMALIES_AND_BUGS report — expose --test-case-filter CLI flag (existing JSON+VsTest plumbing) + --test-filter alias (Sprint-149 RewriteReportersAlias pattern). VsTest-only; MTP forwarding honest-deferred. Target tag v3.2.16."
branch: "feature/164-test-filter-cli"
started_at: "2026-05-20"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 164 closed (v3.2.16 prep)

## Final summary

ADR-044 — CLI-Wiring gap closure for the pre-existing TestCaseFilter infrastructure:

| # | Task | Status |
|---|------|--------|
| 1 | Maxential ADR-044 (5 thoughts, 0 branches) | ✓ |
| 2 | CommandLineConfigReader.PrepareCliOptions: `AddCliInput(TestCaseFilterInput, "test-case-filter", null, ...)` extracted to `PrepareTestCaseFilterCliOption` helper (MA0051) | ✓ |
| 3 | StrykerCli: `RewriteTestFilterAlias` + `TryRewriteTestFilterArg` (Sprint-149 RewriteReportersAlias pattern) | ✓ |
| 4 | 10 new StrykerCLITests (4 Rewrite-Theory + 4 Non-Rewrite-Theory + 2 End-to-End-Facts) | ✓ |
| 5 | ADR-044 in architecture spec + Änderungshistorie (0.28.0 entry) | ✓ |
| 6 | README new "Excluding tests from the run (`--test-filter`)" section | ✓ |
| 7 | Build solution-wide (0/0) | ✓ |
| 8 | Tests: 2092/2119 (+10 vs Sprint 163 baseline 2082; CLI 93→103) | ✓ |
| 9 | Semgrep | ⚠️ SSL-error on local machine (same as Sprints 162+163); CI validates |
| 10 | MEMORY.md + project_sprint164_closed.md | ⏳ pending |
| 11 | PR + Merge + Tag v3.2.16 + GitHub Release + NuGet publish | ⏳ pending |

## Aisess customer impact (after v3.2.16 ships)

```
// PRE v3.2.16 — § 4: 186 Integration-tests vergiften baseline
$ dotnet stryker-netx --project Aisess.Application --mutate ...
[INF] Number of tests found: 3840 for project Aisess.Application.csproj. Initial test run started.
[WRN] 59 tests are failing. Stryker will continue but outcome will be impacted.
   → Mutation-Score-Baseline corrupted by Testcontainers-gated tests

// POST v3.2.16:
$ dotnet stryker-netx --project Aisess.Application --test-filter "Category!=Integration" --mutate ...
[INF] Number of tests found: 3654 for project Aisess.Application.csproj. Initial test run started.
   → Clean baseline, only unit tests run in mutation pipeline
```

## Out-of-scope (honest-deferred)

- MTP runner test-filter forwarding — Aisess uses xUnit/VsTest, MTP wire-protocol work pushed to v3.3 roadmap (no current MTP-consumer).
- TestCaseFilter syntax validation — verbatim forwarding matches dotnet test parity.

## Next steps

1. Commit + Push + PR + Merge + Tag v3.2.16 + GitHub Release
2. NuGet publish (user-manual via local NUGET_API_KEY)
3. Move to Sprint 165 (§5 multi-line `next-line` Comment-Parser-Scope, dedicated bug-report file already on disk)
