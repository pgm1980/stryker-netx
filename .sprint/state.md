---
current_sprint: "162"
sprint_goal: "Quick-wins from Aisess STRYKER_NETX_ANOMALIES_AND_BUGS report: §6 (`all,Boolean` parser regression from Sprint 160) + §3 (short-name `--project` resolver) + intake of 2 new bug-report files. Target tag v3.2.14."
branch: "fix/162-parser-allcomma-and-shortname-project"
started_at: "2026-05-20"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 162 closed (v3.2.14 prep)

## Final summary

ADR-042 — 2 backwards-compatible quick-fixes + 2 bug-report intakes:

| # | Task | Status |
|---|------|--------|
| 1 | Maxential ADR-042 (3 thoughts, 0 branches) | ✓ |
| 2 | §6 fix: `all` wildcard within comma-list (CommentParser.cs `ParseMutatorList` helper extracted, MA0051 cap respected) | ✓ |
| 3 | §3 fix: short-name `--project` resolver in `ResolveMultiReferenceCase` (filename-match via Sprint-159 helper + better ambiguity error) | ✓ |
| 4 | 4 new CommentParser tests (1 [Fact] + 1 [Theory] × 3 inlines) | ✓ |
| 5 | Bug-report intake: STRYKER_NETX_ANOMALIES_AND_BUGS.md + dedicated §5 file | ✓ |
| 6 | Build solution-wide (0/0) | ✓ |
| 7 | Tests: 1907/1933 (+4 vs Sprint 161 baseline 1903) | ✓ |
| 8 | Semgrep | ⚠️ SSL-error on local machine (`semgrep.dev` unreachable); will be validated by CI |
| 9 | MEMORY.md + project_sprint162_closed.md | ✓ |
| 10 | PR + Merge + Tag v3.2.14 + GitHub Release + NuGet publish | ⏳ pending |

## Aisess customer impact (after v3.2.14 ships)

```
// PRE v3.2.14 — § 6 silent ERR-log:
// Stryker disable next-line all,Boolean : reason
   → ERR-log "Unknown mutator kind 'all'"
   → "Boolean" is the only kind disabled (not "all" as user intended)

// POST v3.2.14:
// Stryker disable next-line all,Boolean : reason
   → no ERR-log
   → all enum values disabled (union-semantik: all ∪ {Boolean} = all)

// PRE v3.2.14 — § 3 misleading error:
dotnet stryker-netx --project Aisess.Application
   → "Test project contains more than one project reference. Please set the project option..."
   → User confused: they DID set --project!

// POST v3.2.14:
dotnet stryker-netx --project Aisess.Application
   → MatchesFilter("Aisess.Application", "...Aisess.Application.csproj") → match → proceed
   → Or if ambiguous: "Project filter 'X' is ambiguous — multiple references match. Please supply a more specific value or an absolute .csproj path."
```

## Next steps

1. Commit + Push + PR + Merge + Tag v3.2.14 + GitHub Release
2. NuGet publish (user-manual via local NUGET_API_KEY)
3. Move to Sprint 163 (§2 silent-hang)
