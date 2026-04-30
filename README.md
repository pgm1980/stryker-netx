# stryker-netx

> **A 1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to C# 14 / .NET 10 — fully `.slnx`-aware — with a v2.0.0 mutation-operator catalogue that rivals PIT, cargo-mutants, and mutmut.**

[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

`stryker-netx` is a fork of Stryker.NET that targets `.NET 10`, eliminates the `Buildalyzer` dependency in favour of `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`, supports the modern `.slnx` (XML-based) solution format, and (since v2.0.0) ships a substantially expanded mutation-operator catalogue plus a `MutationProfile` opt-in surface for tunable noise/aggression. All public CLI flags and the `stryker-config.json` schema remain 1:1 backwards-compatible with upstream Stryker.NET 4.14.1.

## What's new in v2.0.0

v2.0.0 is **fully backwards-compatible**: existing v1.x users see zero behavioral change unless they opt into a stronger mutation profile.

- **`--mutation-profile` flag** (`Defaults` | `Stronger` | `All`) — orthogonal to `--mutation-level`; controls *which mutators* run (not just which mutations).
- **14 net-new mutators** across 4 batches (typed-driven, PIT-1, PIT-2 + cargo-mutants, .NET-greenfield).
- **Operator hierarchy** + `[MutationProfileMembership]` attribute on every mutator (ADR-014, ADR-018).
- **SemanticModel-driven type-aware mutators** (ADR-015).
- **Equivalent-Mutant Filter pipeline** as a first-class stage (ADR-017).
- **`--engine` flag** (`Recompile` default | `HotSwap` SCAFFOLDING-only — see roadmap).

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
| `Stronger` | Defaults + 10 type-aware / catalogue-closing mutators (= 36 total) | Catch more bugs while keeping noise manageable. |
| `All` | Stronger + 4 most-aggressive operators (UoiMutator, NakedReceiver, ExceptionSwap, GenericConstraint) (= 40 total) | Maximum coverage; expect mutation volume to grow ~2-4× and runtime accordingly. |

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

## Operator catalogue (v2.0.0)

| Family | v1.x mutators (Defaults) | Stronger additions | All-only additions |
|--------|--------------------------|--------------------|--------------------|
| Arithmetic | BinaryExpression, Math | Aod (operator deletion), InlineConstants | — |
| Relational | BinaryExpression, RelationalPattern | RorMatrix (full 5-replacement matrix) | — |
| Logical / Boolean | Boolean, NegateCondition, ConditionalExpression, BinaryPattern | — | — |
| Unary | PrefixUnary, PostfixUnary | — | UoiMutator |
| Strings | String, StringEmpty, StringMethod, StringMethodToConstant, InterpolatedString | — | — |
| Collections / LINQ | Linq, Collection, Initializer, ArrayCreation | TypeDrivenReturn (cargo-mutants C2) | — |
| Object construction | ObjectCreation | ConstructorNull (PIT CONSTRUCTOR_CALLS) | — |
| Method calls | (covered indirectly) | — | NakedReceiver (PIT EXP_NAKED_RECEIVER) |
| Pattern matching | IsPatternExpression, RelationalPattern, BinaryPattern | MatchGuard (cargo-mutants C4) | — |
| Records | (covered by ObjectCreation/Initializer) | WithExpression (cargo-mutants C5) | — |
| Async / await | (none in v1.x) | AsyncAwait (greenfield) | — |
| DateTime | (none in v1.x) | DateTime (greenfield) | — |
| Span / Memory | (none in v1.x) | SpanMemory (greenfield) | — |
| Exceptions | (none in v1.x) | — | ExceptionSwap (greenfield) |
| Generics | (none in v1.x) | — | GenericConstraint (greenfield) |
| Other | Block, Statement, Assignment, Checked, Regex, NullCoalescing | — | — |

Total: **26 (Defaults) + 10 (Stronger) + 4 (All-only) = 40 mutators**.

After Sprint 12, the catalogue closes the major operator-shaped recommendations from PIT (`AOD`, `ROR`, `UOI`, `INLINE_CONSTS`, `CONSTRUCTOR_CALLS`, `EXP_NAKED_RECEIVER`) and cargo-mutants (typed default returns, `with`-expression field deletion, `when`-clause mutation, conservative-defaults equality filtering), plus 5 .NET-specific operators (Async/Await, DateTime, Span/Memory, Exception-Swap, Generic-Constraint). A handful of finer-grained items from the comparison spec remain open and are tracked under "Roadmap" below.

## Mutation Engines (v2.0.0)

`--engine` selects the execution model:

| Engine | Status | Description |
|--------|--------|-------------|
| `Recompile` (default) | ✅ Production | v1.x default — compile per mutant, run the test suite, discard. Maximally compatible. |
| `HotSwap` | 🚧 Scaffolding only | v2.0.0 ships the `IMutationEngine` plumbing; the `MetadataUpdater.ApplyUpdate`-based implementation is roadmapped (see `_docs/architecture spec` ADR-016). Selecting it currently throws `NotSupportedException` with a pointer to the follow-up implementation work. |

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

## Known limitations (v2.0.0)

- **NetFramework projects** (legacy `packages.config` style — `<TargetFramework>net48</TargetFramework>`) require `nuget.exe restore` of the .sln before invocation, because `dotnet msbuild -restore` only handles `<PackageReference>` style. CI's `windows-latest` runner ships `nuget.exe`; local-only blocked unless `nuget.exe` is on PATH. (Carried forward from v1.0.)
- `JsonReport` reporter still uses runtime reflection (not source-generated) — functional, just not AOT-trimmable. Tracking for a future "AOT" sprint.
- Validation framework count-based assertions in `integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs` hardcode upstream Stryker.NET 4.14.1's exact mutant counts and have NOT been reconciled to our mutator output (which legitimately differs slightly due to C#-14-aware behavior + the v2.0 expanded catalogue). The framework BUILDS and the InitCommand validation test PASSES; per-fixture count reconciliation is a follow-up task.
- **HotSwap engine** — scaffolding only in v2.0.0 (see Mutation Engines table above).
- **Open spec items** carried into the v2.0.x roadmap (see [MIGRATION-v1-to-v2.md](MIGRATION-v1-to-v2.md#roadmap-v20x--v21) for the full list). Not yet implemented:
  - PIT: `CRCR` full matrix, Argument Propagation, Member Variable Mutator
  - cargo-mutants: Function-Body-Replacement genre, Match-Arm-Deletion (switch with `_`-default)
  - Greenfield: `Task.WhenAll → WhenAny`, `ConfigureAwait` swap, `AddDays(n) ↔ AddDays(-n)`, `AsSpan() ↔ AsMemory()`, `Span<T> ↔ ReadOnlySpan<T>`
  - mutmut: coverage-driven mutation skip, Roslyn Diagnostics filter
- **AsyncAwaitMutator** semantics — emits `await x → x.GetAwaiter().GetResult()`, **not** `await x → x.Result` as listed in the comparison spec. The two are similar but not identical (`.Result` wraps exceptions in `AggregateException`; `GetAwaiter().GetResult()` unwraps). Documented for transparency.
- **GenericConstraintMutator** semantics — drops the **entire** constraint clause set on a method, rather than the spec-listed constraint *loosening* (`where T : class → where T : new()`). Closely related but more aggressive.
- **SpanMemoryMutator** semantics — emits `span.Slice(start, length) → span.Slice(0, length)`, which is a stryker-netx-specific variant; the spec-listed `Span<T> ↔ ReadOnlySpan<T>` and `AsSpan() → AsMemory()` are not yet implemented.

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
