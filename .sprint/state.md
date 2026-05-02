---
current_sprint: "62"
sprint_goal: "CsharpMutantOrchestratorTests subset port (drift-risk triage) → v2.48.0"
branch: "feature/62-csharp-mutant-orchestrator-tests"
started_at: "2026-05-02"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: false
---
# Sprint 62 — CsharpMutantOrchestratorTests subset port (drift-risk triage)

## Outcome
- Maxential branch B "triage-by-drift-risk" applied to upstream's largest single test file (1968 LOC, 95 [TestMethod]s)
- Ported MutantOrchestratorTestsBase + 10 green + 5 explicitly skipped
  - 7 bucket-1 (source==expected, no-mutation-expected): all green
  - 3 bucket-2 (single-mutation, low-drift-risk pattern): all green
  - 5 bucket-3 (multi-mutation hardcoded IDs): skipped with uniform reason
- Dogfood-project total: 421 grün + 14 skip = 435
- Solution-wide: 1237 green + 32 skip ohne E2E
- Semgrep: 0 findings on Sprint-62 files

## Lessons (NEW)
- **Drift-risk triage** for hardcoded-IsActive(N) tests: bucket-1 (no mutation) is robust to mutator-set drift; bucket-2 (single mutation) often works because the FIRST-firing mutator is stable; bucket-3 (multi-mutation hardcoded IDs) is brittle and best deferred to a "rewrite-as-structural-assertions" sprint
- **NormalizeWhitespace + ToFullString** is a viable stand-in for Shouldly's ShouldBeSemantically — strict node-shape match, no trivia
- **Empirical-validation-first** for foundation/risky ports: write base helpers + ONE simplest test, run, and only then scale up
