# stryker-netx — DEEP MEMORY (360°)

> Vollständiger Projekt-Kontext. Geht über die operativen Direktiven der CLAUDE.md hinaus und beschreibt **Vision, Hintergrund, Architektur, Toolchain, Roadmap, Risiken**. Kontinuierlich verdichtet — *umso mehr Memory, umso besser*.
>
> **Lese-Reihenfolge bei Session-Start:** [MEMORY.md](MEMORY.md) (Index) → DEEP_MEMORY.md (dieses Dokument) → [CLAUDE.md](CLAUDE.md) (Direktiven) → [_config/development_process.md](_config/development_process.md) (Prozess) → [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md) (12 ADRs).

---

## 1. Vision & Mission

### 1.1 Problem
[Stryker.NET](https://github.com/stryker-mutator/stryker-net) ist der etablierte Mutation-Testing-Framework für .NET. Die aktuelle Version **4.14.1** (released 2026-04-10) funktioniert nicht zuverlässig mit **.NET 9 / .NET 10**-Test-Projekten — verifiziert via GitHub-Issues:

1. **Buildalyzer 8.0** (transitive Dep) parst .NET-10-MSBuild-Strukturen nicht (Buildalyzer-Issue [#318](https://github.com/Buildalyzer/Buildalyzer/issues/318))
2. **MsBuildHelper-Fallback** sucht `MsBuild.exe` via `vswhere` → schlägt auf reinen .NET-10-SDK-Maschinen ohne Visual Studio fehl (stryker-net-Issue [#3351](https://github.com/stryker-mutator/stryker-net/issues/3351))
3. **C# Interceptors** — bereits in 4.14.1 PR [#3471](https://github.com/stryker-mutator/stryker-net/pull/3471) (2026-03-16) gefixt ✅
4. **DI/Logging-Init-Order** — bereits in 4.14.1 PR #3383 gefixt ✅

**Buildalyzer 9.0.0 (Fix für #318) wurde am 2026-04-18 released — 8 Tage NACH Stryker 4.14.1.** Damit ist die transitive Dep der eigentliche Blocker, kein interner Stryker-Bug.

### 1.2 Ziel
**stryker-netx** ist eine 1:1-Portierung von Stryker.NET 4.14.1 auf:
- **C# 14** (LangVersion `latest`)
- **.NET 10** (Runtime + BCL-APIs)

Die Portierung **erhält 100% der CLI-Schnittstelle, Config-Schemas und Reporter-Outputs** des Originals, **modernisiert die transitiven Dependencies** und **fixt die identifizierten .NET-10-Inkompatibilitäten**.

### 1.3 Nicht-Ziele (Sprint 1)
- Kein neues Mutator-Set (1:1 zu Upstream)
- Keine CLI-Flag-Änderungen (1:1 zu Upstream)
- Keine Config-Schema-Änderungen (1:1 zu Upstream)
- Kein NativeAOT-Erzwingen (siehe ADR-006)
- Keine McMaster-Replacement (siehe ADR-007 HYBRID)
- Keine IDE-Plugins
- Kein Visual-Basic / F# / non-C#-Source-Mutation

---

## 2. Stryker.NET — Hintergrund

### 2.1 Mutation Testing — Kurzdefinition
Mutation Testing misst die Test-Qualität, indem es **kleine, gezielte Änderungen ("Mutationen")** in den Produktionscode einfügt und prüft, ob die existierenden Tests die Mutation entdecken (sie "töten"). Wenn Tests trotz Mutation grün bleiben, deckt die Test-Suite den betroffenen Code nicht ausreichend ab.

**Klassische Mutator-Kategorien:**
- **Equality** (`==` ↔ `!=`)
- **Arithmetic** (`+` → `-`, `*` → `/`)
- **Boolean** (`true` ↔ `false`)
- **Conditional** (`if (x)` → `if (true)` / `if (false)`)
- **Block Removal** (Statements weglassen)
- **String Literal** (`"abc"` → `""`)
- **Boolean Negation, Logical Operator Swap**
- **Regex-Mutation** (Stryker-spezifisch via Stryker.Regex.Parser)

**Mutation Score** = (killed + timeout) / (total − no-coverage) — Qualitätsmetrik.

### 2.2 Stryker.NET 4.14.1 — Architektur (aus _reference/)

**17 Projekte** in der Solution:

| Projekt | Verantwortung | TFM (Original) |
|---------|---------------|----------------|
| `Stryker.Abstractions` | Interfaces, Modelle, geteilte Types | net8.0 |
| `Stryker.CLI` | Command-Line-Interface (Entry Point `dotnet stryker`) | net8.0, OutputType=Exe, PackAsTool=true |
| `Stryker.CLI.UnitTest` | Tests für CLI | net8.0 (mit MSTest) |
| `Stryker.Configuration` | Config-Loader (YAML/JSON/CLI-Args) | net8.0 |
| `Stryker.Core` | Hauptlogik: Orchestrator, Mutator-Engine, Diff, Reporting | net8.0 (mit Embedded Resources: MutantControl.cs, MutantContext.cs, mutation-test-elements.js, mutation-report.html) |
| `Stryker.Core.UnitTest` | Tests für Core | net8.0 |
| `Stryker.DataCollector` | Coverage-Datensammlung während Test-Runs | **netstandard2.0 (HARTKODIERT)** — VsTest-Adapter-Constraint |
| `Stryker.RegexMutators` | Spezialisierte Regex-Mutatoren | net8.0 |
| `Stryker.RegexMutators.UnitTest` | Tests dafür | net8.0 |
| `Stryker.Solutions` | Solution-/csproj-Parsing, Dependency-Graph | net8.0 |
| `Stryker.Solutions.Test` | Tests dafür | net8.0 |
| `Stryker.TestRunner` | Test-Runner-Abstraktion (`ITestRunner`) | net8.0 |
| `Stryker.TestRunner.MicrosoftTestPlatform` | Microsoft Testing Platform Adapter | net8.0 |
| `Stryker.TestRunner.MicrosoftTestPlatform.UnitTest` | Tests dafür | net8.0 |
| `Stryker.TestRunner.VsTest` | VsTest-Adapter | net8.0 |
| `Stryker.TestRunner.VsTest.UnitTest` | Tests dafür | net8.0 |
| `Stryker.Utilities` | Hilfsklassen (FileSystem-Wrapper, Logging-Helper) | net8.0 |

**Layering** (verifiziert via .csproj `ProjectReference`):
```
Layer 4: Stryker.CLI                                  → Layer 3
Layer 3: Stryker.Core                                  → Layer 2 + 1 + 0
Layer 2: Stryker.TestRunner.{MTP,VsTest}              → Layer 1 + 0
Layer 1: Stryker.Configuration, RegexMutators,        → Layer 0
         Solutions, TestRunner
Layer 0: Stryker.Abstractions, Stryker.Utilities,      → externe Pakete
         Stryker.DataCollector
```

### 2.3 Stryker.NET 4.14.1 — Externe Dependencies (verifiziert via Directory.Packages.props)

**Schon auf .NET-10-Versionen** (in 4.14.1):
- `Microsoft.Extensions.DependencyInjection 10.0.5`
- `Microsoft.Extensions.Logging 10.0.5` + `Logging.Abstractions 10.0.5`
- `Microsoft.TestPlatform 18.4.0` + `ObjectModel/Portable/TranslationLayer 18.4.0`
- `Microsoft.Testing.Platform 1.5.2`
- `Microsoft.VisualStudio.SolutionPersistence 1.0.52` (`.slnx`-Support)
- `System.Net.Http.Json 10.0.5`

**Zu aktualisieren** (für stryker-netx):
- **`Buildalyzer 8.0.0` → 9.0.0+** ← KRITISCHER Fix
- `Microsoft.CodeAnalysis.* 5.3.0` → C#-14-fähige Version
- Alle anderen Pakete auf neueste stable

**Bemerkenswert**:
- `McMaster.Extensions.CommandLineUtils 5.1.0` — **deprecated** (Maintainer hat Repo archiviert), v5.1.0 ist letzte stabile Version → ADR-007 HYBRID-Strategie
- `Stryker.Regex.Parser 1.0.0` — eigener Stryker-Fork eines Regex-Parsers
- `LibGit2Sharp 0.31.0` — native git-Bindings für Diff-Logik
- `AWSSDK.S3 4.0.21` + `Azure.Storage.Files.Shares 12.25.0` — für Baseline-Reporter
- `Mono.Cecil 0.11.6` — IL-Manipulation (DataCollector)
- `Spectre.Console 0.54.0` — CLI-Output

---

## 3. Stack & Toolchain (für stryker-netx)

### 3.1 Sprache und Runtime
- **C# 14** mit `<LangVersion>latest</LangVersion>` (statt strikt `14.0` — robuster gegen SDK-Updates)
- **.NET 10** als Target-Framework für alle Hauptprojekte
- **netstandard2.0** für Stryker.DataCollector (VsTest-Adapter-Constraint)

### 3.2 Build & Tooling
- **Solution-Format:** `.slnx` (XML, neu in VS 17.10+)
- **SDK-Pinning:** `global.json` mit 10.0.x
- **Code-Style:** `.editorconfig` (zentralisiert in Repo-Root)
- **MSBuild-Properties:** `Directory.Build.props` (zentral)
- **NuGet-Versionen:** Central Package Management via `Directory.Packages.props`

### 3.3 Analyzer (im Build aktiv, Big-Bang Sprint 1 — ADR-004)
- **Roslynator.Analyzers v4.15.0**
- **SonarAnalyzer.CSharp v10.20.0**
- **Meziantou.Analyzer v3.0.22**
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` aktiv
- `.editorconfig`-Tuning für berechtigte Stryker-Patterns (Defensive-Catch, etc.)

### 3.4 Test-Stack (Voll-Migration in Sprint 1 — ADR-005)

**Migration MSTest+Shouldly → xUnit+FluentAssertions:**

| Stryker 4.14.1 | stryker-netx |
|----------------|--------------|
| MSTest + MSTest.TestFramework 4.1.0 | xUnit 2.9.x + xunit.runner.visualstudio 3.1.x |
| Shouldly 4.3.0 | FluentAssertions 8.8.x |
| Moq 4.20.72 | Moq 4.20.72 (beibehalten) |
| Spectre.Console.Testing 0.54.0 | Spectre.Console.Testing (beibehalten) |
| TestableIO.System.IO.Abstractions.TestingHelpers 22.1.1 | beibehalten |
| Microsoft.NET.Test.Sdk 18.4.0 | beibehalten/aktuell |
| (neu) | TngTech.ArchUnitNET.xUnit 0.11.x |
| (neu) | FsCheck.Xunit 3.1.x |
| (neu) | coverlet.collector 8.0.x |
| (neu, separates Projekt) | BenchmarkDotNet 0.14.x |

### 3.5 MCP-Tooling (Pflicht laut CLAUDE.md)

| Tool | Zweck | Pflicht-Trigger |
|------|-------|-----------------|
| **Serena** | Symbolbasierte Code-Analyse via Roslyn (OmniSharp/C# LSP) | IMMER vor Grep für Klassen/Methoden/Properties |
| **Semgrep** | Security-Scanning | Vor jedem Sprint-Abschluss + bei security-relevantem Code |
| **Context7** | Aktuelle API-Doku (NuGet, .NET-APIs) | **PFLICHT vor Buildalyzer-9-Migration**, vor Roslyn-Updates, vor jedem Major-Update |
| **Sequential Thinking (Maxential)** | Branching-Reasoning | ≥10 Schritte bei Architektur, ≥8 bei Algorithmen, ≥3 bei Trade-offs |
| **Tree of Thoughts (NextGen ToT)** | Multi-Option-Exploration | Bei mehreren validen Lösungen (User-Vorgabe) |
| **GitHub CLI (`gh`)** | Mehrstufige Git-Workflows | Branch+PR+Push, Tag+Release, Issue-Erstellung |

> **NICHT MEHR:** FS MCP Server — wurde aus Setup entfernt (war für Projekt-Root falsch konfiguriert).

### 3.6 Permissions
- **`.claude/settings.json` läuft im `bypassPermissions`-Modus** — keine deny/allow-Liste, alle Tool-Calls erlaubt
- Filesystem-Direktiven (Built-In Tools, Serena vor Grep) sind **reine Konvention**, kein Harness-Enforcement
- Subagenten erben dieselben Permissions; CLAUDE.md-Direktiven MÜSSEN explizit im Subagent-Prompt mitgegeben werden (siehe Subagent-Prompt-Schablone in ADR-011)

---

## 4. Architektur-Vision (für stryker-netx)

> Vollständig dokumentiert in [_docs/architecture spec/architecture_specification.md](_docs/architecture%20spec/architecture_specification.md) — 12 ADRs.

### 4.1 Layering (ADR-012)

```
Layer 4 (Composition Root)
  └── Stryker.CLI                            net10.0, OutputType=Exe

Layer 3 (Core Orchestration)
  └── Stryker.Core                            net10.0

Layer 2 (Test-Runner-Adapter)
  ├── Stryker.TestRunner.MicrosoftTestPlatform  net10.0
  └── Stryker.TestRunner.VsTest                 net10.0

Layer 1 (Domain)
  ├── Stryker.Configuration                  net10.0
  ├── Stryker.RegexMutators                  net10.0
  ├── Stryker.Solutions                      net10.0
  └── Stryker.TestRunner                     net10.0

Layer 0 (Foundations)
  ├── Stryker.Abstractions                   net10.0
  ├── Stryker.Utilities                      net10.0
  └── Stryker.DataCollector                  netstandard2.0  ← HARTKODIERT
```

### 4.2 Architektur-Tests (ADR-012)
ArchUnitNET-Tests in `tests/Stryker.Architecture.Tests/`:
- Layer-Trennung enforced
- Mutator-Klassen müssen `sealed` und `IMutator` implementieren
- Reporter-Klassen müssen `sealed` und `IReporter` implementieren
- McMaster-Reference nur in `Stryker.CLI` (Wrapper-Layer-Enforcement)
- Statische `Architecture`-Instanz pro Testklasse (CLAUDE.md-Hinweis: nicht pro Test, teuer)

### 4.3 Code-Standards (CLAUDE.md)
- `sealed` Default für nicht-vererbbare Klassen
- XML-Doc-Kommentare auf allen `public` APIs
- `ConfigureAwait(false)` auf allen `await` Calls in Library-Code
- `catch (Exception ex) when (ex is not OperationCanceledException) { ... }` Pattern
- Namespace folgt Verzeichnisstruktur

---

## 5. Sprint-1-Roadmap (PILOT + DAG-LAYER-PARALLEL — ADR-011)

ToT-Best-Path-Strategie. Realdauer-Schätzung: **4–6 Wochen**.

| Phase | Dauer | Modul-Abdeckung | Subagent-Setup |
|-------|-------|-----------------|----------------|
| **0 — Repo-Bootstrap** | ~½ Tag | Hauptsession seriell | global.json, .editorconfig, .slnx, Directory.{Build,Packages}.props, License-Stack |
| **1 — PILOT Stryker.Abstractions** | ~1–2 Tage | Hauptsession seriell | TWAE + 3 Analyzer + Cleanup, Lessons-Doku |
| **2 — Layer 0 parallel** | ~3–5 Tage | 2 Subagents (Worktree-Isolation) | Stryker.Utilities, Stryker.DataCollector |
| **3 — Layer 1 parallel** | ~5–7 Tage | 4 Subagents (Worktree-Isolation) | Configuration, RegexMutators, Solutions, TestRunner |
| **4 — Layer 2 parallel** | ~3–5 Tage | 2 Subagents (Worktree-Isolation) | TestRunner.MTP, TestRunner.VsTest |
| **5 — Stryker.Core dediziert** | ~5–7 Tage | Hauptsession (oder 1 Subagent) | **Buildalyzer 9 + MsBuildHelper-Fix** + Stryker.Core.UnitTest |
| **6 — Stryker.CLI + Identitäts-Migration** | ~2–3 Tage | Hauptsession | `dotnet stryker-netx`, `dotnet-stryker-netx` Package-IDs, IStrykerCommandLine-Wrapper |
| **7 — Integration & DoD** | ~2–3 Tage | Hauptsession | ArchUnit-Tests, FsCheck, BenchmarkDotNet, ExampleProjects-Smoke-Test |

### Subagent-Prompt-Schablone (Pflicht — siehe ADR-011)

Jeder Subagent-Prompt MUSS die 5 Sektionen aus CLAUDE.md enthalten: KONTEXT, ZIEL, CONSTRAINTS, MCP-ANWEISUNGEN, OUTPUT. Plus Worktree-Isolation und MaxTurns gemäß Aufgabentyp (40–50 für komplexe Implementierung).

### Sprint-1-DoD (Phase 7 Abschluss)

- [ ] `dotnet build` 0 Warnings, 0 Errors
- [ ] `dotnet test` alle grün (Unit + ArchUnit + FsCheck-Properties)
- [ ] `semgrep scan --config auto` ohne neue Findings
- [ ] Mindestens 1 ExampleProject aus `_reference/.../ExampleProjects/` erfolgreich gemutet
- [ ] CLI-Smoke-Test: `dotnet stryker-netx --version` und `--help` funktionieren
- [ ] BenchmarkDotNet-Setup für mindestens 3 Hot Paths
- [ ] Conventional Commits durchgängig
- [ ] Sprint-Tag `v1.0.0-preview.1` gesetzt (optional)

---

## 6. Risiken (Top-12, vollständig in Architecture Spec)

| # | Risiko | Impact | Mitigation |
|---|--------|--------|------------|
| R1 | Buildalyzer-9-API-Migration kann unerwartete Refactors erzwingen | High | Context7 vor Update; Phase 5 dediziert |
| R2 | TWAE + 3 Analyzer können 1500+ Initial-Issues produzieren | High | .editorconfig-Tuning; Pilot-Lessons; Subagent-Parallelisierung |
| R3 | MSTest-Edge-Cases ([ClassInitialize], [ExpectedException]) | Medium | Roslyn-Code-Mod als Tooling |
| R4 | Roslyn-API-Updates können Breaking Changes haben | Medium | Context7-Pflicht |
| R5 | DataCollector netstandard2.0-Pinning blockiert moderne BCL | Low | Bewusste Inkaufnahme |
| R6 | McMaster-Deprecation kann CVE bringen | Medium | ADR-007 HYBRID + Migration-Trigger |
| R7 | Spätere AOT-Aktivierung erfordert Refactor | Low | ADR-006 (tauglich aber nicht erzwungen) |
| R8 | ExampleProjects als Smoke-Tests können brechen | Medium | Phase-7-Verifikation |
| R9 | Stryker-Upstream-4.15.0 könnte Eigenarbeit obsolet machen | Low | Apache-2.0 erlaubt Re-Sync |
| R10 | `.slnx` Tooling-Support unklar bei manchen .NET 10 SDK | Low | Phase-0-Smoke-Test, .sln-Fallback |
| R11 | Sprint-1 4–6 Wochen übersteigt Standard-Sprint | Medium | Bewusste Mega-Sprint-Entscheidung |
| R12 | Worktree-Subagent-Konflikte beim Merge | Medium | Hauptsession-Koordination, Konflikt-Resolution-Plan |

---

## 7. Tooling-Setup-Status

| Komponente | Status | Notiz |
|------------|--------|-------|
| .NET 10 SDK | ⚠ Phase 0 verifizieren | `dotnet --version`-Check |
| Git for Windows | ✓ Konfiguriert | sslBackend=openssl, credential.helper=store |
| GitHub CLI (`gh`) | ✓ Auth als pgm1980 | volle Scopes inkl. admin, repo, workflow |
| Repo `pgm1980/stryker-netx` | ✓ Erstellt (privat), Sprint 0 gepusht | Default branch `main` |
| Serena MCP | ✓ Verfügbar | Roslyn/OmniSharp-basiert |
| Semgrep CLI | ⚠ Phase 0 verifizieren | `semgrep --version`-Check |
| Context7 MCP | ✓ Verfügbar | `mcp__context7__*` |
| Sequential Thinking (Maxential) | ✓ Verfügbar | `mcp__maxential-cot-mcp-server__*` (CLAUDE.md-Naming `mcp__sequential-thinking-maxential__*` ist Doku-Inkonsistenz) |
| NextGen ToT MCP | ✓ Verfügbar | `mcp__nextgen-tot-mcp-server__*` |
| FS MCP Server | ✗ Entfernt | War für Projekt-Root falsch konfiguriert |
| Hooks (`.claude/hooks/`) | ✓ Aktiv | sprint-health, sprint-gate, statusline, post-compact-reminder, sprint-housekeeping-reminder, sprint-state-save, verify-after-agent |

---

## 8. Externe Referenzen

- **Stryker-Mutator Hauptseite**: https://stryker-mutator.io/
- **Stryker.NET Docs**: https://stryker-mutator.io/docs/stryker-net/
- **Stryker.NET GitHub**: https://github.com/stryker-mutator/stryker-net
- **Stryker.NET 4.14.1 Source (lokal)**: [_reference/stryker-4.14.1/](_reference/stryker-4.14.1/)
- **mutation-testing-elements** (HTML-Report-Frontend): https://github.com/stryker-mutator/mutation-testing-elements
- **C# 14**: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14
- **.NET 10**: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- **Buildalyzer**: https://github.com/Buildalyzer/Buildalyzer
- **Apache 2.0**: http://www.apache.org/licenses/LICENSE-2.0
- **Developer Certificate of Origin**: https://developercertificate.org/
- **Contributor Covenant 2.1**: https://www.contributor-covenant.org/version/2/1/code_of_conduct/

---

## 9. Konventionen-Glossar

| Begriff | Bedeutung |
|---------|-----------|
| **PFLICHT / VERBOTEN** | Nicht-verhandelbare CLAUDE.md-Direktive |
| **Sprint 0** | Architektur- und Design-Sprint, kein Code (✅ abgeschlossen 2026-04-30) |
| **Sprint 1** | Mega-Sprint Implementation (geplant, 4–6 Wochen) |
| **Sprints 2..N** | Refinement-Sprints |
| **ADR** | Architecture Decision Record |
| **FR / NFR** | Functional / Non-Functional Requirement |
| **DoD** | Definition of Done |
| **DCO** | Developer Certificate of Origin |
| **TWAE** | TreatWarningsAsErrors |
| **CPM** | Central Package Management (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) |
| **TFM** | Target Framework Moniker (`net10.0`, `netstandard2.0`, …) |
| **MTP** | Microsoft Testing Platform (neue Generation) |
| **VsTest** | Visual Studio Test (klassische Generation) |
| **ToT** | Tree of Thoughts (Reasoning-Methode) |

---

## 10. Stryker-spezifisches Glossar

| Begriff | Bedeutung |
|---------|-----------|
| **Mutator** | Klasse, die eine bestimmte Code-Mutation produziert (z.B. EqualityMutator) |
| **Mutant** | Konkrete Mutation einer Quelldatei an einer bestimmten Stelle |
| **Killed Mutant** | Tests haben die Mutation entdeckt (gut) |
| **Survived Mutant** | Tests haben die Mutation NICHT entdeckt (Lücke) |
| **Timeout Mutant** | Mutation führte zu Endlosschleife / Hang (zählt als killed) |
| **No-Coverage Mutant** | Mutation in Code, der nicht durch Tests abgedeckt ist |
| **Mutation Score** | (killed + timeout) / (total − no-coverage) |
| **Reporter** | Output-Format (Console, HTML, JSON, Dashboard, Baseline, Markdown, ClearText) |
| **Test Runner** | Adapter zu Test-Framework (VsTest, MicrosoftTestPlatform/MTP) |
| **DataCollector** | VsTest-Komponente die während Test-Runs Coverage sammelt |
| **Baseline** | Referenz-Mutation-Run für differential-Vergleich |

---

## 11. Update-Log

| Datum | Änderung |
|-------|----------|
| 2026-04-29 | Initial-Erstellung als Sprint-0-Baseline (Bootstrap, FS-MCP-Entfernung, Git-Setup, Repo-Init) |
| 2026-04-30 | Sprint 0 abgeschlossen: 12 ADRs, FRs/NFRs, License-Stack, README; korrekte Stryker-4.14.1-Erkenntnisse (bereits net8.0, alle Master-PRs drin, Buildalyzer 8.0 ist eigentlicher Bug) |
