# stryker-netx

> **A 1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to C# 14 / .NET 10 — fully `.slnx`-aware — with a v2.0.0 mutation-operator catalogue that rivals PIT, cargo-mutants, and mutmut.**

[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

`stryker-netx` is a fork of Stryker.NET that targets `.NET 10`, eliminates the `Buildalyzer` dependency in favour of `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`, supports the modern `.slnx` (XML-based) solution format, and (since v2.0.0) ships a substantially expanded mutation-operator catalogue plus a `MutationProfile` opt-in surface for tunable noise/aggression. All public CLI flags and the `stryker-config.json` schema remain 1:1 backwards-compatible with upstream Stryker.NET 4.14.1.

## What's new in v2.x line

v2.x is **fully backwards-compatible**: existing v1.x users see zero behavioral change unless they opt into a stronger mutation profile.

- **`--mutation-profile` flag** (`Defaults` | `Stronger` | `All`) — orthogonal to `--mutation-level`; controls *which mutators* run (not just which mutations).
- **25 net-new mutators** across 6 batches (typed-driven, PIT-1, PIT-2 + cargo-mutants, .NET-greenfield, v2.0.1 spec-gap closure, **v2.1.0 filter pipeline + operator completion**).
- **4 equivalent-mutant filters** in the pipeline (`IdentityArithmetic`, `IdempotentBoolean`, `ConservativeDefaultsEquality`, `RoslynDiagnostics` — the v2.1.0 mutmut-style mypy/pyrefly pre-filter).
- **Operator hierarchy** + `[MutationProfileMembership]` attribute on every mutator (ADR-014, ADR-018).
- **SemanticModel-driven type-aware mutators** (ADR-015) — used by `TypeDrivenReturn`, `ArgumentPropagation`, `MemberVariable`, `MethodBodyReplacement`.
- **Equivalent-Mutant Filter pipeline** as a first-class stage (ADR-017).
- **`--engine` flag** — deprecated v2.2.0 per ADR-021 (was based on a wrong mental model; accepted as a no-op shim).

See [MIGRATION-v1-to-v2.md](MIGRATION-v1-to-v2.md) for the full upgrade story.

## Why this fork exists

Upstream Stryker.NET 4.14.1 (released 2026-04-10) does not run on .NET 9 / .NET 10 projects nor on `.slnx` solutions because:

1. **Buildalyzer 8.0 transitive dependency** cannot parse .NET 10 MSBuild structures. Buildalyzer 9.0 — the fix — landed *eight days after* Stryker.NET 4.14.1.
2. **`MsBuildHelper`** falls back to `vswhere`/`MsBuild.exe` paths that don't exist on .NET-10-SDK-only machines.
3. **`.slnx`** support is missing from upstream's `SolutionFile` parser.

`stryker-netx` replaces the Buildalyzer pipeline entirely with `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`, gives the CLI a parallel `Microsoft.VisualStudio.SolutionPersistence`-based `.slnx` reader, and renames the tool to avoid colliding with the upstream package.

## Disclaimer

`stryker-netx` is an **independent community fork**. It is **NOT affiliated with, endorsed by, or sponsored by** the official Stryker.NET project, the Stryker Mutator team, or Info Support BV. The "Stryker" name is used here descriptively (Stryker-Mutator-compatible tooling) and not as a trademark assertion.

For the official Stryker.NET project see https://github.com/stryker-mutator/stryker-net.

## Compatibility

| Component | Supported |
|-----------|-----------|
| .NET Runtime (the tool itself) | .NET 10 |
| Test-project Target Frameworks | net8.0, net9.0, net10.0 |
| C# Language Version (in user code) | C# 12, 13, 14 |
| Solution formats | `.sln`, **`.slnx`** |
| Test runners | VsTest, Microsoft Testing Platform |
| Test frameworks | xUnit, MSTest 2/3, NUnit, TUnit |
| OS | Windows ✓, Linux ✓ (CI), macOS (best-effort) |

## Installation

```bash
dotnet tool install -g dotnet-stryker-netx
```

Pin a specific version:

```bash
dotnet tool install -g dotnet-stryker-netx --version 2.0.0
```

## Quickstart

In your test project directory:

```bash
cd /path/to/your/Tests
dotnet stryker-netx
```

Or against a solution from the solution directory:

```bash
cd /path/to/your/solution
dotnet stryker-netx --solution YourApp.slnx
```

Or with a `stryker-config.json`:

```bash
dotnet stryker-netx --config-file stryker-config.json
```

CLI flags, configuration schema, and reporter outputs are 1:1 compatible with [upstream Stryker.NET configuration docs](https://stryker-mutator.io/docs/stryker-net/configuration).

## Mutation Profiles (v2.0.0)

`--mutation-profile` controls which mutators are *active* during a run. It is independent of `--mutation-level` (which controls which *mutations* a given mutator emits).

| Profile | Active mutators | Use case |
|---------|-----------------|----------|
| `Defaults` (default) | 26 v1.x mutators only | Drop-in v1.x parity. Same behavior as upstream Stryker.NET. |
| `Stronger` | Defaults + 17 type-aware / catalogue-closing mutators (= 43 total) | Catch more bugs while keeping noise manageable. |
| `All` | Stronger + 8 most-aggressive operators (UoiMutator, NakedReceiver, ExceptionSwap, GenericConstraint, ArgumentPropagation, AsSpanAsMemory, MethodBodyReplacement, SpanReadOnlySpanDeclaration) (= 51 total) | Maximum coverage; expect mutation volume to grow ~3-5× and runtime accordingly. |

Set via CLI:

```bash
dotnet stryker-netx --mutation-profile Stronger
dotnet stryker-netx --mutation-profile All
```

Or in `stryker-config.json`:

```json
{
  "stryker-config": {
    "mutation-profile": "Stronger"
  }
}
```

## Operator catalogue (v2.1.0)

| Family | v1.x mutators (Defaults) | Stronger additions | All-only additions |
|--------|--------------------------|--------------------|--------------------|
| Arithmetic | BinaryExpression, Math | Aod (operator deletion), InlineConstants, ConstantReplacement (PIT CRCR, v2.1) | — |
| Relational | BinaryExpression, RelationalPattern | RorMatrix (full 5-replacement matrix) | — |
| Logical / Boolean | Boolean, NegateCondition, ConditionalExpression, BinaryPattern | — | — |
| Unary | PrefixUnary, PostfixUnary | — | UoiMutator |
| Strings | String, StringEmpty, StringMethod, StringMethodToConstant, InterpolatedString | — | — |
| Collections / LINQ | Linq, Collection, Initializer, ArrayCreation | TypeDrivenReturn (cargo-mutants C2) | — |
| Object construction | ObjectCreation | ConstructorNull (PIT CONSTRUCTOR_CALLS) | — |
| Method calls | (covered indirectly) | — | NakedReceiver (PIT EXP_NAKED_RECEIVER), ArgumentPropagation (PIT EXP_ARGUMENT_PROPAGATION, v2.0.1) |
| Pattern matching | IsPatternExpression, RelationalPattern, BinaryPattern | MatchGuard (cargo-mutants C4), SwitchArmDeletion (cargo-mutants C3, v2.0.1) | — |
| Records | (covered by ObjectCreation/Initializer) | WithExpression (cargo-mutants C5) | — |
| Member variables | (covered by AssignmentExpression) | MemberVariable (PIT EXP_MEMBER_VARIABLE, v2.0.1) | — |
| Method bodies | (covered by Block/Statement) | — | MethodBodyReplacement (cargo-mutants C1, v2.0.1) |
| Async / await | (none in v1.x) | AsyncAwait, ConfigureAwait (v2.0.1), TaskWhenAllToWhenAny (v2.0.1) | — |
| DateTime | (none in v1.x) | DateTime, DateTimeAddSign (v2.0.1) | — |
| Span / Memory | (none in v1.x) | SpanMemory | AsSpanAsMemory (v2.0.1), SpanReadOnlySpanDeclaration (v2.1) |
| Exceptions | (none in v1.x) | — | ExceptionSwap |
| Generics | (none in v1.x) | — | GenericConstraint |
| Generic constraints | — | GenericConstraintLoosen (per-clause, v2.1) | GenericConstraint (drop-all) |
| Other | Block, Statement, Assignment, Checked, Regex, NullCoalescing | — | — |

Total: **26 (Defaults) + 17 (Stronger) + 8 (All-only) = 51 mutators** (v2.1.0).

The equivalent-mutant filter pipeline ships **4 filters** (v2.1.0): `IdentityArithmeticFilter`, `IdempotentBooleanFilter`, `ConservativeDefaultsEqualityFilter`, and `RoslynDiagnosticsEquivalenceFilter` (v2.1 — fast-paths mutants with parser-error replacement nodes, mutmut's mypy/pyrefly-style pre-filter).

After v2.1.0, the catalogue closes essentially all operator-shaped recommendations from the comparison spec. Operator-level gaps from PIT/cargo-mutants/mutmut are exhausted.

## Mutation execution model (updated v2.2.0)

Stryker.NET (and stryker-netx) compile **all mutations into a single assembly** with runtime `ActiveMutationId` switching. There is no per-mutant compile to optimize away. The actual cost driver is test-host process spawn per batch — already mitigated by the existing `--coverage-analysis` flag (default `perTest`), which skips uncovered mutants and reuses the test host across covered mutants.

The `--engine Recompile|HotSwap` flag introduced in v2.0.0 was based on a wrong mental model of this cost structure. **v2.2.0 walks it back per [ADR-021](_docs/architecture%20spec/architecture_specification.md):**

- The flag is still accepted for backwards compatibility (no breaking change for existing scripts/configs).
- Both values are treated identically — they have no functional effect.
- A deprecation warning is logged when the flag is supplied explicitly.
- The `HotSwapEngine` and `RecompileEngine` implementation classes have been deleted.
- The `MutationEngine` enum, `IMutationEngine` interface, `IStrykerOptions.MutationEngine` property, and `MutationEngineInput` config class are marked `[Obsolete]` as v2.x source-compat shims; v3.0 may hard-remove them.

For real performance tuning, use `--coverage-analysis perTest` (default) or `--coverage-analysis all` — see `OptimizationModes` documentation. A future incremental-mutation-testing direction (file-watcher + change-driven re-run) is tracked in [ADR-022 (Proposed)](_docs/architecture%20spec/architecture_specification.md), without commitment.

## Migration from upstream Stryker.NET

If you already use `dotnet-stryker`, switching is a two-step rename:

1. Uninstall the upstream tool, install `stryker-netx`:
   ```bash
   dotnet tool uninstall -g dotnet-stryker
   dotnet tool install -g dotnet-stryker-netx
   ```
2. In your scripts / CI workflows, replace `dotnet stryker` with `dotnet stryker-netx`.

That's it. **`stryker-config.json`, CLI flags, and reporter output formats are unchanged.** No config file edits required.

## Migration from stryker-netx v1.x to v2.0.0

See [MIGRATION-v1-to-v2.md](MIGRATION-v1-to-v2.md). Short version: **no breaking changes for the default profile**. To opt into the expanded catalogue, add `--mutation-profile Stronger` or `--mutation-profile All`.

## Known limitations (v2.1.0)

- **NetFramework projects** (legacy `packages.config` style — `<TargetFramework>net48</TargetFramework>`) require `nuget.exe restore` of the .sln before invocation, because `dotnet msbuild -restore` only handles `<PackageReference>` style. CI's `windows-latest` runner ships `nuget.exe`; local-only blocked unless `nuget.exe` is on PATH. (Carried forward from v1.0.)
- `JsonReport` reporter still uses runtime reflection (not source-generated) — functional, just not AOT-trimmable. Tracking for a future "AOT" sprint.
- Validation framework count-based assertions in `integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs` hardcode upstream Stryker.NET 4.14.1's exact mutant counts and have NOT been reconciled to our mutator output (which legitimately differs slightly due to C#-14-aware behavior + the v2.0/v2.1 expanded catalogue). The framework BUILDS and the InitCommand validation test PASSES; per-fixture count reconciliation is a follow-up task.
- **HotSwap engine** — removed in v2.2.0 per ADR-021. The `--engine` flag is accepted as a deprecated no-op shim; both `Recompile` and `HotSwap` are treated identically with a deprecation warning.
- **AsyncAwaitMutator** semantics — emits `await x → x.GetAwaiter().GetResult()`, **not** `await x → x.Result` as listed in the comparison spec. The two are similar but not identical (`.Result` wraps exceptions in `AggregateException`; `GetAwaiter().GetResult()` unwraps). Documented for transparency.
- **GenericConstraintMutator** (v2.0.0) drops the entire constraint-clause set; **GenericConstraintLoosenMutator** (v2.1.0) does the spec-listed per-clause loosening (`where T : class → where T : new()` etc.). Both ship — the former is more aggressive, the latter more targeted. Use the profile to control activation.
- **SpanMemoryMutator** semantics — emits `span.Slice(start, length) → span.Slice(0, length)`, a stryker-netx-specific variant. **AsSpanAsMemoryMutator** (v2.0.1) handles invocation-site `AsSpan() ↔ AsMemory()`; **SpanReadOnlySpanDeclarationMutator** (v2.1) handles declaration-site `Span<T> ↔ ReadOnlySpan<T>`. All three coexist.
- **Coverage-driven mutation skip** (mutmut-style) — already shipped via `OptimizationModes.SkipUncoveredMutants` and `CoverageBasedTest` (v1.x). Use `--coverage-analysis perTest` (the default) or `--coverage-analysis all` to enable; mutants for lines without test coverage are flagged `NoCoverage` and never run.

## Project status

| Sprint | Outcome |
|--------|---------|
| Sprint 0 — Architecture & Design | ✅ 12 ADRs, FRs, NFRs, test stack chosen |
| Sprint 1 — Implementation (Mega-Sprint, 10 phases) | ✅ Tag `v1.0.0-preview.1` — Buildalyzer fully removed, all 11 + 6 projects on .NET 10 |
| Sprint 2 — Code Excellence | ✅ Tag `v1.0.0-preview.2` — C# 14 extension members, source-gen regex, list patterns, RSL |
| Sprint 3 — Production Hardening | ✅ Tag `v1.0.0-rc.1` — integration suite vendored, NuGet + CI + Release pipeline |
| Sprint 4 — Bug Elimination | ✅ Tag **`v1.0.0`** — Bug-5 fixed; all NetCore + MTP + Edge integration categories run end-to-end |
| Sprint 5 — v2.0.0 Architecture Foundation | ✅ ADRs 013–018 + interface stubs (no tag) |
| Sprint 6 — Operator-Hierarchy + Profile Refactor | ✅ Tag `v2.0.0-preview.1` |
| Sprint 7 — SemanticModel + Equiv-Mutant Filter | ✅ Tag `v2.0.0-preview.2` |
| Sprint 8 — Hot-Swap engine SCAFFOLDING | ✅ Tag `v2.0.0-preview.3` |
| Sprint 9 — Type-Driven Mutators | ✅ Tag `v2.0.0-preview.4` |
| Sprint 10 — PIT-1 Operator Batch | ✅ Tag `v2.0.0-preview.5` |
| Sprint 11 — PIT-2 + cargo-mutants Batch | ✅ Tag `v2.0.0-rc.1` |
| Sprint 12 — Greenfield + Release | ✅ Tag **`v2.0.0`** — production |
| Sprint 13 — Spec-gap closure | ✅ Tag **`v2.0.1`** — 8 new mutators (ConfigureAwait, DateTimeAddSign, SwitchArmDeletion, MemberVariable, TaskWhenAllToWhenAny, ArgumentPropagation, AsSpanAsMemory, MethodBodyReplacement) closing remaining §4.1 / §4.2 / §4.4 spec items |
| Sprint 14 — Filter pipeline + operator completion | ✅ Tag **`v2.1.0`** — 3 new mutators (ConstantReplacement = PIT CRCR, GenericConstraintLoosen, SpanReadOnlySpanDeclaration) + 1 new equivalence filter (RoslynDiagnostics, mutmut-style); HotSwap engine deferred to v2.2.0 per ADR-019 |
| Sprint 15 — HotSwap walk-back | ✅ Tag **`v2.2.0`** — pre-implementation recherche revealed ADR-016 was based on a wrong mental model of Stryker.NET's cost structure (no per-mutant compile to optimize away). ADR-021 walks back ADR-016, soft-deprecates the engine surface, deletes dead code. ADR-022 (Proposed) records incremental mutation testing as the legitimate future perf direction without commitment. |

See [`_docs/`](_docs/) for per-sprint lessons.

## Building from source

Requires .NET SDK **10.0.107+**.

```bash
dotnet build stryker-netx.slnx
dotnet test stryker-netx.slnx
dotnet pack src/Stryker.CLI/Stryker.CLI.csproj -c Release -p:PackageVersion=$YOUR_VERSION
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). All contributions require **DCO sign-off** (`git commit -s`).

## License

Licensed under the [Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for attribution to the original Stryker.NET project and its authors.

## References

- Original Stryker.NET project: https://github.com/stryker-mutator/stryker-net
- Stryker Mutator docs: https://stryker-mutator.io/
- Architecture decisions (incl. v2.0.0 ADRs 013–018): [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md)
- v1→v2 migration guide: [MIGRATION-v1-to-v2.md](MIGRATION-v1-to-v2.md)
- Per-sprint lessons: [_docs/](_docs/)
