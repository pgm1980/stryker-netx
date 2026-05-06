---
current_sprint: "142"
sprint_goal: "Hotfix v3.1.2 — Bug #9 (--mutation-profile All InvalidCastException) via UoiMutator pre-check + SpanReadOnly disable + global DoNotMutate"
branch: "main"
started_at: "2026-05-06"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 142 closed (v3.1.2 hotfix released)

## Sprint 142 closed cleanly
- Bug #9 Maxential-decided fix-strategy: A+B (Mutator-pre-check + global DoNotMutate, Sprint 23 precedent) + SpanReadOnly disable
- ADR-026 written + 0.8.0 history entry
- 3 new UoiMutator regression tests + 2 reflection-property tests adapted with IntentionallyDisabledMutators-allowlist
- Solution-wide: 2003 tests green, 27 legitimate skips
- Semgrep: 0 findings
- v3.1.2 published to NuGet.org, Banner shows `3.1.2`, `-T` returns `3.1.2`

## Bug #9 Status: GEFIXT
Calculator-tester real-life crash mit `--mutation-profile All` + `InvalidCastException(ParenthesizedExpression → TypeSyntax/SimpleNameSyntax)` ist behoben. Lokale Repro mit `data.Length`-Pattern: war crash, jetzt 61 mutants / score 65.22% / kein Crash. Tester kann mit `dotnet tool update -g dotnet-stryker-netx --version 3.1.2` updaten.

## Open follow-ups (nicht Sprint 142)
- **Sprint 143 (potential)**: Bug #4 (`--version` Tester-Sicht) + Bug #6 (`--reporters` plural Alias) — Tester sieht beide noch als unfixed obwohl Sprint 138/141 die additiv-Variante implementiert haben.
- **Engine type-position-aware Refaktor**: Würde SpanReadOnlySpanDeclarationMutator wieder enable. Eigene ADR + multi-sprint, kein Commitment.

## Cumulative Session-Stats (Sprints 95-142)
- 48 Sprints, 47 Releases
- 8 production-bug-fix Sprints
- 2003 tests grün solution-wide
- Calculator-Tester Bug-Report 1: 8/8 closed (Sprints 138-141)
- Calculator-Tester Bug-Report 2: 1 critical Bug #9 fixed in Sprint 142; #4 + #6 disagreement bleibt offen
