# stryker-netx — Project Overview

## Purpose
Port von [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 auf **C# 14 / .NET 10**.
Upstream kann .NET-10-Projekte nicht mutieren; stryker-netx fixt das — Kernunterschied zur
Ursprungsidee: **Buildalyzer wurde komplett durch MSBuildWorkspace ersetzt** (Phase 9b,
`Microsoft.Build.Locator.RegisterDefaults()` in Program.cs). CLI/Config/Reporter bleiben
upstream-kompatibel; der Mutatoren-Katalog ist auf 52 Operatoren erweitert (upstream: 40)
mit Profil-System Defaults/Stronger/All (ADR-018) × MutationLevel (ADR-025-Auto-Bump).

## Stand (2026-06-12, nach Sprint 179)
- **Aktuelle Version: v3.3.4** (Tags = SemVer auf Squash-Merge-Commits; VersionPrefix im Repo ist 0.0.0-localdev, Release-Version kommt aus dem Tag via release.yml)
- 360°-Analyse-Programm (Sprints 173–178) KOMPLETT: ~430 Dateien, 139 Findings, Synthese in `_docs/analysis/sprint_178_synthesis.md`
- Fix-Ära läuft: Fahrplan Sprints 179–184; 179 (Quick-Wins+P1) shipped
- Live-Stand IMMER führend in `.sprint/state.md` (Wahrheitsquelle Nr. 1), dann `MEMORY.md`/GitHub-Issues — diese Serena-Memory beschreibt nur Stabiles

## Identity (ADR-003)
- NuGet: `stryker-netx` (lib), `dotnet-stryker-netx` (Tool; Aufruf `dotnet stryker-netx`)
- Namespaces `Stryker.*` 1:1 erhalten (API-Kompat)
- Repo: `pgm1980/stryker-netx` (privat), Default-Branch `main`
- Upstream-Referenzquelle read-only unter `_references/stryker-net/`

## Schlüsseldokumente
- `CLAUDE.md` — verbindliche Direktiven (Serena-first, TDD, TWAE, Semgrep, FluentAssertions …)
- `_docs/architecture spec/architecture_specification.md` — ADR-001…053 + Änderungshistorie
- `_docs/analysis/sprint_173..178_*.md` — Findings-Register + Synthese (Bug-Backlog-Basis)
- `.sprint/state.md` — Sprint-Status (Hook-gesteuert, FÜHREND)
- `MEMORY.md` (Index) + Claude-Memory-Verzeichnis — Session-übergreifendes Gedächtnis

## Architektur in einem Satz
Alle Mutationen werden in EIN Assembly kompiliert und zur Laufzeit per
`MutantControl.IsActive(id)` geschaltet (if/else-Wrap für Statements/Blöcke,
Ternary für Expressions); CompileError-Mutanten entfernt der RollbackProcess
in bis zu 50 Re-Emit-Runden.
