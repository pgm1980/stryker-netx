# Sprint 15 — v2.2.0 HotSwap Walk-Back: Lessons Learned

**Sprint:** 15 (2026-05-01, autonomous run)
**Branch:** `feature/15-v2.2-hotswap-walkback`
**Base:** v2.1.0 (Sprint 14 closed)
**Final Tag:** `v2.2.0`
**Source:** Pre-implementation recherche on actual mutation-execution pipeline + Sprint-15 Maxential session (14 thoughts, 3-way branch C2 chosen).

## Sprint Outcome

| Metric | Result |
|--------|--------|
| ADR-021 (walk back ADR-016) written | ✅ |
| ADR-022 (incremental mutation testing as future direction, Status: Proposed) written | ✅ |
| `[Obsolete]` annotations on `MutationEngine` enum + `IMutationEngine` interface + `IStrykerOptions.MutationEngine` property + `MutationEngineInput` class | ✅ |
| `HotSwapEngine.cs` + `RecompileEngine.cs` deleted | ✅ |
| CLI shim: `--engine recompile|hotswap` accepted with deprecation warning (no breaking change) | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E (default profile) | ✅ 100.00 % (zero behavior change for non-`--engine` users) |
| Semgrep | ✅ clean |
| README + MIGRATION updated with "Documented removals (v2.2)" section | ✅ |
| Tag | `v2.2.0` |

## Sprint plot twist — recherche revealed ADR-016 was wrong

Sprint 15 began as the focused HotSwap engine implementation deferred from v2.1.0 per ADR-019. Pre-implementation recherche via Serena+Grep on the actual mutation-execution pipeline surfaced:

> `Stryker.Core/MutationTest/CsharpMutationProcess.CompileMutations` calls
> `compilingProcess.Compile(projectInfo.CompilationSyntaxTrees, ms, msForSymbols)` —
> a SINGLE compile pass that produces ONE assembly containing ALL mutations,
> with runtime `ActiveMutationId` switching at the test host.

ADR-016 (Sprint 5) had assumed Stryker.NET compiles **per mutant** and would benefit from a hot-swap pattern. That assumption was wrong. Stryker.NET has used the all-mutations-in-one-assembly + `ActiveMutationId` pattern since v1.x. There is no per-mutant compile to optimize away.

The "5–10× perf boost" claim of ADR-016 has no basis in the actual pipeline. The legitimate perf-relevant configuration is `OptimizationModes.SkipUncoveredMutants` / `CoverageBasedTest`, exposed via `--coverage-analysis` (default `perTest`), which has shipped since v1.x.

## Maxential 3-way branch decision

Sprint 15 Maxential evaluated three responses:

- **C1 — Build HotSwap framework anyway** (~1500 LOC dead code, no value delivered without test-runner protocol changes that are out of scope). Rejected — violates YAGNI.
- **C2 — Walk back ADR-016 with ADR-021, deprecate the engine surface, delete dead code**. ✅ Chosen.
- **C3 — Pivot to incremental mutation testing as the actual perf opportunity** (legitimate but is its own multi-sprint project). Rejected as v2.2 scope; documented as ADR-022 (Proposed) without commitment.

Three arguments tipped the scale to C2:
1. Honest engineering trumps consistency-with-prior-decisions. The Sprint-13 Phase A reconciliation discipline pattern set the precedent.
2. YAGNI is a stated project principle. Building 1500 LOC of HotSwap framework that doesn't deliver value violates it directly.
3. C3 is interesting but deserves its own ADR + multi-sprint roadmap, not a stealth pivot.

## What landed

### ADR-021 — Walking back ADR-016

