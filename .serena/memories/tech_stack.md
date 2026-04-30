# Tech Stack

## Runtime / Language
- **.NET 10** (TFM `net10.0` for all main projects; `Stryker.DataCollector` pinned to `netstandard2.0` per VsTest-adapter constraint)
- **C# 14** with `<LangVersion>latest</LangVersion>`
- **Solution format**: `.slnx` (XML, new format)
- **SDK pinning**: `global.json` with 10.0.100 + `rollForward=latestFeature`

## Build
- MSBuild + `Directory.Build.props` (zentral)
- **Central Package Management** via `Directory.Packages.props` — versions NEVER in csproj
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — every warning is build error
- `AnalysisLevel=latest-recommended`, `AnalysisMode=AllEnabledByDefault`
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>disable</ImplicitUsings>`
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` — XML doc on public APIs

## Analyzers (Big-Bang Sprint 1, ADR-004)
- Roslynator.Analyzers 4.15.0
- SonarAnalyzer.CSharp 10.20.0.135146
- Meziantou.Analyzer 3.0.22
- DotNet.ReproducibleBuilds 2.0.2 (deterministic builds)

## Test Stack (xUnit + FluentAssertions, ADR-005)
- xUnit 2.9.3
- xunit.runner.visualstudio 3.1.4
- FluentAssertions 8.8.0 (PFLICHT — keine MSTest-Asserts, keine Shouldly!)
- Moq 4.20.72
- Microsoft.NET.Test.Sdk 17.14.1
- coverlet.collector 8.0.0 — always run with `--collect:"XPlat Code Coverage"`
- TngTech.ArchUnitNET.xUnit 0.11.0 — architecture-as-tests
- FsCheck.Xunit 3.1.0 — property-based testing for invariants/roundtrips
- BenchmarkDotNet 0.14.0 — separate `benchmarks/` project, Release-mode mandatory

## Stryker-specific Dependencies
- **Buildalyzer 9.0.0** ← KEY .NET-10-COMPAT FIX
- Microsoft.CodeAnalysis.* 5.3.0 (Roslyn — revisit Phase 5 via Context7)
- Microsoft.TestPlatform 18.4.0 + Microsoft.Testing.Platform 1.5.2
- Microsoft.VisualStudio.SolutionPersistence 1.0.52 (.slnx parsing)
- McMaster.Extensions.CommandLineUtils 5.1.0 (deprecated but functional, ADR-007 HYBRID + Wrapper)
- Spectre.Console 0.54.0 (CLI output)
- Serilog 4.3.1 + sinks (structured logging)
- YamlDotNet 17.0.1 (config parsing)
- Mono.Cecil 0.11.6 (IL manipulation in DataCollector)
- LibGit2Sharp 0.31.0 (git diff for incremental mutation)
- AWSSDK.S3 4.0.21 + Azure.Storage.Files.Shares 12.25.0 (baseline reporter)

## Tooling MCP servers (CLAUDE.md PFLICHT)
- **Serena** — symbol-based code analysis (Roslyn/OmniSharp); USE BEFORE Grep/Glob/Read for code symbols
- **Context7** — current API docs; PFLICHT before using new APIs (insb. Buildalyzer 9 migration)
- **Sequential Thinking (Maxential)** `mcp__maxential-cot-mcp-server__*` — ≥10 thoughts for architecture decisions, ≥3 for trade-offs
- **NextGen ToT** `mcp__nextgen-tot-mcp-server__*` — when there are multiple valid solutions
- **GitHub CLI (`gh`)** — multi-step git workflows
- **Semgrep** — security scan before sprint close
