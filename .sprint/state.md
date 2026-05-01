---
current_sprint: "18"
sprint_goal: "Hardening Super-Sprint A: full unit-test coverage for all 52 mutators + base classes. Target ~400-500 tests, >85% line coverage on Mutators namespace. Single Stryker.Core.Tests project. Hybrid SemanticModel strategy (Real CSharpCompilation + null-cast)."
branch: "feature/18-hardening-mutator-unit-tests"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 18 — Hardening Super-Sprint A: Mutator Unit Tests

**GitHub-Issue:** [#18](https://github.com/pgm1980/stryker-netx/issues/18)
**Base-Tag:** `v2.4.0` (Sprint 17 closed)
**Final-Tag:** `v2.5.0`
**Reference:** Sprint 18 Maxential (11 thoughts, 1 branch S1 chosen) + Sprint 18 ToT (12 nodes, 4-branch evaluation, Hybrid chosen).

## Phase plan

- [ ] **Phase 0** — `tests/Stryker.Core.Tests/` project skeleton + `MutatorTestBase` + `MutatorTestHelper` + `SemanticModelHelpers`
- [ ] **Phase 1** — 5 reference-mutator test classes validating helper infrastructure (BinaryExpression / NakedReceiver / RorMatrix / TypeDrivenReturn / GenericConstraint)
- [ ] **Phase 2** — Batch of 10-12 simple-swap mutators (BinaryPattern, RelationalPattern, Boolean, NegateCondition, ConditionalExpression, AssignmentExpression, PrefixUnary, PostfixUnary, NullCoalescingExpression, IsPatternExpression, BinaryExpression — already in Phase 1)
- [ ] **Phase 3** — Batch of 10-12 string + collection mutators (String, StringEmpty, StringMethod, StringMethodToConstant, InterpolatedString, ArrayCreation, CollectionExpression, Initializer, ObjectCreation, Linq, Math)
- [ ] **Phase 4** — Batch of 10-12 statement + special mutators (Block, Statement, Checked, Regex, ConstructorNull, MatchGuard, WithExpression, AsyncAwait, AsyncAwaitResult, ConfigureAwait, DateTime)
- [ ] **Phase 5** — Final batch (DateTimeAddSign, ExceptionSwap, SpanMemory, AsSpanAsMemory, SpanReadOnlySpanDeclaration, ConstantReplacement, InlineConstants, Aod, RorMatrix-already-Phase-1, Uoi, NakedReceiver-already-Phase-1, GenericConstraintLoosen, GenericConstraint-already-Phase-1, TaskWhenAllToWhenAny, SwitchArmDeletion) — plus 4 type-aware (TypeDrivenReturn-already-Phase-1, MemberVariable, ArgumentPropagation, MethodBodyReplacement)
- [ ] **Phase 6** — Coverage measurement + lessons doc + tag `v2.5.0`

## Sprint-18-DoD

- [ ] tests/Stryker.Core.Tests/ project created
- [ ] MutatorTestBase + helpers
- [ ] 52 per-mutator test classes
- [ ] Base-class tests
- [ ] dotnet build 0/0
- [ ] dotnet test all-green (~400-500+ new tests)
- [ ] Sample E2E 100% under default profile (zero behavior change)
- [ ] Semgrep clean
- [ ] Coverage >85% line on Stryker.Core.Mutators.*
- [ ] README + MIGRATION updated
- [ ] Lessons doc
- [ ] Tag `v2.5.0`
- [ ] GitHub release published
- [ ] Branch merged into main
- [ ] Issue #18 closed
- [ ] housekeeping_done=true
