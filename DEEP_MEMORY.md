# stryker-netx — DEEP MEMORY (360°)

> Vollständiger Kontext für Mensch und KI. Dieses Dokument geht über die operativen Direktiven der CLAUDE.md hinaus und beschreibt **Vision, Hintergrund, Architektur, Toolchain, Roadmap, Risiken**. Wird kontinuierlich verdichtet — *umso mehr Memory, umso besser*.
>
> **Lese-Reihenfolge bei Session-Start:** [MEMORY.md](MEMORY.md) (Index) → DEEP_MEMORY.md (dieses Dokument) → [CLAUDE.md](CLAUDE.md) (Direktiven) → [_config/development_process.md](_config/development_process.md) (Prozess).

---

## 1. Vision & Mission

### 1.1 Problem
Stryker.NET ist der etablierte Mutation-Testing-Framework für die .NET-Welt — entwickelt vom Stryker-Mutator-Team (NodeJS-Stryker, Stryker4s, Stryker.NET als Geschwister-Projekte). Die aktuelle Version **4.14.1** unterstützt offiziell nur:

- .NET Framework 4.8
- .NET Core 3.1
- .NET Standard 1.3 (Mindest-Target)

**Konsequenz**: Projekte auf **.NET 10** können Stryker.NET nicht produktiv einsetzen. Das ist ein blocker für moderne .NET-Codebasen, die von Mutation Testing profitieren würden.

### 1.2 Ziel
**stryker-netx** ist eine vollständige Portierung von Stryker.NET 4.14.1 auf:

- **C# 14** (Sprachfeatures: Primary Constructors, Collection Expressions, `field`-Keyword, ref struct interfaces, etc.)
- **.NET 10** (Runtime-Features: NativeAOT, neue BCL-APIs, performance-Optimierungen)

Die Portierung ist **kein Fork mit minimalem Patch**, sondern eine **strukturierte Modernisierung**, die:
- die ursprüngliche Funktionalität vollständig erhält (Mutator-Set, Reporters, Test-Runner-Adapter, CLI-Kompatibilität)
- moderne C#-14- und .NET-10-Features einsetzt, wo sie Wert schaffen
- die Code-Qualität durch strikte Analyzer-Konfiguration und umfassende Test-Pyramide erhöht
- die Performance via BenchmarkDotNet messbar macht

### 1.3 Nicht-Ziele
- Keine Änderung der Stryker-Konventionen (Stryker-Config-Format, Reporter-Output-Schema, CLI-Flags) ohne starke Begründung — Migration für bestehende Stryker-Nutzer soll trivial bleiben
- Keine Breaking Changes an der HTML-Report-Struktur (kompatibel mit `mutation-testing-elements`)
- Keine Unterstützung von .NET Framework 4.8 oder .NET Core 3.1 mehr — nur .NET 10+

---

## 2. Stryker.NET — Hintergrund

### 2.1 Was ist Mutation Testing?
Mutation Testing misst die Qualität von Tests, indem es **kleine, gezielte Änderungen ("Mutationen")** in den Produktionscode einfügt und prüft, ob die existierenden Tests die Mutation entdecken (sie "töten"). Wenn die Tests trotz Mutation grün bleiben, deckt die Test-Suite den betroffenen Code nicht ausreichend ab.

**Klassische Mutationen:**
- `==` → `!=` (Comparison Mutation)
- `+` → `-` (Arithmetic Mutation)
- `if (x)` → `if (true)` / `if (false)` (Condition Mutation)
- `return value` → `return null` / `return default` (Return Mutation)
- `string.Empty` → `"Stryker was here!"` (String Literal Mutation)
- Boolean Negation, Logical Operator Swap, etc.

**Mutation Score** = (getötete Mutanten) / (alle Mutanten) — höher ist besser.

### 2.2 Stryker.NET 4.14.1 — Architektur (aus _reference/)
Die Solution besteht aus **17 Projekten**:

