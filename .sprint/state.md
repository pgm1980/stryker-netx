---
current_sprint: "5"
sprint_goal: "v2.0.0 Architecture Foundation: 6 ADRs (013-018) + IMutator-hierarchy interface stub + MutationProfile enum (no behavior change)"
branch: "feature/5-v2-architecture-foundation"
started_at: "2026-04-30"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 5 — v2.0.0 Architecture Foundation

**GitHub-Issue:** [#5](https://github.com/pgm1980/stryker-netx/issues/5)
**Base-Tag:** `v1.0.0` (Sprint 4 closed)
**Final-Tag:** none (ADR-only sprint)
**Reference inputs:** `_input/mutation_framework_comparison.md`, `_references/{pitest,cargo-mutants,mutmut}`

## Aktueller Phase-Stand

- [ ] **5.1** — ADR-013 AST/IL hybrid decision
- [ ] **5.2** — ADR-014 Operator hierarchy (Operator → Sub-Operator → Group)
- [ ] **5.3** — ADR-015 SemanticModel-driven mutator infrastructure
- [ ] **5.4** — ADR-016 AssemblyLoadContext Hot-Swap (design only, impl in Sprint 8)
- [ ] **5.5** — ADR-017 Equivalent-Mutant Filtering as first-class layer
- [ ] **5.6** — ADR-018 Mutation Levels as Profiles
- [ ] **5.7** — IMutator-hierarchy interfaces + MutationProfile enum (stubs)
- [ ] **5.8** — Sprint-close + lessons (no tag)

## Sprint-5-DoD

- [ ] 6 ADRs appended to `_docs/architecture spec/architecture_specification.md`
- [ ] IMutator hierarchy + MutationProfile in Stryker.Abstractions
- [ ] dotnet build 0/0
- [ ] dotnet test 27/27 (no behavior change)
- [ ] Sample E2E 100 %
- [ ] Public API: additive only, no breaking changes
- [ ] Semgrep clean
- [ ] Lessons doc
- [ ] No tag
- [ ] Issue #5 closed
- [ ] housekeeping_done=true
