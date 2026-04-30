# Sprint 8 — Hot-Swap Engine Scaffolding: Lessons Learned

**Sprint:** 8 (2026-05-01, autonomous run)
**Branch:** `feature/8-v2-hotswap-engine`
**Base:** v2.0.0-preview.2 (Sprint 7 closed)
**Final Tag:** `v2.0.0-preview.3`
**ADR implemented (scaffolding):** 016 (AssemblyLoadContext Hot-Swap)
**Scope:** SCAFFOLDING ONLY per macro-plan — full impl is a focused follow-up sub-sprint. Sprint-3-precedent honest deferral.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| Maxential Phase 8.1 (10 thoughts + conclusion) | ✅ — Decision: scaffold toward `MetadataUpdater.ApplyUpdate`, not ALC |
| `MutationEngine` enum (Recompile / HotSwap) in Stryker.Abstractions | ✅ |
| `IMutationEngine` marker interface | ✅ |
| `RecompileEngine` (current behaviour, default) | ✅ |
| `HotSwapEngine` (stub w/ ThrowIfInvoked → NotSupportedException + ADR-016 pointer) | ✅ |
| `MutationEngineInput` + wired through StrykerInputs.ValidateAll() | ✅ |
| `IStrykerOptions.MutationEngine` property | ✅ |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test` | ✅ 27/27 |
| Sample E2E | ✅ 100.00 % (default = Recompile = current behaviour) |
| Public API of `Stryker.*` libs | additive only (1 new enum + 1 new interface in Stryker.Abstractions; 2 new engine classes in Stryker.Core) |
| Tag | `v2.0.0-preview.3` |

## Maxential decision summary (Phase 8.1)

| Option | Decision | Rationale |
|--------|----------|-----------|
| (a) AssemblyLoadContext (collectable) — load baseline + per-mutant override, unload | ❌ Rejected | Every mutant pays the test-host startup cost — defeats the trampoline-perf gain. Test-runner integration boundary is the deal-breaker. |
| (b) `MetadataUpdater.ApplyUpdate` (.NET EnC API) | ✅ Chosen | In-process IL-delta apply; test-host stays alive; .NET 10 first-class API; matches mutmut's trampoline shape; constraints (MTP-only initial, `DOTNET_MODIFIABLE_ASSEMBLIES=debug` env) are documentable |
| (c) DynamicMethod / Reflection.Emit | ❌ Pre-rejected (ADR-013) | Bypasses NRT annotations + breaks debug symbols |

Maxential thoughts 1–11 (1 conclusion). Tag: `decision` on thought 5; `plan` on thought 10. Full transcript in the branch's commit history via Maxential session log.

## What landed (scaffolding)

1. **`MutationEngine` enum** — `Recompile = 0` (default), `HotSwap = 1`.
2. **`IMutationEngine` marker interface** — only exposes `MutationEngine Kind { get; }`. Intentionally minimal — engine impls grow methods as they implement, the contract grows alongside.
3. **`RecompileEngine`** — returns `Kind = Recompile`; the orchestrator's existing recompile-per-mutant logic stays where it is for now (a future sprint will move it into this class so the engine becomes truly swappable rather than dispatch-on-Kind).
4. **`HotSwapEngine`** — returns `Kind = HotSwap`; static `ThrowIfInvoked()` method that future code paths must call before doing any HotSwap work, throwing `NotSupportedException` with a clear pointer to ADR-016 + this lessons doc.
5. **CLI/options plumbing** — `MutationEngineInput` (case-insensitive parse), `IStrykerOptions.MutationEngine` property, `StrykerOptions` impl with default `Recompile`, `StrykerInputs.ValidateAll()` propagation.

Default behaviour preserved: `--engine recompile` (default) = current v1.x behaviour = 27/27 + 100 % Sample E2E.

## What deliberately deferred to a focused follow-up

The actual hot-swap implementation requires three substantial pieces of work, each meriting its own scoped sprint slice:

1. **EmitDifference loop** — for each mutant, call `Microsoft.CodeAnalysis.CSharp.EmitDifference` to produce the metadata + IL deltas vs the baseline assembly. Needs an `EmitBaseline` cached from the first compilation and propagated across mutants.
2. **MetadataUpdater integration** — call `MetadataUpdater.ApplyUpdate(assembly, metadataDelta, ilDelta, pdbDelta)` against the test-host's loaded baseline assembly. Requires `DOTNET_MODIFIABLE_ASSEMBLIES=debug` env-var on the test-host process; currently the test-runners don't set this.
3. **Test-runner integration** — VsTest spawns a fresh process per run (incompatible with in-process patching); MTP can stay in-process but needs the patching hook. Initial scope is MTP-only; VsTest support comes later.

Each of these is a 1-2 day investigation + implementation. Sprint 8 deliberately does NOT compress them into one sprint — Sprint-3-precedent: scaffolding now, full implementation as named sub-sprint(s).

## Process lessons

1. **Maxential before scaffolding paid off.** Without the structured ALC-vs-MetadataUpdater comparison, I'd likely have started ALC scaffolding (it's the more obvious pattern from Java). The Maxential exposed the test-host process boundary as the decisive factor — saved a wrong-direction engine implementation.
2. **`Meziantou.Analyzer` MA0025 prefers `NotSupportedException` over `NotImplementedException`** for intentional-deferral scaffolding. Initially used `NotImplementedException`; analyzer flagged it. Switched to `NotSupportedException` with explicit message about Sprint-N follow-up. Documented for future scaffolding patterns.
3. **Sprint-3 PARTIAL pattern remains the right shape for high-risk sprints.** Sprint 8 ships the architecture commitment (engine boundary, opt-in flag, design decision) without faking the implementation. Same pattern that worked for Bug-5 → v1.0.0-rc.1 → Sprint-4 fix → v1.0.0.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1
[done]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
[done]    Sprint 8  → Hot-Swap engine SCAFFOLDING           → v2.0.0-preview.3  ⭐ (impl deferred)
[next]    Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
          Sprint 10 → Coverage-Driven + PIT-1 Operators     → v2.0.0-preview.5
          Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
