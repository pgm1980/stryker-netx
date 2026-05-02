# Sprint 48 — Stryker.Core.Mutators Batch B1 (Abstract + String)

**Tag:** v2.35.0 | **Branch:** `feature/48-core-mutators-batch-b1`

## Outcome
- **14 new green** (Abstract 5 + StringMutator 9).
- Dogfood-project total: 129 green + 2 skip = 131.
- Solution-wide: 945 green excl E2E.
- 1 build-fix-cycle (S1939 + MA0025 trivial).
- Scope reduced mid-sprint per Sprint 25 lesson: planned 5 files, delivered 2 — remaining 11 mutator-test files (BinaryExpression, RegexMutator, NullCoalescing, AssignmentStatement, StringEmpty, Linq, IsPattern, Block, Math, CollectionExpression, TestMutator) deferred to Sprints 49+ given growing context.

## Files Ported
- AbstractMutatorTests (114 LOC, 5 tests — exercises MutatorBase&lt;T&gt; via custom ExampleMutator)
- StringMutatorTests (121 LOC, 9 tests including Theory)

## Lessons (NEW)
- **S1939 redundant-interface-implementation**: `MutatorBase<T>` already implements `IMutator`; remove `, IMutator` from inheritance list.
- **MA0025 NotImplementedException → NotSupportedException** with comment-as-Justification: established in Sprint 26, applies uniformly to test-marker exceptions.

## Scope Decision (mid-sprint)
Original Sprint 48 scope: 5 medium-tier mutator tests. After delivering 2 and assessing remaining context budget, narrowed to 2 files. Honest scope-reduction over ambitious-fail (Sprint 25 lesson). The remaining 11 mutator test files become Sprint 49+.

## Roadmap
- **Sprint 49**: BinaryExpression + RegexMutator + NullCoalescing (3 files, ~400 LOC)
- **Sprint 50+**: AssignmentStatement + StringEmpty + Linq + IsPattern + Block + Math + CollectionExpression + TestMutator helper
- **Sprint 51+**: Options + Reporters + Initialisation + remaining (per `_docs/sprint_46_decomposition.md`)
