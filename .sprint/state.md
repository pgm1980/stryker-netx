---
current_sprint: "12"
sprint_goal: "Greenfield .NET-specific operators (Async/Await, DateTime, ExceptionSwap, Span/Memory, GenericConstraint) + README v2 + Migration Guide v1->v2 -> v2.0.0 (production)"
branch: "feature/12-v2-greenfield-release"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 12 — Greenfield .NET-specific operators + Release

**GitHub-Issue:** [#12](https://github.com/pgm1980/stryker-netx/issues/12)
**Base-Tag:** `v2.0.0-rc.1` (Sprint 11 closed)
**Final-Tag:** `v2.0.0` (production)
**Reference inputs:** `_input/mutation_framework_comparison.md` §5 (greenfield .NET-specific) + `_docs/sprint_11_lessons.md`

## Aktueller Phase-Stand

- [x] **12.1** — `AsyncAwaitMutator` (`await x → x.GetAwaiter().GetResult()`) — Stronger | All
- [x] **12.2** — `DateTimeMutator` (`DateTime.UtcNow → DateTime.MinValue/MaxValue`) — Stronger | All
- [x] **12.3** — `ExceptionSwapMutator` (`throw new ArgumentException → throw new InvalidOperationException`) — All only
- [x] **12.4** — `SpanMemoryMutator` (`span.Slice(a,b) → span.Slice(0,b) / span.Slice(a, span.Length-a)`) — Stronger | All
- [x] **12.5** — `GenericConstraintMutator` (drop `where T : class` constraint) — All only
- [x] **12.6** — Wire all 5 into `DefaultMutatorList`
- [x] **12.7** — README.md v2 (root-level rewrite)
- [x] **12.8** — `MIGRATION-v1-to-v2.md`
- [x] **12.9** — Build/Test/E2E green
- [x] **12.10** — Sprint-close + lessons + tag `v2.0.0` + GitHub release + merge to main

## Sprint-12-DoD

- [x] 5 new greenfield mutator files
- [x] All carry `[MutationProfileMembership]`
- [x] All wired into `DefaultMutatorList`
- [x] README.md v2 written (operator catalogue table, profile flag, engine flag)
- [x] MIGRATION-v1-to-v2.md written (zero breaking changes for default; how to opt-in)
- [x] dotnet build 0/0
- [x] dotnet test 27/27
- [x] Sample E2E 100% under default profile
- [x] Semgrep clean
- [x] Lessons doc
- [x] Tag `v2.0.0` (production — no -preview/-rc suffix)
- [x] GitHub release created with full v1->v2 changelog
- [x] Branch merged into main
- [x] Issue #12 closed
- [x] housekeeping_done=true
