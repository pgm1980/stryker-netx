---
current_sprint: "159"
sprint_goal: "Fix Aisess .slnx mutable-assembly-resolution bug (H2 confirmed): C+B-Kombi filter robustness + log clarity + latent-H1 pre-emptive. Target tag v3.2.11."
branch: "fix/159-slnx-source-project-filter"
started_at: "2026-05-07"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 159 closed (Aisess `.slnx` Source-Project Filter Fix — v3.2.11 prep)

## Sprint Plan — final status

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | ADR-039 — Source-project filter behavior in solution-mode (3-Layer Defense) | ✓ done | Inline in `architecture_specification.md` Z. 2647-ff. |
| 2 | Fix-1 (Layer 1 fast-fail) — `ValidateFilterMatchesAnyProject` in `ScanInSolutionMode` | ✓ done | InputException w/ available-projects list, ~10ms feedback |
| 3 | Fix-1 (Layer 2 proactive) — `ApplyProjectFilter` w/ test-project-as-filter detection | ✓ done | New private method, refactored `AnalyzeThisProject` to per-project-only |
| 4 | Fix-1 (Layer 3 fallback) — zero-match safety-net warn + return unfiltered | ✓ done | `LogFilterFallback` Warning |
| 5 | Filter-Match-Semantik BREAKING — substring → exact filename | ✓ done | New `MatchesFilter` helper, `.csproj`-ext tolerance |
| 6 | Fix-2 (log clarity) | ✓ done (subset) | New `LogFilterFallback` covers Layer 3 path; existing trio still emits for legacy paths |
| 7 | Fix-3 (latent-H1 pre-emptive) — Stage-2 OrdinalIgnoreCase + Path.GetFullPath | ✓ done | `ScanProjectReferences` updated |
| 8 | Fix-4 (integration-test fixture) — `samples/AisessLikeSlnxFolders/` w/ 4-layer DDD-Onion + `<Folder>` `.slnx` | ✓ done | Subagent worktree-isolated, 4 new E2E tests grün |
| 9 | Build + Test verify Solution-wide | ✓ done | 0/0 build, 1909/1935 tests green |
| 10 | Semgrep clean | ✓ done | 0 findings on 12 changed files |
| 11 | MEMORY.md + project_sprint159_closed.md | ✓ done | User-level auto-memory updated |
| 12 | Tag v3.2.11 + GitHub Release | ⏳ pending | After PR merge to main (Sprint-Tag-Convention) |

## Verification summary

- Solution-wide build: **0 warnings, 0 errors** (TreatWarningsAsErrors=true)
- All test suites: **1909 / 0 / 26 = 1935** (failures / passing / skipped — skips are pre-existing, documented)
- Semgrep: **0 findings** on `InputFileResolver.cs` + `samples/AisessLikeSlnxFolders/` + `tests/Stryker.E2E.Tests/AisessLikeSlnxFoldersTests.cs`
- Maxential session "sprint-159-adr-039-filter-defense" archived (20 thoughts, 2 branches via `full_integration`)

## Aisess customer impact

After v3.2.11 ships, Aisess Platform Team can:
- Drop the `"project": "Aisess.Tests.csproj"` field from `stryker-config.json` (their interim workaround) and stryker-netx will mutate all 4 source projects automatically, OR
- Keep the `"project"` field but with a **source** project name (e.g. `"Aisess.Infrastructure.csproj"`) and stryker-netx will mutate only that project. If they accidentally pass a test-project name, they get a clear error in <100ms with a migration cue instead of an opaque 6s failure.

## Next steps

1. PR creation (manual user trigger): `gh pr create` against main
2. CI run + review
3. Squash-merge on main (preserves clean Bisect history)
4. Tag `v3.2.11` ON the squash-merge-commit (Sprint-Tag-Convention from CLAUDE.md)
5. GitHub Release with notes pointing to ADR-039 + Aisess bug-report archive
