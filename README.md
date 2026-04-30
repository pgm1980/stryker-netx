# stryker-netx

> **A 1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to C# 14 / .NET 10 — fully `.slnx`-aware.**

[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

`stryker-netx` is a fork of Stryker.NET that targets `.NET 10`, eliminates the `Buildalyzer` dependency in favour of `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`, and supports the modern `.slnx` (XML-based) solution format that ships as the default with the .NET 9 / 10 SDKs. All public CLI flags, configuration schema, and reporter outputs remain 1:1 compatible with upstream Stryker.NET 4.14.1.

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

Or pin a specific version:

```bash
dotnet tool install -g dotnet-stryker-netx --version 1.0.0-preview.2
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

## Migration from Stryker.NET

If you already use `dotnet-stryker`, switching is a two-step rename:

1. Uninstall the upstream tool, install `stryker-netx`:
   ```bash
   dotnet tool uninstall -g dotnet-stryker
   dotnet tool install -g dotnet-stryker-netx
   ```
2. In your scripts / CI workflows, replace `dotnet stryker` with `dotnet stryker-netx`.

That's it. **`stryker-config.json`, CLI flags, and reporter output formats are unchanged.** No config file edits required.

## Known limitations (v1.0.0-preview.2)

- `<ProjectReference Include="..." Aliases="X"/>` (project-references with extern aliases) is not yet handled in the mutated-compilation step — `extern alias X;` in source code triggers `CS0430` after mutation. Tracking issue / fix planned for the next sub-phase. Workaround: drop the `Aliases` metadata or use `<Reference>` (metadata-reference) form instead.
- Integration-test parity with upstream is partial — see `integrationtest/` for the full upstream-compatibility test matrix; categories that pass / fail are documented in the Sprint 3 lessons.
- `JsonReport` reporter still uses runtime reflection (not source-generated) — Sprint 4 candidate. Functional, just not AOT-trimmable.

## Project status

| Sprint | Outcome |
|--------|---------|
| Sprint 0 — Architecture & Design | ✅ 12 ADRs, FRs, NFRs, test stack chosen |
| Sprint 1 — Implementation (Mega-Sprint, 10 phases) | ✅ Tag `v1.0.0-preview.1` — Buildalyzer fully removed, all 11 + 6 projects on .NET 10, 233 ILogger calls source-generated |
| Sprint 2 — Code Excellence (8 phases) | ✅ Tag `v1.0.0-preview.2` — C# 14 extension members, [GeneratedRegex], JsonSerializerContext, field keyword, list patterns, RSL — code-quality lifted to "high-end" |
| Sprint 3 — Production Hardening | 🚧 In progress — integration suite, NuGet packaging, CI |

See [`_docs/`](`_docs`) for sprint lessons docs.

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
- Architecture decisions: [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md)
- Sprint lessons: [_docs/sprint_1_lessons.md](_docs/sprint_1_lessons.md), [_docs/sprint_2_lessons.md](_docs/sprint_2_lessons.md)
