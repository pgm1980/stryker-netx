# stryker-netx — Project Overview

## Purpose
1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to **C# 14 / .NET 10**.
Stryker.NET 4.14.1 cannot reliably mutate .NET 9/10 test projects (Buildalyzer 8.0 + .NET 10 MSBuild = broken). stryker-netx fixes this via Buildalyzer 9.0 + MsBuildHelper-Fix while preserving full CLI/config/reporter compatibility with upstream.

## Status (2026-04-30)
- Sprint 0 (Architecture + Design): ✅ Complete (12 ADRs, FRs/NFRs, License-Stack)
- Sprint 1 (Mega-Sprint Bootstrap + Cleanup + Test-Stack-Migration): in progress, branch `feature/1-bootstrap-and-cleanup`, GitHub issue #1, milestone `stryker-netx 1.0.0-preview.1`

## Identity (ADR-003)
- NuGet packages: `stryker-netx` (lib), `dotnet-stryker-netx` (tool)
- C# Namespaces: `Stryker.*` (preserved 1:1 for library-consumer-API-compat)
- CLI command: `dotnet stryker-netx`
- Versioning: SemVer ab `1.0.0-preview.1` (eigene Versionsserie, getrennt von Upstream)

## Repository
- Owner: `pgm1980/stryker-netx` (private)
- Default branch: `main`
- Reference source: `_reference/stryker-4.14.1/` (read-only baseline)
- License: Apache 2.0 + DCO

## Key Documents
- `CLAUDE.md` — binding development directives (ALL rules NON-NEGOTIABLE)
- `_docs/architecture spec/architecture_specification.md` — 12 ADRs
- `_docs/design spec/software_design_specification.md` — FRs/NFRs
- `_config/development_process.md` — Scrum-based workflow
- `MEMORY.md` / `DEEP_MEMORY.md` — project memory (read in this order at session start)