Long-form ADR documenting:
- The recherche finding (Stryker.NET's actual cost structure)
- Why ADR-016's "5–10× boost" claim was wrong
- The 3-way Maxential branch evaluation (C1/C2/C3)
- The decision to walk back, with full alternatives-rejected rationale
- Lessons learned (pre-implementation recherche is mandatory before architecture decisions; comparison-spec-inspiration ≠ implementation-reality; sunk-cost-fallacy must be actively resisted)

### ADR-022 — Incremental mutation testing (Proposed)

Forward-looking, no commitment. Documents the legitimate future perf direction (file-watcher + source-change-diff + mutant-impact-analysis + partial-rerun + persistent cache). Realistic perf-impact: ~99% mutant-skip on 1-file-edit in a 100-file codebase — that would be the "5–10× boost" ADR-016 originally promised, but for the watch-loop use case, not for full CI runs.

### Code changes (soft deprecation)

- **Deleted**: `src/Stryker.Core/Engines/HotSwapEngine.cs` (only threw `NotSupportedException`), `src/Stryker.Core/Engines/RecompileEngine.cs` (only carried `IMutationEngine.Kind`-marker, no execution path), and the now-empty `src/Stryker.Core/Engines/` directory.
- **`[Obsolete]`** on: `MutationEngine` enum, `IMutationEngine` interface, `IStrykerOptions.MutationEngine` property, `MutationEngineInput` class.
- **CLI shim**: `--engine recompile|hotswap` continues to parse without error; `MutationEngineInput.Validate()` logs a deprecation warning when the user explicitly supplies the flag.
- **Pragma management**: Sonar S1133 ("don't forget to remove deprecated code") and CS0618 (reference to obsolete) are suppressed at the local shim sites with explicit "deferred to v3.0 per ADR-021" comments. CA1040 (empty interface) suppressed on `IMutationEngine` for the same reason.

## Process lessons

### 1. **Pre-implementation recherche is mandatory before architecture decisions.**

ADR-016 was written in Sprint 5 (v2.0.0 Architecture Foundation) based on the comparison.md §5 Punkt 4 ("für C# wäre AssemblyLoadContext mit Hot-Swap der größte Wettbewerbsvorteil"). 30 minutes of `find_symbol` + `Grep` on `CompileMutations` would have revealed the wrong mental model immediately. **Lesson:** future Architecture-Foundation sprints must include a "recherche the actual pipeline" step in the Maxential before locking in an ADR.

### 2. **Comparison-spec-inspiration ≠ implementation-reality.**

What delivers performance value at PIT/mutmut/cargo-mutants depends on those frameworks' specific architecture. PIT compiles per mutant in JVM bytecode; mutmut uses trampolines because it operates at the AST level. Stryker.NET's all-mutations-in-one-assembly pattern is fundamentally different and benefits from different optimizations (coverage-driven skip, batch-test-host). Cross-framework inspiration is valuable but requires architectural-fit verification.

### 3. **Sunk-cost-fallacy must be actively resisted.**

The temptation to think "we shipped Sprint 8 scaffolding and Sprint 14 ADR-019 deferral; we have to ship HotSwap eventually" was directly addressed in Maxential thought 8 — three explicit arguments rejecting it. Without the structured branch evaluation, the easier path would have been C1 (ship the framework anyway) and pretend the perf claim was always conservative.

### 4. **Sonar S1133 + Roslyn CS0618 + CA1040 interaction with `[Obsolete]` shims.**

When deprecating a public API surface that's still called by deprecated-internal code (the shim pattern), you need `#pragma warning disable` for THREE warnings simultaneously: S1133 (don't forget to remove someday), CS0618 (consumer of the obsolete), and CA1040 (if any of the obsolete types are empty interfaces). All three need explicit "deferred to v3.0 per ADR-XXX" comments per CLAUDE.md.

### 5. **`partial class` for `[LoggerMessage]` source-gen pattern.**

The first version of `MutationEngineInput.Validate` used `Logger.LogWarning(...)` for the deprecation warning, which triggered CA1848 ("use LoggerMessage delegates"). Fix: mark the class `partial` and define `private static partial void LogDeprecated(...)` with `[LoggerMessage(EventId, Level, Message)]`. Source generator emits the implementation. Documented for future deprecation-warning patterns.

## v2.x roadmap (post-v2.2.0)

- **No v2.x infrastructure work scheduled.** Operator-shaped recommendations from comparison.md are exhausted (v2.1.0). HotSwap walk-back complete (v2.2.0). The catalogue and pipeline are at a natural stopping point.
- **v3.0 (when scheduled):** hard-remove the `[Obsolete]` `MutationEngine` symbols.
- **ADR-022 (Proposed):** incremental mutation testing — multi-sprint project, only commit if user-demand surfaces.
- Long-tail items:
  - AsyncAwait `.Result` semantic variant (currently emits GetAwaiter().GetResult())
  - GenericConstraintLoosen interface-target case (currently emits class-constraint replacement)
  - validation framework count-reconciliation
  - JsonReport AOT-trim-friendly source-gen rewrite
  - RoslynDiagnostics filter v2 (semantic errors via extended `IEquivalentMutantFilter` contract)

## v2.x progress map (v2.2.0 closes the v2.x line for now)

```
[done]    Sprint 14 → Filter pipeline + operator completion → v2.1.0
[done]    Sprint 15 → HotSwap walk-back (ADR-021)            → v2.2.0   ⭐ MINOR ⭐
[future]  v2.3+    → only on user demand
[future]  v3.0     → hard-remove [Obsolete] MutationEngine surface
```
