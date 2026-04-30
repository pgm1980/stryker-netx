# stryker-netx — Memory Index

> Einstiegspunkt zum Project Memory. **Vertiefung in [DEEP_MEMORY.md](DEEP_MEMORY.md)** — 360° Deep Level Memory.

## Status (Stand: 2026-04-30, Sprint 0 abgeschlossen)

- **Projekt:** 1:1-Portierung von Stryker.NET 4.14.1 auf C# 14 / .NET 10
- **Repo:** [pgm1980/stryker-netx](https://github.com/pgm1980/stryker-netx) (privat, GitHub Flow)
- **User:** GitHub-Account `pgm1980`, Sprache Deutsch (Anweisungen) + Englisch (Code/Commits)
- **Sprint-Phase:** ✅ **Sprint 0 abgeschlossen** — Brainstorming + 12 ADRs + Software Design Spec + License-Stack
- **Aktiver Branch:** `main`; Sprint 1 ab nächstem Sprint-State-Update auf `feature/<issue>-bootstrap-and-cleanup`

## Sprint-0-Ergebnisse (alle approved)

| Output | Datei |
|--------|-------|
| Architecture Specification mit 12 ADRs | [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md) |
| Software Design Specification (FR-01..09 + NFR-01..09) | [_docs/design spec/software_design_specification.md](_docs/design%20spec/software_design_specification.md) |
| Apache 2.0 + Attribution | [LICENSE](LICENSE), [NOTICE](NOTICE) |
| DCO-Workflow + PR-Standards | [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) |
| README mit Disclaimer + Compat-Section | [README.md](README.md) |
| Maxential-Reasoning-Session (19 Schritte) | gespeichert als `d4cc4d23b8d3` im Maxential-MCP-Server |
| ToT-Trees | `95a80ba9` (NativeAOT), `c928b0c5` (McMaster), `01b5e0be` (License), `19336423` (Modul-Reihenfolge) |

## Schlüssel-Entscheidungen aus Sprint 0

| ADR | Entscheidung |
|-----|--------------|
| 001 | Code-Baseline: 4.14.1 strikt + transitive Dep-Updates |
| 002 | TFM: net10.0 für alle Hauptprojekte, netstandard2.0 für DataCollector |
| 003 | Repo-Identität: `stryker-netx` Suffix, Namespaces bleiben `Stryker.*`, CLI `dotnet stryker-netx`, SemVer ab `1.0.0-preview.1` |
| 004 | Analyzer: **Big-Bang Sprint 1** (Roslynator + Sonar + Meziantou + TWAE) |
| 005 | Test-Stack: **Voll-Migration Sprint 1** (MSTest→xUnit, Shouldly→FluentAssertions) |
| 006 | NativeAOT: tauglich aber nicht erzwungen |
| 007 | McMaster: HYBRID (v5.1.0 belassen + Wrapper-Layer + Migration-Trigger-Liste) |
| 008 | License: Apache 2.0 + NOTICE + DCO via CONTRIBUTING.md |
| 009 | NuGet-Update-Plan: **Buildalyzer 8.0 → 9.0.0** (kritischer Fix) + Microsoft.* auf .NET-10 |
| 010 | MsBuildHelper: vswhere-Fallback entfernen, Default `dotnet msbuild` |
| 011 | Subagent-Dispatching: PILOT (Stryker.Abstractions) + DAG-LAYER-PARALLEL (7 Phasen) |
| 012 | Architektur-Layering: 5 Schichten + ArchUnitNET-Tests |

## Dokumenten-Index

| Datei | Inhalt |
|-------|--------|
| [DEEP_MEMORY.md](DEEP_MEMORY.md) | 360° Deep Memory — Vision, Stack, Architektur, Roadmap, Risiken, Stryker-Background |
| [CLAUDE.md](CLAUDE.md) | Verbindliche Direktiven (Tool-Nutzung, Subagent-Policy, Quality-Gates) |
| [_config/development_process.md](_config/development_process.md) | Scrum-basierter Entwicklungsprozess |
| [_misc/git-setup-for-claude-code.md](_misc/git-setup-for-claude-code.md) | Windows git+gh Setup |
| [.sprint/state.md](.sprint/state.md) | Aktueller Sprint-State (Hook-Steuerung) |
| [_reference/stryker-4.14.1/](_reference/stryker-4.14.1) | Original-Source als read-only Portierungs-Baseline |

## Surprising / Non-Obvious (verifiziert in Sprint 0)

- **Stryker.NET 4.14.1 ist BEREITS auf `net8.0`**, nicht netcoreapp3.1 oder net48 (das veraltete README sagt anderes). Es nutzt Microsoft.Extensions.* 10.0.5, Microsoft.TestPlatform 18.4.0, Microsoft.Testing.Platform 1.5.2 und VisualStudio.SolutionPersistence 1.0.52.
- **Alle drei recherchierten Master-Bug-Fix-PRs (#3375, #3383, #3471) sind BEREITS in 4.14.1 enthalten** (4.14.1 release: 2026-04-10; PRs gemerged 2025-12 bis 2026-03). Damit kollabierte „Cherry-Pick-Hybrid" zu „strikt 4.14.1 + Dependency-Updates".
- **Buildalyzer 9.0.0 wurde am 2026-04-18 released — exakt 8 Tage NACH Stryker.NET 4.14.1.** Das ist die echte Hauptursache der .NET-10-Inkompatibilität.
- **`MsBuildHelper.cs` Z. 67–69** in 4.14.1 hat den `vswhere`/`MsBuild.exe`-Fallback-Bug noch drin — greift nur auf Win-ohne-VS, nicht in Linux-Docker.
- **Stryker-Tests verwenden MSTest + Shouldly + Moq**, kein xUnit. Migration auf unseren Stack ist mechanisch aber umfangreich (~6 Test-Projekte, ~1000+ Test-Methoden).
- **`.claude/settings.json` ist im `bypassPermissions`-Modus** — alle CLAUDE.md-Direktiven sind reine Konvention, kein Harness-Enforcement.
- **FS MCP Server wurde komplett entfernt** (war für Projekt-Root falsch konfiguriert).
- **Sequential Thinking MCP-Naming-Inkonsistenz**: CLAUDE.md sagt `mcp__sequential-thinking-maxential__*`, real ist `mcp__maxential-cot-mcp-server__*`. Tool-Funktionen identisch.
- **CODE_OF_CONDUCT.md verweist auf Contributor Covenant 2.1 (URL)** statt vollen Text inline — sauberer + vermeidet Content-Filter-Hits beim Auto-Generieren.

## Sprint-1-Roadmap (geplant — startet nach diesem Commit)

7 Phasen, geschätzte Realdauer 4–6 Wochen:

| Phase | Dauer | Inhalt |
|-------|-------|--------|
| 0 | ~½ Tag | Repo-Bootstrap (.slnx, global.json, .editorconfig, Directory.Build.props, Directory.Packages.props) |
| 1 | ~1–2 Tage | PILOT Stryker.Abstractions (seriell, Lessons-Doku) |
| 2 | ~3–5 Tage | Layer 0 parallel (2 Subagents: Utilities, DataCollector) |
| 3 | ~5–7 Tage | Layer 1 parallel (4 Subagents: Configuration, RegexMutators, Solutions, TestRunner) |
| 4 | ~3–5 Tage | Layer 2 parallel (2 Subagents: TestRunner.MTP, TestRunner.VsTest) |
| 5 | ~5–7 Tage | Stryker.Core dediziert (Buildalyzer 9 + MsBuildHelper-Fix) |
| 6 | ~2–3 Tage | Stryker.CLI + Identitäts-Migration (Tool-Rename, IStrykerCommandLine-Wrapper) |
| 7 | ~2–3 Tage | Integration: ArchUnit-Tests, FsCheck, BenchmarkDotNet, ExampleProjects-Smoke-Tests |

## Risiken (Top-5 aus 12)

- **R1**: Buildalyzer-9-API-Migration könnte Code-Refactor in Stryker.Core erzwingen (Phase 5)
- **R2**: TWAE + 3 Analyzer können 1500+ Initial-Issues produzieren — Pilot-Lessons-Doku als Mitigation
- **R3**: MSTest-spezifische Patterns ([ClassInitialize], [ExpectedException]) brauchen manuelle Migration
- **R4**: Roslyn 5.3 → C#-14-fähige Version kann Breaking Changes bringen
- **R8**: ExampleProjects in `_reference/.../ExampleProjects/` als Smoke-Test können durch Refactors brechen

Vollständige Risikoliste: [Architecture Spec Sektion 7](_docs/architecture%20spec/architecture_specification.md#7-risiken-und-technische-schulden).

## Offene Punkte für Sprint 1

- [ ] `.sprint/state.md` für Sprint 1 anlegen (`current_sprint: "1"`, branch `feature/<issue>-bootstrap-and-cleanup`)
- [ ] GitHub Issues anlegen: 1 Epic „Sprint-1 Mega-Sprint Bootstrap+Cleanup+TestStackMigration", 7 Sub-Issues (1 pro Phase)
- [ ] Phase 0 starten: Repo-Bootstrap-Files anlegen
