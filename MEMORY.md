# stryker-netx — Memory Index

> Einstiegspunkt zum Project Memory. **Vertiefung in [DEEP_MEMORY.md](DEEP_MEMORY.md)** — 360° Deep Level Memory mit Vision, Stack, Architektur, Stryker-Background, Roadmap, Toolchain, Risiken.

## Baseline (Stand: 2026-04-29)

- **Projekt**: Portierung von Stryker.NET 4.14.1 auf C# 14 / .NET 10 (Original ist nur kompatibel zu .NET Framework 4.8 und .NET Core 3.1)
- **Repo**: [pgm1980/stryker-netx](https://github.com/pgm1980/stryker-netx) (privat, GitHub Flow)
- **User**: GitHub-Account `pgm1980`, lokale git-Identität konfiguriert (`pgm1980@users.noreply.github.com`), Sprache Deutsch (Anweisungen) + Englisch (Code/Commits)
- **Sprint-Phase**: Sprint 0 — Brainstorming + Architektur (ADRs) + Software-Design (FRs/NFRs)
- **Aktiver Branch**: `main` (Bootstrap & Sprint 0); ab Sprint 1 → `feature/[ISSUE-NR]-name`
- **Initial-Commits**: `f1a8de6` (Stryker.NET 4.14.1 Reference) + `ff9f14c` (Project Bootstrap)

## Dokumenten-Index

| Datei | Inhalt |
|-------|--------|
| **[DEEP_MEMORY.md](DEEP_MEMORY.md)** | 360° Deep Memory — alles was über Reine Konventionen und Code hinausgeht |
| [CLAUDE.md](CLAUDE.md) | Verbindliche Direktiven (Tool-Nutzung, Subagent-Policy, Quality-Gates) |
| [_config/development_process.md](_config/development_process.md) | Scrum-basierter Entwicklungsprozess (Sprint 0..N, GitHub-Mapping) |
| [_misc/git-setup-for-claude-code.md](_misc/git-setup-for-claude-code.md) | Windows git+gh Setup |
| [.sprint/state.md](.sprint/state.md) | Aktueller Sprint-State (Hook-Steuerung) |
| [_reference/stryker-4.14.1/](_reference/stryker-4.14.1) | Original Stryker.NET Source als read-only Portierungs-Baseline |

## Aktive Konventionen (TL;DR — Details in CLAUDE.md)

- **Stack**: C# 14 / .NET 10, `.slnx` Solution-Format, `global.json` für SDK-Pinning
- **Test-Stack**: xUnit + FluentAssertions + FsCheck + ArchUnitNET + Coverlet + BenchmarkDotNet + Moq
- **Analyzers**: Roslynator + SonarAnalyzer + Meziantou — `TreatWarningsAsErrors=true`, 0 Warnings/0 Errors
- **MCP-Tooling**: Serena (Roslyn) vor Grep für Code-Symbole, Context7 vor neuen APIs, Semgrep vor Sprint-Abschluss, Sequential Thinking (Maxential) ≥10 Schritte bei Architektur
- **Git**: GitHub CLI (`gh`) für mehrstufige Workflows, Conventional Commits, GitHub Flow, SemVer Tags
- **Code**: `sealed` default, `ConfigureAwait(false)`, `catch (Exception ex) when (ex is not OperationCanceledException)`, XML-Doc auf öffentlichen APIs

## Surprising / Non-Obvious

- **`.claude/settings.json` ist im `bypassPermissions`-Modus** — keine deny/allow-Liste, keine FS MCP Server. Filesystem-Direktiven aus CLAUDE.md sind reine Konvention, kein Harness-Enforcement.
- **FS MCP Server wurde komplett entfernt** (war für `C:\claude_code\stryker-netx` falsch konfiguriert, registrierte nur 5 von angeblich 138 Tools). Built-In Read/Edit/Write/Glob/Grep sind primäre Filesystem-Tools.
- **Sequential Thinking MCP-Naming-Inkonsistenz**: CLAUDE.md spricht von `mcp__sequential-thinking-maxential__*`, der tatsächlich verfügbare Server heißt `mcp__maxential-cot-mcp-server__*`. Tool-Funktionen (`think`/`branch`/`merge_branch`/`complete`) sind 1:1 vorhanden.
- **`_reference/stryker-4.14.1/` ist KEIN nested git repo** — kann normal committet werden, kein Submodul-Setup nötig.
- **Stryker.NET 4.14.1 ist eine 17-Projekt-Solution** mit klarer Modul-Trennung (Abstractions, CLI, Configuration, Core, DataCollector, RegexMutators, Solutions, TestRunner + 3 Adapter-Projekte für VsTest/MTP, Utilities). Portierungsstrategie sollte modulweise (vertikal) erfolgen — siehe DEEP_MEMORY.md.
- **`global.json` und `.slnx` und `.editorconfig` fehlen noch** im Repo — werden in Sprint 0 / Sprint 1 angelegt.

## Offene To-Dos (Sprint 0)

- [ ] Sprint 0 Brainstorming starten (Skill: `brainstorming`) — Portierungsstrategie, Mutator-Auswahl, Modul-Reihenfolge, Compat-Goals
- [ ] `architecture_specification.md` mit ADRs erzeugen (Skill: `architecture-designer` + Sequential Thinking ≥10 Schritte)
- [ ] `software_design_specification.md` mit FRs/NFRs erzeugen (Skill: `write-spec`)
- [ ] Product Backlog ableiten → GitHub Issues + Milestones
- [ ] `global.json`, `.slnx`, `.editorconfig` anlegen (zu Sprint 1 oder Tail Sprint 0)
