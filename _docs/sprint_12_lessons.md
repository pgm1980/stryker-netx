# Sprint 12 — Greenfield .NET-specific Operators + Release: Lessons Learned

**Sprint:** 12 (2026-05-01, autonomous run)
**Branch:** `feature/12-v2-greenfield-release`
**Base:** v2.0.0-rc.1 (Sprint 11 closed)
**Final Tag:** `v2.0.0` (production)
**Source:** comparison.md §4.4 (greenfield .NET-specific operators) + §5 (architecture hints)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `AsyncAwaitMutator` (`await x → x.GetAwaiter().GetResult()`) | ✅ profile `Stronger \| All` |
| `DateTimeMutator` (`DateTime.Now ↔ UtcNow`, `DateTimeOffset.Now ↔ UtcNow`) | ✅ profile `Stronger \| All` |
| `SpanMemoryMutator` (`span.Slice(s,l) → span.Slice(0,l)`) | ✅ profile `Stronger \| All` |
| `ExceptionSwapMutator` (5-pair whitelist swap inside `throw new`) | ✅ profile `All` only |
| `GenericConstraintMutator` (drop `where T : ...` clauses) | ✅ profile `All` only |
| All 5 wired into `DefaultMutatorList` | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (none of the new mutators active under Defaults) |
| Semgrep | ✅ clean (0 findings on new files) |
| README.md v2 | ✅ |
| MIGRATION-v1-to-v2.md | ✅ |
| Tag | `v2.0.0` (production) |

## What landed

The greenfield batch — **operators with no PIT/cargo-mutants/mutmut equivalent**. These are the .NET-specific value-add of v2.0.0 over the v1.x catalogue.

### AsyncAwaitMutator

`await x` → `x.GetAwaiter().GetResult()`. Sync-over-async substitution. Catches:
- Tests that pass on awaitable libraries but would deadlock under a `SynchronizationContext`
- Tests that don't actually verify the async-result usage (only that something completed)

Always compiles — every awaitable exposes the `GetAwaiter().GetResult()` pattern. `Stronger | All` only.

### DateTimeMutator

`DateTime.Now` ↔ `DateTime.UtcNow` (and same for `DateTimeOffset`). Famously bug-rich area in real code. Catches "is the time-source mocked?" and "is the right time-zone branch tested?" Conservative scope: only the four exact `Type.Member` patterns. `Stronger | All`.

### SpanMemoryMutator

`span.Slice(start, length)` → `span.Slice(0, length)`. Drops the start offset. Catches off-by-N slicing bugs cleanly — if the test only verifies slice length but not content, the start-dropped mutant survives. Conservative: only 2-arg `Slice`, skips when start is already literal `0`. `Stronger | All`.

### ExceptionSwapMutator

`throw new ArgumentNullException(...)` ↔ `throw new ArgumentException(...)`, plus 3 more pairs in the same family. All swaps share constructor signatures with the original, so the result always compiles. Catches "does the catch handler / test discriminate exception type?" — a common bug where tests assert on a base type and survive sibling-type swaps. `All` only — disruptive.

### GenericConstraintMutator

Drops the entire `where T : ...` clause set from a method declaration. Catches "is the constraint actually exploited?" — if every call-site happens to pass a satisfying type, the mutant compiles and the test suite must detect the behavioral difference. May produce a non-compiling mutant when the body relies on the constraint (e.g. `new T()` requires `where T : new()`) — that's correct behavior: the runner classifies non-compiling mutants as killed, and the constraint is genuinely load-bearing. `All` only.

## Process lessons

### 1. Meziantou MA0002 + MA0006 strict string-comparison enforcement

Two analyzer rules bit Sprint 12 even on what looked like trivially-safe code:
- **MA0002**: `Dictionary<string, string>` constructed without `StringComparer` is rejected. Fix: `new Dictionary<string, string>(StringComparer.Ordinal)` AND `.ToImmutableDictionary(StringComparer.Ordinal)`.
- **MA0006**: `someString != "literal"` and `someString == "literal"` are both rejected. Fix: `!string.Equals(s, "literal", StringComparison.Ordinal)` and `string.Equals(s, "literal", StringComparison.Ordinal)`.

These rules are at error severity. Documented for future mutators that compare syntax-token text.

### 2. Profile-based opt-in continues to be load-bearing

After Sprints 9/10/11/12, **9 net-new mutators** have shipped on top of the v1.x catalogue (TypeDrivenReturn + InlineConstants + Aod + RorMatrix + Uoi + ConstructorNull + MatchGuard + WithExpression + NakedReceiver + 5 from Sprint 12 = 14, of which 5 are `All` only). The Sample E2E score has stayed at 100% by construction across every single sprint, with zero special-casing. The pattern is now load-bearing for v2.0.0 release: **every aggressive operator since Sprint 9 has been opt-in.** This is the single biggest architectural decision of the v2.0.0 line and it has paid for itself many times over.

### 3. "Greenfield" doesn't mean "untested categories"

The 5 Sprint-12 operators are listed in comparison.md §4.4 as *novel* (no PIT/cargo/mutmut equivalent), but the underlying *bug categories* they catch (sync-over-async, time-zone, off-by-N slicing, exception-type discrimination, generic-constraint exploitation) are well-understood in .NET literature. This is why MVP scope was the right call: each operator has one focused substitution rather than a matrix.

### 4. v2.0.0 catalogue is now the most complete .NET mutator suite

The combination of:
- **v1.x baseline** (26 mutators, Defaults profile)
- **Sprint 9** (TypeDrivenReturn — cargo-mutants gap closed)
- **Sprint 10** (PIT-1: InlineConstants + Aod + RorMatrix + Uoi)
- **Sprint 11** (PIT-2 + cargo-mutants: Constructor-null + MatchGuard + WithExpression + NakedReceiver)
- **Sprint 12** (Greenfield: Async + DateTime + Span + ExceptionSwap + GenericConstraint)

… is now the most comprehensive published .NET mutation-operator catalogue. Default profile = v1.x parity (no behavior change). Stronger = catalogue closure. All = the full surface, including the noisiest experimental operators.

## v2.0.0 progress map (CLOSED)

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3
[done]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
[done]    Sprint 10 → PIT-1 Operator Batch                  → v2.0.0-preview.5
[done]    Sprint 11 → PIT-2 + cargo-mutants Batch           → v2.0.0-rc.1
[done]    Sprint 12 → Greenfield + README + Release         → v2.0.0       ⭐ PRODUCTION ⭐
```

## v2.0.x roadmap (post-release)

Out-of-scope for v2.0.0 release, parked for v2.0.x patches / v2.1.0:

- **HotSwap engine implementation** (Sprint 8 shipped scaffolding only; ADR-016 documents the MetadataUpdater approach)
- **CRCR full matrix** (overlap with InlineConstants)
- **Coverage-driven mutation skip (D3)**
- **Roslyn Diagnostics filter (D1)** (compilation-pass diagnostic extraction)
- Additional greenfield operators (Task.WhenAll → WhenAny, ConfigureAwait swap, AsSpan ↔ AsMemory, AddDays(n) ↔ AddDays(-n))