| Projekt | Verantwortung |
|---------|---------------|
| `Stryker.Abstractions` | Interfaces, Abstrakte Modelle, geteilte Types |
| `Stryker.CLI` | Command-Line-Interface, Entry Point (`dotnet stryker`) |
| `Stryker.CLI.UnitTest` | Tests für CLI |
| `Stryker.Configuration` | Config-Loader (YAML/JSON/CLI-Args), Schema-Validierung |
| `Stryker.Core` | Hauptlogik: Orchestrator, Mutator-Engine, Diff-Logic, Reporting |
| `Stryker.Core.UnitTest` | Tests für Core |
| `Stryker.DataCollector` | Coverage-Datensammlung während Test-Runs (VsTest-Adapter) |
| `Stryker.RegexMutators` | Spezialisierte Mutatoren für Regex-Pattern |
| `Stryker.RegexMutators.UnitTest` | Tests dafür |
| `Stryker.Solutions` | Solution-/csproj-Parsing, Projekt-Dependency-Graph |
| `Stryker.Solutions.Test` | Tests dafür |
| `Stryker.TestRunner` | Abstraktion über Test-Frameworks |
| `Stryker.TestRunner.MicrosoftTestPlatform` | MTP-Adapter |
| `Stryker.TestRunner.MicrosoftTestPlatform.UnitTest` | Tests dafür |
| `Stryker.TestRunner.VsTest` | VsTest-Adapter (Hauptpfad in Praxis) |
| `Stryker.TestRunner.VsTest.UnitTest` | Tests dafür |
| `Stryker.Utilities` | Hilfsklassen (FileSystem-Wrapper, Logging-Helper, etc.) |

**Schichten-Hierarchie (Annahme — in Sprint 0 zu verifizieren):**
```
Stryker.CLI
    ↓
Stryker.Core ←→ Stryker.Configuration
    ↓                ↓
Stryker.Solutions, Stryker.TestRunner.*, Stryker.RegexMutators, Stryker.DataCollector
    ↓
Stryker.Abstractions, Stryker.Utilities
```

### 2.3 Externe Abhängigkeiten (Stryker 4.14.1 — Kernkomponenten)
- **Microsoft.CodeAnalysis (Roslyn)** — Code-Parsing, AST-Manipulation, Mutationen-Injection
- **Buildalyzer / MsBuild API** — Solution/Project-Analyse
- **Spectre.Console** — CLI-Output, Progress-Bars
- **Microsoft.TestPlatform / VsTest** — Test-Execution
- **Newtonsoft.Json** (vermutlich) oder System.Text.Json — Config/Report-Serialization
- **Serilog** — Strukturiertes Logging
- **mutation-testing-elements** (npm/CDN) — HTML-Report-Frontend

→ **In Sprint 0 zu verifizieren** durch Lesen der `.csproj`-Dateien unter `_reference/stryker-4.14.1/src/`. Manche Pakete brauchen .NET-10-kompatible Versionen.

### 2.4 Was muss portiert werden? (high-level)
- **API-Surface**: ~15 öffentliche Top-Level-Interfaces in `Stryker.Abstractions`
- **Mutator-Klassen**: ~30 Mutator-Implementierungen (Statement-, Expression-, Regex-Level)
- **Reporter-Klassen**: ~10 (HTML, JSON, Console, Progress, Dashboard, Baseline, ClearText, etc.)
- **Test-Runner-Adapter**: 2 (VsTest, MicrosoftTestPlatform)
- **Configuration-Loader**: YAML/JSON/CLI-Merging, Defaults, Validation
- **CLI-Frontend**: Spectre.Console-basiert, ~20 Command-Line-Optionen

---

## 3. Stack & Toolchain

### 3.1 Sprache und Runtime
- **C# 14** — neue Sprachfeatures aktiv nutzen, wo sie Mehrwert bringen:
  - Primary Constructors (Boilerplate-Reduktion)
  - Collection Expressions (`[1, 2, 3]`)
  - `field`-Keyword für Property-Backing-Fields
  - `ref struct` mit Interface-Implementation
  - `params` für `IEnumerable<T>` / `Span<T>`
- **.NET 10** — Runtime-Features:
  - NativeAOT-Kompatibilität anstreben (für CLI-Tool eine echte Verbesserung)
  - Neue BCL-APIs (z.B. `System.Text.Json` Source-Generation, neue `LINQ`-APIs)
  - Performance-Verbesserungen in Roslyn-APIs

### 3.2 Build & Tooling
- **Solution-Format**: `.slnx` (XML-basiertes Solution-Format, neu in Visual Studio / dotnet CLI 17.10+)
- **SDK-Pinning**: `global.json` (`{ "sdk": { "version": "10.0.100" } }`)
- **Code-Style**: `.editorconfig` mit Naming-Conventions, Severity-Overrides — wird von Roslyn-Analyzern ausgewertet
- **MSBuild Properties**: zentral in `Directory.Build.props`

