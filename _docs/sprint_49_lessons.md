# Sprint 49 — Core.Mutators Batch B2 (BinaryExpression + RegexMutator + NullCoalescing)

**Tag:** v2.36.0 | **Branch:** `feature/49-core-mutators-batch-b2`

## Outcome
- **35 new green** across 3 mutator test files.
- Dogfood-project total: 164 green + 2 skip = 166.
- Solution-wide: 980 green excl E2E.
- 1 build-fix-cycle (MA0006 string.Equals + ApplicationLogging seeding).

## Lessons (NEW)
- **`TestBase` inheritance restored for logger-using production code**: `RegexMutator` ctor calls `ApplicationLogging.LoggerFactory.CreateLogger<RegexMutator>()` — without `: TestBase` (which seeds the factory) → `ArgumentNullException: factory`. Re-add `: TestBase` for affected tests. (Sprint 46's "drop `: TestBase`" was over-broad; refine to: drop ONLY when test doesn't construct logger-init-needing production type.)
- **MA0006 string-equals-with-comparison**: `arg.NameColon?.Name.Identifier.Text == "pattern"` → `string.Equals(arg.NameColon?.Name.Identifier.Text, "pattern", StringComparison.Ordinal)`.
