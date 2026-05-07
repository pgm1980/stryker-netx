---
current_sprint: "159"
sprint_goal: "Fix Aisess .slnx mutable-assembly-resolution bug (H2 confirmed): C+B-Kombi filter robustness + log clarity + latent-H1 pre-emptive. Target tag v3.2.11."
branch: "fix/159-slnx-source-project-filter"
started_at: "2026-05-07"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 159 (Aisess `.slnx` Source-Project Filter Fix)

## Sprint Plan

| # | Task | Status | Gates |
|---|------|--------|-------|
| 1 | ADR-039 — Source-project filter behavior in solution-mode (C+B Kombi) | open | doc-only |
| 2 | Fix-1 (proactive validation) — pre-validate filter before AnalyzeAllNeededProjects loop | open | unit test (FluentAssertions) |
| 3 | Fix-1 (zero-match fallback) — warn + retry without filter when mutableProjects=0 | open | unit test |
| 4 | Fix-2 (log clarity) — replace misleading "Analyzing 0 projects" trio | open | snapshot test |
| 5 | Fix-3 (latent H1 pre-emptive) — Stage-2 OrdinalIgnoreCase + Path.GetFullPath | open | unit test (Windows path edge cases) |
| 6 | Fix-4 (integration test fixture) — `samples/AisessLikeSlnxFolders/` with 4-layer DDD-onion + `<Folder>` `.slnx` | open | E2E test |
| 7 | Version bump + CHANGELOG + tag v3.2.11 + GitHub release | open | release notes |

## Hypothesis recap (from PR #250)

- **H2 confirmed**: filter-induced empty `mutableProjects` collection
- **H6 dead**: Roslyn populates `ProjectReferences` correctly (4/4 for test project)
- **H1 latent**: Stage-2 `StringComparer.Ordinal` is fragile on Windows — pre-emptive fix included
- **Aisess workaround**: drop the `"project"` field from `stryker-config.json` (their interim fix while v3.2.11 ships)

## Acceptance criteria

- Aisess `.slnx` setup runs successfully OR fails with a clear, actionable error (no more `Failed to analyze project builds` on a misconfigured filter)
- All Calculator-Tester E2E tests stay green
- New `samples/AisessLikeSlnxFolders/` integration suite covers 4 filter cases:
  1. happy path (source-project filter)
  2. test-project as filter → clear error
  3. non-existent filter → clear error
  4. no filter → all source projects mutated
- 0 warnings, 0 errors with `TreatWarningsAsErrors`
- Semgrep scan green