### 3.3 Analyzer (im Build aktiv)
- **Roslynator.Analyzers v4.15.0** — Code-Qualität, Vereinfachungen, Best Practices
- **SonarAnalyzer.CSharp v10.20.0** — Security, Reliability, Maintainability (~600 Regeln)
- **Meziantou.Analyzer v3.0.22** — .NET-spezifische Best Practices, Performance-Pitfalls

→ `TreatWarningsAsErrors=true` ist **aktiv**. Jede Warnung blockiert den Build. Kein `#pragma warning disable` ohne dokumentierte Begründung im Code-Kommentar direkt darüber.

### 3.4 Test-Stack
| Paket | Version | Zweck |
|-------|---------|-------|
| `xunit` | 2.9.3 | Test-Framework |
| `xunit.runner.visualstudio` | 3.1.4 | VS-Runner-Adapter |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | dotnet test-Adapter |
| `FluentAssertions` | 8.8.0 | Lesbare Assertions (PFLICHT statt `Assert.Equal`) |
| `Moq` | 4.20.72 | Mocking (Vorsicht: keine sealed/static — sealed by default!) |
| `coverlet.collector` | 8.0.0 | Code-Coverage (immer mit `--collect:"XPlat Code Coverage"`) |
| `TngTech.ArchUnitNET.xUnit` | 0.11.0 | Architektur-Tests (Schichten-Regeln als Tests) |
| `FsCheck.Xunit` | 3.1.0 | Property-Based Testing (Roundtrip, Invarianten, Edge Cases) |
| `BenchmarkDotNet` | 0.14.0 | Performance-Benchmarks (in separatem `benchmarks/`-Projekt, nur Release-Mode) |

### 3.5 Logging & Konfiguration
- **Logging**: Serilog (strukturiert, mit Sinks für File + Console)
- **Konfiguration**: YAML- und JSON-basierte Config-Files; CLI-Args überschreiben Config-Werte (Stryker-Konvention behalten)

