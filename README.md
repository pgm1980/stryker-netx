# stryker-netx

> **A 1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to C# 14 and .NET 10.**
>
> ⚠️ **Status:** Sprint 0 (Architecture & Design) complete. Sprint 1 (Implementation) not yet started. Not production-ready. Repository is currently private.

---

## Why this project exists

[Stryker.NET](https://stryker-mutator.io/) is the established mutation-testing framework for the .NET ecosystem. As of version 4.14.1 (released 2026-04-10), it does not work reliably with **.NET 9 / .NET 10** projects because of:

1. **Buildalyzer 8.0 transitive dependency** cannot parse .NET 10 MSBuild structures (Buildalyzer issue [#318](https://github.com/Buildalyzer/Buildalyzer/issues/318)). Buildalyzer 9.0 — the fix — was released only 8 days *after* Stryker.NET 4.14.1 (2026-04-18).
2. **`MsBuildHelper`** falls back to `vswhere` / `MsBuild.exe` paths that fail on .NET-10-SDK-only machines without Visual Studio (stryker-net issue [#3351](https://github.com/stryker-mutator/stryker-net/issues/3351)).

**stryker-netx** addresses these blockers via a 1:1 port that:
- targets `net10.0` (with `Stryker.DataCollector` pinned to `netstandard2.0` for VsTest-adapter compatibility);
- updates Buildalyzer to 9.0+ and Microsoft.* dependencies to .NET 10 versions;
- fixes the `MsBuildHelper` fallback to default to `dotnet msbuild`;
- preserves CLI flags, configuration schema, and reporter outputs identical to Upstream Stryker.NET 4.14.1.

## Disclaimer

This project is an **independent fork**. It is **NOT affiliated with, endorsed by, or sponsored by** the official Stryker.NET project, the Stryker Mutator team, or Info Support BV (the Dutch organization behind Stryker). The "Stryker" name is used here solely descriptively (Stryker-Mutator-compatible tooling) and not as a trademark assertion.

If you are looking for the official Stryker.NET project: https://github.com/stryker-mutator/stryker-net

## Compatibility (target state for v1.0.0)

| Component | Supported |
|-----------|-----------|
| .NET Runtime (Stryker tool itself) | .NET 10 |
| Test-project Target Frameworks | net8.0, net9.0, net10.0 |
| C# Language Version (in user code) | C# 12, 13, 14 |
| Solution formats | `.sln`, `.slnx` |
| Test runners | VsTest, Microsoft Testing Platform |
| OS | Windows (primary), Linux (primary), macOS (best-effort) |

## Installation (target state — not yet released)

```bash
# Once published to NuGet:
dotnet tool install -g dotnet-stryker-netx --version 1.0.0-preview.1

# Then in your test project directory:
cd /path/to/your/test/project
dotnet stryker-netx
```

CLI flags and configuration are 1:1 compatible with Stryker.NET 4.14.1 — see the [official Stryker.NET configuration docs](https://stryker-mutator.io/docs/stryker-net/configuration) for details.

## Project status

| Phase | Status | Output |
|-------|--------|--------|
| Sprint 0 — Architecture & Design | ✅ Complete | [architecture spec](_docs/architecture%20spec/architecture_specification.md), [software design spec](_docs/design%20spec/software_design_specification.md), 12 ADRs |
| Sprint 1 — Mega-Sprint Implementation | ⏳ Pending | TFM update, Buildalyzer 9, analyzer big-bang, test-stack migration, repo identity |
| Sprint 2+ — Refinement | ⏳ Future | Performance, additional features, public release |

Realistic Sprint 1 duration estimate: **4–6 weeks** (mega-sprint covering 11 production + 6 test projects via DAG-layer-parallel subagents).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). All contributions require **DCO sign-off** (`git commit -s`).

## License

Licensed under the [Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for attribution to the original Stryker.NET project and its authors.

## Reference Material

- [_reference/stryker-4.14.1/](_reference/stryker-4.14.1) — Original Stryker.NET 4.14.1 source code (read-only baseline)
- [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md) — Architecture decisions (12 ADRs)
- [_docs/design spec/software_design_specification.md](_docs/design%20spec/software_design_specification.md) — Functional and non-functional requirements
- [_config/development_process.md](_config/development_process.md) — Scrum-based development process
- [CLAUDE.md](CLAUDE.md) — Binding development directives (tooling, code standards, subagent policy)
- [DEEP_MEMORY.md](DEEP_MEMORY.md) — 360° project memory (vision, stack, roadmap, risks)
- Original Stryker.NET project: https://github.com/stryker-mutator/stryker-net

---

*This README will be expanded with concrete usage examples and CI/CD recipes once Sprint 1 produces a working build.*
