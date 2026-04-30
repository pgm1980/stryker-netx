# Sprint 6 — Operator-Hierarchy + Profile Refactor: Lessons Learned

**Sprint:** 6 (2026-04-30, autonomous run)
**Branch:** `feature/6-v2-hierarchy-refactor`
**Base:** v1.0.0 + Sprint 5 ADRs (main at `884ba2c`)
**Final Tag:** `v2.0.0-preview.1`
**ADRs implemented:** 014 (Operator-Hierarchy), 018 (Mutation Profiles)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| 26 existing mutators decorated with `[MutationProfileMembership]` | ✅ all via Serena `insert_before_symbol` |
| `IStrykerOptions.MutationProfile` property | ✅ added |
| `MutationProfileInput` + wired into `StrykerInputs.ValidateAll()` | ✅ |
| `MutatorProfileFilter` static helper + orchestrator filter | ✅ |
| `dotnet build`: | ✅ 0 / 0 |
| `dotnet test`: | ✅ 27/27 |
| Sample E2E: | ✅ 100.00 % |
| Public API of `Stryker.*` libs | additive only (new property + new types); no breaking changes |
| Tag | `v2.0.0-preview.1` |

## What landed (minimum-viable per Sprint-6 plan)

1. **Attribute decoration of all 26 mutators** — every existing v1.x mutator carries `[MutationProfileMembership(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]`. Default behaviour preserved by construction (Defaults profile = all 26 active).
2. **`IStrykerOptions.MutationProfile` property** with `init` accessor; defaults to `MutationProfile.Defaults`.
3. **`MutationProfileInput` Input** wired into `StrykerInputs.ValidateAll()`. Accepts `Defaults` / `Stronger` / `All` (case-insensitive).
4. **`MutatorProfileFilter`** static helper with `IsInProfile(IMutator, MutationProfile)` reading the attribute via reflection; absent attribute → all-profiles default for safety.
5. **`CsharpMutantOrchestrator` constructor filter** uses `MutatorProfileFilter` to select active mutators based on `options.MutationProfile`.

## What deliberately deferred to a follow-up

- **CLI `--profile` flag wiring** in `CommandLineConfigReader` — currently `MutationProfile` is settable programmatically + via `stryker-config.json` (once the config reader picks up the new key), but no dedicated CLI flag yet. This is a 20-line change for Sprint 6b or as part of Sprint 7's CLI work.
- **IMutatorGroup / IMutationOperator implementations** — interfaces are in place (Sprint 5 stubs), but no concrete `DefaultMutatorGroup` class wraps the 26 mutators yet. The orchestrator currently uses the raw `IList<IMutator>`, which is still valid because `IMutatorGroup` is purely additive. Sub-operator decomposition (splitting `BinaryExpressionMutator` into `MATH_ADD_TO_SUB` etc.) is genuinely Sprint 10+ work.
- **Per-mutator profile refinement** — every mutator currently belongs to all three profiles (Defaults | Stronger | All). When new operators arrive in Sprints 9-12, they will be tagged more selectively (e.g. AOD, ROR-Vollmatrix → Stronger only; Access-Modifier → All only).

## Process lessons

1. **Serena `insert_before_symbol` for attribute application is fast once you know the absolute-path syntax.** First batch failed with "multiple symbols matching" for mutators where the static constructor shares the class name (LinqMutator, MathMutator, RegexMutator). Fix: use absolute name path with leading slash + full namespace prefix: `/Stryker.Core.Mutators/MathMutator`. After that, 23 of 23 remaining inserts succeeded in two parallel batches of 10 + 10 + 3.
2. **`replace_symbol_body` index-disambiguation `[1]` did not work for me on overloaded ctors** (one static, one instance, same name_path). Root cause unclear — `find_symbol` returns both with no obvious disambiguator. Workaround: fall back to `Edit` after explicit `Read`. Documented for future Serena usage; not a blocker.
3. **Caught + corrected a CLAUDE.md violation early.** I had been using `Read` to inspect a mutator file (MathMutator.cs:1-128) when I should have used Serena. The user explicitly flagged this. Switched to Serena for all subsequent code-symbol work in Sprint 6 and going forward. Documented in MEMORY/feedback for the next sprints.
4. **Additive over invasive paid off.** Decoration with attribute + a single filter call in the orchestrator delivered ADR-018 with zero risk to existing 27/27 + Sample E2E. Sub-operator decomposition (the more invasive interpretation of ADR-014) is correctly deferred to when new operators actually need it.

## Risks carried into Sprint 7+

- **`MutationProfile` flag membership audit** — every new operator added in Sprints 9-12 needs a deliberate profile assignment. Defaults to all-profiles is safe but slightly defeats the purpose. Sprint 9 sub-task: review profile membership for every new operator.
- **Config-file `mutation-profile` key** — `stryker-config.json` schema not yet updated to advertise the new key. README + JSON-schema update is a Sprint 7 housekeeping item.

## v2.0.0 progress map

```
[done]    Sprint 5  → ADRs 013–018 + stubs                 (no tag)
[done]    Sprint 6  → Operator-Hierarchy + Profile Refactor → v2.0.0-preview.1  ⭐
[next]    Sprint 7  → SemanticModel + EquivMutFilter        → v2.0.0-preview.2
          Sprint 8  → AssemblyLoadContext Hot-Swap          → v2.0.0-preview.3
          Sprint 9  → Type-Driven Mutators                  → v2.0.0-preview.4
          Sprint 10 → Coverage-Driven + PIT-1 Operators     → v2.0.0-preview.5
          Sprint 11 → PIT-2 + cargo-mutants Operators       → v2.0.0-rc.1
          Sprint 12 → Greenfield + Release                  → v2.0.0
```