### 3.6 MCP-Tooling
| Tool | Zweck | Pflicht-Trigger |
|------|-------|-----------------|
| **Serena** | Symbolbasierte Code-Analyse via Roslyn (OmniSharp/C# LSP) | IMMER vor Grep für Klassen/Methoden/Properties |
| **Semgrep** | Security-Scanning | Vor jedem Sprint-Abschluss + bei security-relevantem Code |
| **Context7** | Aktuelle API-Doku (NuGet-Pakete, .NET-APIs) | Vor Nutzung neuer APIs / bei Versionswechseln |
| **Sequential Thinking** (MCP-Server-Name: `maxential-cot-mcp-server`) | Branching-Reasoning für komplexe Entscheidungen | ≥10 Schritte bei Architektur, ≥8 bei Algorithmen, ≥3 bei Trade-offs |
| **GitHub CLI (`gh`)** | Mehrstufige Git-Workflows (Branch+PR+Push, Tag+Release, Issue) | Statt manuelle Bash-Sequenzen |

> **Ehemals geplant, entfernt:** FS MCP Server (`fs-mcp-server`) — war für das Projekt-Root falsch konfiguriert (resolved gegen `C:\WINDOWS`, registrierte nur 5 von angeblich 138 Tools). Built-In Read/Edit/Write/Glob/Grep haben übernommen.

### 3.7 Permissions / Enforcement
- **`.claude/settings.json` läuft im `bypassPermissions`-Modus** — keine deny/allow-Liste, alle Tool-Calls erlaubt
- Filesystem-Direktiven (Built-In Tools statt Bash, Serena vor Grep, etc.) sind **reine Projekt-Konvention** — kein Harness-Enforcement
- Subagenten erben dieselben Permissions; CLAUDE.md-Direktiven MÜSSEN explizit im Subagent-Prompt mitgegeben werden

---

## 4. Architektur-Vision (für stryker-netx)

> Nur Vision — wird in Sprint 0 als ADRs verfeinert in `_docs/architecture_spec/architecture_specification.md`.

### 4.1 Layering-Prinzip
**Clean Architecture** mit klarer Schicht-Trennung:

```
src/
  Stryker.NetX/                   # Application Layer (CLI Entry, Composition Root)
    Program.cs
  Stryker.NetX.Core/              # Use Cases / Orchestration
  Stryker.NetX.Domain/            # Domain Models, Interfaces, Pure Logic (Mutators!)
  Stryker.NetX.Infrastructure/    # Roslyn, Buildalyzer, FileSystem, TestRunner
  Stryker.NetX.Reporters/         # Reporter-Implementierungen
  Stryker.NetX.TestRunners/       # Test-Runner-Adapter (VsTest, MTP)
  Stryker.NetX.Configuration/     # Config-Loading
```

→ **In Sprint 0 zu entscheiden**: Behalten wir die ursprüngliche 17-Projekte-Struktur (1:1-Portierung) oder konsolidieren wir? **Trade-off**: Konsolidierung erhöht Wartbarkeit, behindert aber Drop-In-Replacement. Sequential Thinking ≥10 Schritte!

### 4.2 ArchUnitNET-Regeln (Vorab-Vision)
- `Domain` darf **nicht** auf `Infrastructure` zugreifen
- `Domain` darf **nicht** auf `Roslyn`-Types direkt zugreifen (Adapter-Pattern via Interfaces)
- `Application` darf **nicht** auf `Infrastructure` direkt zugreifen (nur via DI)
- Mutator-Klassen müssen `IMutator` implementieren und `sealed` sein
- Reporter-Klassen müssen `IReporter` implementieren und `sealed` sein

### 4.3 Code-Standards (siehe CLAUDE.md)
- `sealed` als Default für alle Klassen, die nicht zur Vererbung gedacht sind
- XML-Doc-Kommentare auf allen öffentlichen APIs
- `ConfigureAwait(false)` auf allen `await`-Calls
- Exception-Pattern: `catch (Exception ex) when (ex is not OperationCanceledException) { ... }`
- Namespace folgt Verzeichnisstruktur (Roslynator-erzwungen)
- Primary Constructors bei Records / kleinen Service-Klassen
- Collection Expressions statt `new List<T> { ... }` wo möglich

---

## 5. Entwicklungsprozess (Scrum-basiert)

> Vollständig in [_config/development_process.md](_config/development_process.md). Kurzfassung hier.

### 5.1 Sprint 0 — Architektur & Design (aktuell)
- **Brainstorming**: Portierungsstrategie, Mutator-Auswahl, Modul-Reihenfolge, Compat-Goals — Skill: `brainstorming` + Sequential Thinking
- **Architektur**: ADRs (Architecture Decision Records) — Skill: `architecture-designer`
  - ADR-001: Solution-Struktur (1:1 vs konsolidiert)
  - ADR-002: DI-Container (Microsoft.Extensions.DependencyInjection vs. anders)
  - ADR-003: Logging-Strategie (Serilog vs. Microsoft.Extensions.Logging)
  - ADR-004: Test-Runner-Adapter (VsTest beibehalten? MTP als Default?)
  - ADR-005: Konfigurations-Format (YAML, JSON, beide?)
  - ADR-006: NativeAOT-Kompatibilität (Constraints für Reflection/Dynamic-Code)
- **Software Design**: FRs + NFRs — Skill: `write-spec`
  - FRs: Mutator-Engine, Reporter-Pipeline, Test-Runner-Abstraktion, CLI-Frontend, Config-Loader
  - NFRs: Performance (Mutation-Run-Time), Compat (Stryker-Config-Format), Security, Diagnostics

### 5.2 Product Backlog (Nach Sprint 0)
- DoD (übergreifend, alle Sprints)
- Epics → GitHub Milestones
- Features (Sammlungen von User Stories) → GitHub Issues
- User Stories → GitHub Issues
- Acceptance Criteria je Story

### 5.3 Sprints 1–N
- **Vertikal/E2E**: Jeder Sprint produziert ein lauffähiges Increment (Feature-vollständig)
- **TDD-Pflicht**: Red → Green → Refactor, keine Ausnahmen — Skill: `test-driven-development`
- **Branch**: `feature/[ISSUE-NR]-name` (GitHub Flow)
- **DoD pro Sprint**:
  - Alle Tasks implementiert
  - Alle Tests grün (Unit + Integration + ArchUnit + Property)
  - `verification-before-completion` ausgeführt
  - 0 Warnings/0 Errors (TreatWarningsAsErrors)
  - Semgrep-Scan bestanden
  - Increment lauffähig
  - Conventional Commits
- **GitHub-Mapping**: Sprint Increment ↔ `feature/*`-Branch ↔ Feature-Issue-Closure

### 5.4 Review & Integration
- `requesting-code-review` für schnellen Sanity-Check
- `pr-review` für umfassendes Pre-Merge-Review (6 spezialisierte Agents)
- `receiving-code-review` für Feedback-Verarbeitung
- `finishing-a-development-branch` für Merge/PR/Keep/Discard
- Bei Epic-Abschluss: GitHub Tag (SemVer, annotated)

---

## 6. Repository-Struktur

### 6.1 Aktuell (post-Bootstrap)
```
stryker-netx/
├── .claude/                       # Claude Code Config
│   ├── hooks/                     # 7 Bash-Hooks (sprint-health, sprint-gate, etc.)
│   ├── rules/                     # (User-defined rules)
│   ├── settings.json              # bypassPermissions, Worktree, Hook-Triggers
│   ├── settings.local.json        # gitignored — lokale Permissions
│   └── skills/                    # ~30 installierte Skills
├── .serena/
│   └── project.yml                # Serena-Projekt-Konfiguration
├── .sprint/
│   └── state.md                   # YAML-Frontmatter, hook-gesteuert
├── _config/
│   └── development_process.md     # Scrum-Prozess
├── _docs/
│   ├── architecture_spec_template.md
│   ├── product_backlog_template.md
│   ├── software_design_spec_template.md
│   └── sprint_backlog_template.md
├── _misc/
│   └── git-setup-for-claude-code.md
├── _reference/
│   └── stryker-4.14.1/            # Original Stryker.NET (17 Projekte) — read-only Baseline
├── .gitignore
├── CLAUDE.md                      # Verbindliche Direktiven
├── DEEP_MEMORY.md                 # ← dieses Dokument
├── Directory.Build.props          # Roslynator + Sonar + Meziantou + TreatWarningsAsErrors
├── MEMORY.md                      # Memory-Index
└── README.md
```

### 6.2 Geplant (Sprint 0+ ergänzt)
```
+ global.json                      # SDK-Pinning auf 10.0.x
+ .editorconfig                    # Code-Style + Naming
+ stryker-netx.slnx                # Solution-Datei
+ src/                             # zu definieren in Sprint 0 (ADR-001)
+ tests/
+ benchmarks/
+ _docs/architecture spec/architecture_specification.md
+ _docs/design spec/software_design_specification.md
+ _docs/product_backlog.md         # Nach Sprint 0
```

---

## 7. Risiken & Annahmen

| # | Risiko / Annahme | Mitigation |
|---|------------------|------------|
| R1 | **.NET 10 ist neu** — APIs/Tooling können sich noch ändern | global.json pinnen, Context7 vor jedem API-Use, NuGet-Versions explizit pinnen |
| R2 | **`.slnx` Tooling-Support** unklar (manche IDEs / dotnet-Versionen unterstützen es noch nicht 100%) | Check in Sprint 0 ob lokales `dotnet build` mit `.slnx` funktioniert; ggf. `.sln` als Fallback |
| R3 | **Roslyn/Microsoft.CodeAnalysis** muss .NET-10-kompatible Version haben | In Sprint 0: `dotnet list package --outdated` auf einer Test-Solution |
| R4 | **Buildalyzer/MsBuild API** kann inkompatibel sein | Eventuell durch direkte MSBuild-API ersetzen, falls Buildalyzer hinterherhinkt |
| R5 | **VsTest-Adapter** — VsTest ist Legacy, MTP ist die neue Generation; Stryker.NET 4.14.1 supportet beide | ADR in Sprint 0: behalten wir beide oder fokussieren auf MTP? |
| R6 | **NativeAOT-Kompatibilität** — Reflection-Heavy Code (Mutator-Discovery via Reflection) ist AOT-feindlich | Source-Generators erwägen, oder NativeAOT als optionales Feature |
| R7 | **Spectre.Console-Version** — Major-Updates können Breaking Changes haben | Pin Version, Context7 prüfen |
| R8 | **HTML-Report-Kompatibilität** — `mutation-testing-elements` Schema muss passen | Schema-Tests in Integration-Suite |
| R9 | **TreatWarningsAsErrors** kann den Sprint blockieren wenn neue Analyzer-Regeln plötzlich aufschlagen | Dependency-Updates kontrolliert (in einem dedizierten Maintenance-Sprint) |
| R10 | **Performance-Regression** vs. Stryker.NET 4.14.1 möglich, durch Modernisierung | BenchmarkDotNet von Anfang an für Hot Paths |
| R11 | **Stryker-Config-Format** muss kompatibel bleiben — bestehende Nutzer migrieren sonst nicht | Compat-Tests gegen reale Stryker-Configs (aus `_reference/stryker-4.14.1/ExampleProjects`) |

---

## 8. Tooling-Setup-Status

| Komponente | Status | Notiz |
|------------|--------|-------|
| .NET 10 SDK | ⚠ Verifikation nötig | `dotnet --version` in Sprint 0 prüfen |
| Git for Windows | ✓ Konfiguriert | sslBackend=openssl, credential.helper=store |
| GitHub CLI (`gh`) | ✓ Auth als pgm1980 | scopes: admin:*, repo, user, workflow |
| Repo `pgm1980/stryker-netx` | ✓ Erstellt (privat), 2 Commits gepusht | Default branch: `main` |
| Serena MCP | ✓ Verfügbar | Roslyn/OmniSharp-basiert |
| Semgrep CLI | ⚠ Verifikation nötig | `semgrep --version` in Sprint 0 prüfen |
| Context7 MCP | ✓ Verfügbar | Naming: `mcp__context7__*` |
| Sequential Thinking MCP | ✓ Verfügbar | Naming: `mcp__maxential-cot-mcp-server__*` (CLAUDE.md hat anderen Namen — Doku-Inkonsistenz) |
| FS MCP Server | ✗ Entfernt | War defekt für Projekt-Root |
| Hooks (`.claude/hooks/`) | ✓ Aktiv | sprint-health, sprint-gate, statusline, sprint-state-save, post-compact-reminder, sprint-housekeeping-reminder, verify-after-agent |

---

## 9. Externe Referenzen

- **Stryker-Mutator Hauptseite**: https://stryker-mutator.io/
- **Stryker.NET Docs**: https://stryker-mutator.io/docs/stryker-net/
- **Stryker.NET GitHub**: https://github.com/stryker-mutator/stryker-net
- **Stryker.NET 4.14.1 Source (lokal)**: [_reference/stryker-4.14.1/](_reference/stryker-4.14.1/)
- **mutation-testing-elements** (HTML-Report-Frontend): https://github.com/stryker-mutator/mutation-testing-elements
- **Stryker.NET Slack** (Community): https://join.slack.com/t/stryker-mutator/shared_invite/...
- **C# 14 Spec / Whats new**: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14
- **.NET 10 What's New**: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview

---

## 10. Konventionen-Glossar

| Begriff | Bedeutung |
|---------|-----------|
| **PFLICHT / VERBOTEN** | Nicht-verhandelbare CLAUDE.md-Direktive |
| **Sprint 0** | Architektur- und Design-Sprint, kein Code |
| **Sprints 1..N** | Implementation-Sprints, jeder produziert ein lauffähiges Increment |
| **Epic** | GitHub Milestone, entspricht einem MVP |
| **Feature** | Sammlung User Stories, GitHub Issue mit Sub-Issues |
| **User Story** | Einzelne implementierbare Einheit, GitHub Issue mit Acceptance Criteria |
| **Increment** | Funktionsfähiges, getestetes Ergebnis eines Sprints |
| **DoD** | Definition of Done — pro Sprint und übergreifend |
| **PBI** | Product Backlog Item |
| **ADR** | Architecture Decision Record (in `architecture_specification.md`) |
| **FR/NFR** | Functional / Non-Functional Requirement (in `software_design_specification.md`) |

---

## 11. Glossar Stryker-spezifisch

| Begriff | Bedeutung |
|---------|-----------|
| **Mutator** | Klasse, die eine bestimmte Code-Mutation produziert (z.B. `EqualityMutator`, `BooleanMutator`) |
| **Mutant** | Konkrete Mutation einer Quelldatei an einer bestimmten Stelle |
| **Killed Mutant** | Tests haben die Mutation entdeckt (gut) |
| **Survived Mutant** | Tests haben die Mutation NICHT entdeckt (Lücke in Tests) |
| **Timeout Mutant** | Mutation führte zu Endlosschleife / Hang (zählt meist als killed) |
| **No-Coverage Mutant** | Mutation in Code, der nicht durch Tests abgedeckt ist |
| **Mutation Score** | (killed + timeout) / (total - no-coverage) — Qualitätsmaß |
| **Reporter** | Output-Format (Console, HTML, JSON, Dashboard, Baseline, Progress, ClearText) |
| **Test Runner** | Adapter zu Test-Framework (VsTest, MicrosoftTestPlatform/MTP) |
| **DataCollector** | Komponente, die während Test-Runs Coverage-Daten sammelt (für VsTest-Pfad) |
| **Baseline** | Referenz-Mutation-Run, gegen den neue Runs differential verglichen werden |

---

## 12. Update-Log

| Datum | Änderung |
|-------|----------|
| 2026-04-29 | Initial-Erstellung als Sprint-0-Baseline (Bootstrap, FS-MCP-Entfernung, Git-Setup, Repo-Init) |
