---
current_sprint: "76"
sprint_goal: "ProjectComponents batch A (TestFile + TestCase, 4 green) → v2.62.0"
branch: "feature/76-projectcomponents-batch-a"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 76 — ProjectComponents batch A (4 grün, verified-unported)

## Outcome
- TestFileTests (1 fact) — equality on identical {SyntaxTree, FilePath, Source} + AddTest
- TestCaseTests (1 fact + 1 theory ×2 = 3 facts) — equality on {Id, Name, Node}
- Total: 4 green, 0 skip
- Dogfood-project: 660 + 16 skip = 676
- 1 build-fix-cycle:
  1. Production drift: TestFile.SyntaxTree is now `required init` (Sprint 2 modernization). Tests must construct with SyntaxTree assignment.
  2. Production drift: TestCase moved from `Stryker.Abstractions.Testing` → `Stryker.Core.ProjectComponents.TestProjects` namespace.
