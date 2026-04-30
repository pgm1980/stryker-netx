# Software Design Specification — stryker-netx

**Version:** 0.1.0
**Datum:** 2026-04-30
**Status:** Approved (Sprint-0-Output)
**Referenz:** [Architecture Specification](../architecture%20spec/architecture_specification.md)

---

## 1. Functional Requirements (FRs)

> Format: **MUSS** = verpflichtend, **SOLL** = empfohlen, **KANN** = optional. Priorität: Must / Should / Could.
> Alle FRs sind 1:1 zu Stryker.NET 4.14.1 — Verhaltens-Identität mit Upstream ist erste Priorität (siehe ADR-001, ADR-003).

### FR-01: CLI Operations

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-01.1 | Das System MUSS als globales `dotnet tool` mit Command-Name `dotnet stryker-netx` installierbar sein | Must | 1.0.0-preview.1 |
| FR-01.2 | Das System MUSS alle CLI-Flags von Stryker.NET 4.14.1 1:1 unterstützen (z.B. `--project`, `--solution`, `--configuration`, `--reporter`, `--threshold-high`, `--threshold-low`, `--threshold-break`, `--mutate`, `--ignore-mutations`, `--mutation-level`, `--with-baseline`, `--diff-ignore-changes`, etc.) | Must | 1.0.0-preview.1 |
| FR-01.3 | Das System MUSS `--help` mit identischer Hilfe-Struktur zu Upstream ausgeben (mindestens Funktional-Identisch) | Must | 1.0.0-preview.1 |
| FR-01.4 | Das System MUSS `--version` mit der eigenen Version (z.B. `1.0.0-preview.1`) ausgeben | Must | 1.0.0-preview.1 |
| FR-01.5 | Das System SOLL einen Exit-Code != 0 zurückgeben wenn Mutation-Score-Thresholds verletzt werden | Should | 1.0.0-preview.1 |
| FR-01.6 | Das System SOLL Strg+C / SIGTERM behandeln (Graceful Shutdown, partielle Ergebnisse retten) | Should | 1.0.0 |

### FR-02: Configuration Management

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-02.1 | Das System MUSS Konfigurationsdateien `stryker-config.json`, `stryker-config.yaml`, `stryker-config.yml` im Projekt-Root automatisch laden | Must | 1.0.0-preview.1 |
| FR-02.2 | Das System MUSS Custom-Config-Files via `--config <path>` akzeptieren | Must | 1.0.0-preview.1 |
| FR-02.3 | Das System MUSS Config-Schema 1:1 zu Upstream Stryker.NET 4.14.1 unterstützen | Must | 1.0.0-preview.1 |
| FR-02.4 | Das System MUSS CLI-Args über Config-File-Werte priorisieren (CLI > Config > Defaults) | Must | 1.0.0-preview.1 |
| FR-02.5 | Das System MUSS Config-Validierungs-Fehler beim Start mit klarem Error-Message zurückgeben (Fail-Fast) | Must | 1.0.0-preview.1 |
| FR-02.6 | Das System SOLL Config-File-Migration von älteren Stryker.NET-Versionen NICHT explizit unterstützen (1:1 zu 4.14.1 ist genug) | Could | — |

### FR-03: Solution & Project Loading

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-03.1 | Das System MUSS .NET-10-Test-Projekte mit `<TargetFramework>net10.0</TargetFramework>` korrekt laden und parsen | Must | 1.0.0-preview.1 |
| FR-03.2 | Das System MUSS .NET-9-Test-Projekte unterstützen | Must | 1.0.0-preview.1 |
| FR-03.3 | Das System MUSS .NET-8-Test-Projekte unterstützen (Backward-Compat zu Stryker 4.14.1) | Must | 1.0.0-preview.1 |
| FR-03.4 | Das System MUSS sowohl `.sln` als auch `.slnx` Solution-Formate verarbeiten | Must | 1.0.0-preview.1 |
| FR-03.5 | Das System MUSS C# 14 Source-Code parsen können (z.B. Primary Constructors, Collection Expressions, `field`-Keyword) | Must | 1.0.0-preview.1 |
| FR-03.6 | Das System MUSS C# Interceptors propagieren (`InterceptorsNamespaces` MSBuild-Property) — bereits in 4.14.1 PR #3471 enthalten | Must | 1.0.0-preview.1 |
| FR-03.7 | Das System MUSS auf reinen .NET-10-SDK-Maschinen ohne Visual Studio installation lauffähig sein (siehe ADR-010) | Must | 1.0.0-preview.1 |

### FR-04: Mutation Engine

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-04.1 | Das System MUSS alle Mutator-Klassen aus Stryker.NET 4.14.1 1:1 unterstützen (Statement-Level, Expression-Level, Regex-Level — siehe https://stryker-mutator.io/docs/stryker-net/mutations) | Must | 1.0.0-preview.1 |
| FR-04.2 | Das System MUSS Mutator-Verhalten deterministisch reproduzieren (gleiche Eingabe → gleiche Mutationen) | Must | 1.0.0-preview.1 |
| FR-04.3 | Das System MUSS Mutationen via `--mutate` und `--ignore-mutations` selektierbar machen | Must | 1.0.0-preview.1 |
| FR-04.4 | Das System MUSS Mutation-Level (Basic, Standard, Advanced, Complete) wie Upstream unterstützen | Must | 1.0.0-preview.1 |
| FR-04.5 | Das System MUSS Source-Markup-Comments (`// Stryker disable once <Mutator>`) respektieren | Must | 1.0.0-preview.1 |
| FR-04.6 | Das System SOLL spezialisierte Regex-Mutationen via Stryker.Regex.Parser unterstützen | Should | 1.0.0-preview.1 |

### FR-05: Test Execution

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-05.1 | Das System MUSS Tests via VsTest-Adapter ausführen (Stryker.TestRunner.VsTest) | Must | 1.0.0-preview.1 |
| FR-05.2 | Das System MUSS Tests via Microsoft Testing Platform ausführen (Stryker.TestRunner.MicrosoftTestPlatform) | Must | 1.0.0-preview.1 |
| FR-05.3 | Das System MUSS Test-Discovery für xUnit, NUnit, MSTest unterstützen (Standard-Adapter über VsTest/MTP) | Must | 1.0.0-preview.1 |
| FR-05.4 | Das System MUSS Test-Timeout konfigurierbar machen | Must | 1.0.0-preview.1 |
| FR-05.5 | Das System MUSS Test-Filter (`--test-projects`, `--test-case-filter`) wie Upstream unterstützen | Must | 1.0.0-preview.1 |
| FR-05.6 | Das System MUSS DataCollector für Coverage-Tracking während VsTest-Runs nutzen (Stryker.DataCollector, netstandard2.0) | Must | 1.0.0-preview.1 |

### FR-06: Reporting

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-06.1 | Das System MUSS HTML-Reporter unterstützen, kompatibel mit `mutation-testing-elements` Schema | Must | 1.0.0-preview.1 |
| FR-06.2 | Das System MUSS JSON-Reporter unterstützen, Schema 1:1 zu Upstream | Must | 1.0.0-preview.1 |
| FR-06.3 | Das System MUSS Console/Progress-Reporter unterstützen (Spectre.Console-basiert) | Must | 1.0.0-preview.1 |
| FR-06.4 | Das System MUSS Dashboard-Reporter unterstützen (Upload zu mutation-testing.io API) | Must | 1.0.0-preview.1 |
| FR-06.5 | Das System MUSS Baseline-Reporter unterstützen (Markdown-Output für PR-Comments) | Must | 1.0.0-preview.1 |
| FR-06.6 | Das System MUSS Markdown-Reporter unterstützen (via Grynwald.MarkdownGenerator) | Must | 1.0.0-preview.1 |
| FR-06.7 | Das System MUSS ClearText-Reporter unterstützen | Must | 1.0.0-preview.1 |
| FR-06.8 | Das System MUSS mehrere Reporter parallel ausführen können (`--reporter html --reporter json --reporter console`) | Must | 1.0.0-preview.1 |

### FR-07: Diff & Incremental Mutation

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-07.1 | Das System MUSS Git-Diff-basierte Incremental-Mutation unterstützen (`--since`, `--with-baseline`) — via LibGit2Sharp | Must | 1.0.0-preview.1 |
| FR-07.2 | Das System MUSS Baseline-Storage in S3 (AWSSDK.S3) und Azure Files (Azure.Storage.Files.Shares) unterstützen | Must | 1.0.0-preview.1 |
| FR-07.3 | Das System MUSS Local-Filesystem-Baseline unterstützen | Must | 1.0.0-preview.1 |

### FR-08: Library API

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-08.1 | Das System MUSS `Stryker.Core` als NuGet-Library (`stryker-netx`) zur programmatischen Nutzung publizieren | Must | 1.0.0-preview.1 |
| FR-08.2 | Das System MUSS öffentliche API-Surface von `Stryker.Core` und `Stryker.Abstractions` 1:1 zu Upstream halten | Must | 1.0.0-preview.1 |
| FR-08.3 | Das System SOLL Konsumenten erlauben eigene Reporter zu registrieren (Stryker.NET-IReporter-Interface) | Should | 1.0.0-preview.1 |
| FR-08.4 | Das System SOLL Konsumenten erlauben eigene Mutator-Klassen zu registrieren (IMutator-Interface) | Should | 1.0.0-preview.1 |

### FR-09: Logging & Diagnostics

| ID | Anforderung | Priorität | Release |
|----|-------------|-----------|---------|
| FR-09.1 | Das System MUSS strukturiertes Logging via Serilog ausgeben | Must | 1.0.0-preview.1 |
| FR-09.2 | Das System MUSS Log-Level via `--log-level` konfigurierbar machen (ERROR/WARN/INFO/DEBUG) | Must | 1.0.0-preview.1 |
| FR-09.3 | Das System MUSS File-Logging via `--log-to-file` aktivierbar machen | Must | 1.0.0-preview.1 |
| FR-09.4 | Das System MUSS Mutation-Run mit eindeutiger Korrelations-ID loggen | Must | 1.0.0-preview.1 |

---

## 2. Non-Functional Requirements (NFRs)

### NFR-01: Security

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-01.1 | Alle CLI-Args MÜSSEN validiert werden | 100% Input-Validierung; Unit Tests |
| NFR-01.2 | Config-File-Schema MUSS validiert werden (Fail-Fast beim Start) | Schema-Validierung deckt alle Pflichtfelder + Typen ab |
| NFR-01.3 | Keine bekannten Vulnerabilities in Dependencies | `dotnet list package --vulnerable` 0 Findings |
| NFR-01.4 | Semgrep-Scan MUSS vor jedem Sprint-Abschluss bestehen | 0 offene Security-Findings |
| NFR-01.5 | Keine Secrets im Code, Config oder NuGet-Package | Semgrep `secrets.detected`-Rule grün; `.gitignore` schließt `.env`, `*.pfx`, `*.snk`, `secrets.json` aus |
| NFR-01.6 | Externe HTTP-Calls (Dashboard-Reporter, Baseline-S3/Azure) MÜSSEN Token-basiert authentisieren | Token via Env-Var oder CLI-Arg, nie im Config-File |

### NFR-02: Performance

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-02.1 | Mutation-Generation für eine durchschnittliche Source-Datei (~500 LoC) SOLL < 100 ms dauern | BenchmarkDotNet-verifiziert |
| NFR-02.2 | Roslyn-Parsing für eine durchschnittliche Source-Datei SOLL < 50 ms dauern | BenchmarkDotNet-verifiziert |
| NFR-02.3 | Test-Run-Overhead pro Mutation SOLL gleichbleibend zu Upstream 4.14.1 sein | BenchmarkDotNet vs. Upstream-Baseline |
| NFR-02.4 | HTML-Report-Generation für 1000 Mutationen SOLL < 5 s dauern | BenchmarkDotNet-verifiziert |
| NFR-02.5 | Memory-Footprint im Normalbetrieb (mittlere Solution) SOLL < 1 GB sein | dotnet-counters, Profiler-verifiziert |

### NFR-03: Reliability / Zuverlässigkeit

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-03.1 | Single-Mutation-Failures DÜRFEN den Gesamt-Run NICHT abbrechen | Graceful Skip; Logging |
| NFR-03.2 | Test-Runner-Crashes MÜSSEN mit Retry (max 3x, exponential backoff) behandelt werden | Integration Test |
| NFR-03.3 | Strg+C / SIGTERM MUSS partielle Ergebnisse retten | Manueller Test |
| NFR-03.4 | Externe HTTP-Calls (Dashboard, S3, Azure) MÜSSEN bei Fehlern retryen (3x exponential backoff) | Integration Test mit Mock-Endpoint |
| NFR-03.5 | Unerwartete Exceptions MÜSSEN mit Stack-Trace geloggt werden + Exit-Code != 0 | Unit Test |

### NFR-04: Testability

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-04.1 | Unit Tests MÜSSEN TDD-konform geschrieben werden | Red → Green → Refactor (CLAUDE.md-Pflicht) |
| NFR-04.2 | Code Coverage MUSS gemessen werden | Coverlet, Ziel ≥ 80% |
| NFR-04.3 | Architekturregeln MÜSSEN als ausführbare ArchUnitNET-Tests existieren | 0 Architekturverletzungen |
| NFR-04.4 | Property-Based Tests MÜSSEN für Roundtrips/Invarianten existieren | FsCheck, mindestens 1 Property je Mutator-Familie |
| NFR-04.5 | Performance-Benchmarks MÜSSEN für Hot Paths existieren | BenchmarkDotNet, Release-Mode |
| NFR-04.6 | Smoke-Tests MÜSSEN gegen `_reference/.../ExampleProjects` laufen | Integration-Test in Phase 7 |
| NFR-04.7 | Mutation-Score-Selbsttest: stryker-netx mutiert sich selbst | Bonus-Goal Sprint 3+ |

### NFR-05: Maintainability

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-05.1 | Code MUSS Roslynator-, SonarAnalyzer.CSharp- und Meziantou-Analyzer-Prüfungen bestehen | 0 Findings (TWAE) |
| NFR-05.2 | Zirkuläre Abhängigkeiten DÜRFEN NICHT existieren | ArchUnit-Test-verifiziert |
| NFR-05.3 | Public APIs MÜSSEN XML-Doc-Kommentare haben | 100% Coverage; Roslynator RCS1140 |
| NFR-05.4 | Kein `#pragma warning disable` ohne dokumentierte Begründung im Kommentar | Code-Review-Pflicht; CLAUDE.md |
| NFR-05.5 | sealed default für nicht-vererbbare Klassen | ArchUnit-Test (Mutator/Reporter); Code-Review |
| NFR-05.6 | `ConfigureAwait(false)` auf allen async Calls in Library-Code | Roslynator + Meziantou-Regel |
| NFR-05.7 | `catch (Exception ex) when (ex is not OperationCanceledException)` Pattern an System-Boundaries | Code-Review; CLAUDE.md |
| NFR-05.8 | Module/Klassen SOLLEN < 500 Zeilen sein (Stryker hat einige große Klassen — Ausnahmen dokumentiert) | Sonar-Regel S138 |
| NFR-05.9 | Funktionen/Methoden SOLLEN < 50 Zeilen sein | Sonar-Regel S107 / S138 |

### NFR-06: Compatibility

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-06.1 | Das Tool MUSS auf Windows lauffähig sein | CI-verifiziert (Win Runner) |
| NFR-06.2 | Das Tool MUSS auf Linux lauffähig sein | CI-verifiziert (Linux Runner) |
| NFR-06.3 | Das Tool SOLL auf macOS lauffähig sein | Best-Effort (CI-Linux + manuelle Tests) |
| NFR-06.4 | Das Tool MUSS .NET-10, .NET-9, .NET-8 Test-Projekte mutieren können | Smoke-Tests gegen je ein Projekt pro Version |
| NFR-06.5 | Das Tool MUSS C#-14, C#-13, C#-12 Source-Syntax verstehen | Roslyn-Update (C#-14-fähig) |
| NFR-06.6 | CLI-Flags und Config-Schema MÜSSEN 1:1 mit Stryker.NET 4.14.1 kompatibel sein | Cross-Reference Test mit Upstream-Config-Files |
| NFR-06.7 | HTML-Report MUSS mit `mutation-testing-elements` aktueller Version kompatibel sein | Visual-Diff-Test |

### NFR-07: Configurability

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-07.1 | Alle relevanten Limits MÜSSEN konfigurierbar sein mit sinnvollen Defaults | Default-Werte 1:1 zu Upstream |
| NFR-07.2 | Konfigurationsfehler MÜSSEN beim Start erkannt werden (Fail-Fast) | Schema-Validation; Unit Test |
| NFR-07.3 | Default-Logging-Level SOLL `INFO` sein | Konfigurierbar via `--log-level` |

### NFR-08: Observability

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-08.1 | Strukturiertes Logging mit Korrelations-ID pro Mutation-Run | Serilog-Enricher |
| NFR-08.2 | Phase-Übergänge (Solution-Loading, Mutation, Test-Run, Reporting) MÜSSEN als INFO geloggt werden | Log-Output enthält Phase-Marker |
| NFR-08.3 | Mutation-Statistiken (total, killed, survived, timeout, no-coverage) MÜSSEN am Ende ausgegeben werden | Console-Output + JSON-Reporter |
| NFR-08.4 | Performance-Metriken (Phase-Dauer, Memory-Peak) SOLLEN als DEBUG geloggt werden | dotnet-counters-Integration optional |

### NFR-09: Compatibility-with-Upstream (1:1)

| ID | Anforderung | Metrik |
|----|-------------|--------|
| NFR-09.1 | CLI-Argument-Namespace 1:1 zu Stryker.NET 4.14.1 | Cross-Reference-Test-Suite mit jedem Upstream-CLI-Flag |
| NFR-09.2 | Config-File-Schema 1:1 zu Stryker.NET 4.14.1 | Smoke-Test mit Upstream-stryker-config.json-Beispielen |
| NFR-09.3 | Reporter-Output-Schemas 1:1 (HTML, JSON, Markdown, Console) | Visual-Diff bzw. JSON-Schema-Diff gegen Upstream-Output |
| NFR-09.4 | Mutation-Score-Berechnung deterministisch identisch zu Upstream | Score-Cross-Check mit Upstream-Run auf gleichem ExampleProject |

---

## 3. Interface-Spezifikationen

### 3.1 IStrykerCommandLine (neue Abstraktion, Sprint 1 Phase 6)

**Typ:** C# Interface (intern, in `Stryker.CLI`)
**Verantwortung:** Wrapper-Layer über McMaster.Extensions.CommandLineUtils zur Kapselung der CLI-Library (siehe ADR-007).

```csharp
public interface IStrykerCommandLine
{
    /// <summary>Adds a command (or subcommand) with options and arguments.</summary>
    void AddCommand(string name, string description, Action<IStrykerCommandBuilder> build);

    /// <summary>Parses CLI args and dispatches to the appropriate command handler.</summary>
    /// <returns>Process exit code (0 = success).</returns>
    Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken);
}

public interface IStrykerCommandBuilder
{
    void Option<T>(string template, string description, Action<T> setter);
    void Argument<T>(string name, string description, bool required, Action<T> setter);
    void OnExecute(Func<CancellationToken, Task<int>> handler);
}
```

**Default-Implementierung:** `McMasterStrykerCommandLine` (in `Stryker.CLI/Internal/`).

### 3.2 IMutator (von Upstream übernommen, 1:1)

**Typ:** C# Interface (Stryker.Abstractions)
**Verantwortung:** Definiert eine konkrete Code-Mutation (z.B. EqualityMutator, BooleanMutator, BlockMutator).

```csharp
public interface IMutator
{
    /// <summary>Mutator-Kategorie (Statement, Expression, Regex).</summary>
    MutatorType MutatorType { get; }

    /// <summary>Erzeugt Mutationen für den gegebenen SyntaxNode.</summary>
    IEnumerable<Mutation> Mutate(SyntaxNode node, SemanticModel? semanticModel, StrykerOptions options);
}
```

### 3.3 IReporter (von Upstream übernommen, 1:1)

**Typ:** C# Interface (Stryker.Abstractions)
**Verantwortung:** Empfängt Events zum Mutation-Run-Lifecycle und produziert Output (HTML, JSON, Console, etc.).

```csharp
public interface IReporter
{
    void OnMutantsCreated(IReadOnlyProjectComponent reportComponent);
    void OnStartMutantTestRun(IEnumerable<IReadOnlyMutant> mutantsToBeTested);
    void OnMutantTested(IReadOnlyMutant result);
    void OnAllMutantsTested(IReadOnlyProjectComponent reportComponent, ITestProjectsInfo testProjectsInfo);
}
```

### 3.4 ITestRunner (von Upstream übernommen, 1:1)

**Typ:** C# Interface (Stryker.TestRunner)
**Verantwortung:** Abstrahiert Test-Execution über Test-Framework-Adapter.

```csharp
public interface ITestRunner : IDisposable
{
    /// <summary>Discovers all available tests in the project.</summary>
    TestRunResult InitialTest(IProjectAndTests project);

    /// <summary>Runs tests against a specific mutant; returns coverage and pass/fail.</summary>
    TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator? timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update);
}
```

### 3.5 IStrykerOptions (von Upstream übernommen, 1:1)

**Typ:** C# Interface (Stryker.Abstractions)
**Verantwortung:** Aggregiert die geparste Konfiguration aus CLI-Args + Config-File + Defaults.

> Vollständige Member-Liste in [Stryker.Abstractions/IStrykerOptions.cs](../../_reference/stryker-4.14.1/src/Stryker.Abstractions/IStrykerOptions.cs) — wird 1:1 übernommen.

---

## 4. Datenmodelle

### 4.1 Mutant (zentrale Entität)

| Feld | Typ | Beschreibung | Validierung |
|------|-----|-------------|-------------|
| `Id` | `int` | Eindeutige ID innerhalb eines Mutation-Runs | required, ≥ 0 |
| `OriginalNode` | `SyntaxNode` | Ursprünglicher Roslyn-AST-Knoten | required |
| `Mutation` | `Mutation` | Die konkrete Mutation (Replacement-Node + Display-Name) | required |
| `ResultStatus` | `MutantStatus` | Pending / Killed / Survived / Timeout / NoCoverage / Ignored | required |
| `CoveringTests` | `TestGuidsList?` | Tests die diesen Mutant abdecken | optional |
| `Line` | `int` | Source-Zeile (1-basiert) | ≥ 1 |
| `Column` | `int` | Source-Spalte (1-basiert) | ≥ 1 |
| `MutatorName` | `string` | z.B. "EqualityMutator" | required |

### 4.2 StrykerOptions (Config-Aggregat)

| Feld | Typ | Beschreibung | Validierung |
|------|-----|-------------|-------------|
| `ProjectPath` | `string` | Pfad zum zu mutierenden Projekt | Existing path |
| `SolutionPath` | `string?` | Pfad zur Solution | Existing path |
| `Configuration` | `string` | Build-Configuration | "Release" / "Debug" |
| `Reporters` | `IEnumerable<Reporter>` | Aktivierte Reporter | mindestens 1 |
| `MutationLevel` | `MutationLevel` | Basic / Standard / Advanced / Complete | enum |
| `Thresholds` | `Thresholds` | High / Low / Break | 0–100, High ≥ Low ≥ Break |
| `TestProjects` | `IEnumerable<string>` | Filter für Test-Projekte | optional |
| `WithBaseline` | `bool` | Diff-basierte Mutation aktiv | bool |
| `BaselineProvider` | `BaselineProvider` | LocalFile / S3 / AzureFiles / Disk | enum |
| `LogOptions` | `LogOptions` | Level + File-Logging | required |
| `... (weitere)` | | siehe Stryker.NET-IStrykerOptions | |

> Vollständige Felder 1:1 zu Stryker.NET 4.14.1 — siehe [_reference/.../IStrykerOptions.cs](../../_reference/stryker-4.14.1/src/Stryker.Abstractions/IStrykerOptions.cs).

### 4.3 ProjectComponent (Hierarchie)

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| `RelativePath` | `string` | Relative Pfad-Komponente (z.B. "src/Calculator.cs") |
| `FullPath` | `string` | Absoluter Pfad |
| `Children` | `IList<ProjectComponent>` | Bei Folder-Components |
| `Mutants` | `IEnumerable<Mutant>?` | Bei File-Components |

---

## 5. Datenflüsse

### 5.1 Hauptfluss — `dotnet stryker-netx` Run

```
1. CLI-Entry (Stryker.CLI/Program.cs)
   ↓
2. CommandLine-Parsing (IStrykerCommandLine → McMaster)
   ↓
3. Config-File-Loading (Stryker.Configuration: stryker-config.json/yaml)
   ↓
4. Config-Merge (CLI > File > Defaults) → IStrykerOptions
   ↓
5. Solution/Project-Loading (Stryker.Solutions: Buildalyzer 9.0)
   ↓
6. Roslyn-Parsing aller Source-Files (Stryker.Core)
   ↓
7. Mutator-Discovery + Mutation-Generation (Stryker.Core/Mutants)
   ↓
8. Initial-Test-Run zur Coverage-Sammlung (Stryker.TestRunner.VsTest oder MTP, Stryker.DataCollector)
   ↓
9. Mutation-Test-Loop:
   für jeden Mutant:
     - Inject Mutation in Source-File
     - Test-Run via ITestRunner
     - Result → Mutant.ResultStatus (Killed/Survived/Timeout/NoCoverage)
   ↓
10. Reporter-Pipeline (Stryker.Core/Reporters):
    - HTML, JSON, Console, Dashboard, Baseline (parallel)
   ↓
11. Threshold-Check (FR-01.5):
    - Score ≥ High → Exit 0
    - Score zwischen Low und High → Exit 0 mit Warning
    - Score < Break → Exit 1
   ↓
12. Cleanup & Exit
```

### 5.2 Config-Loading-Fluss

```
1. CLI-Args parsen
2. Wenn --config angegeben: spezifizierte Datei laden, sonst stryker-config.{json,yaml,yml} im Working Dir suchen
3. Config-File parsen (YamlDotNet für YAML, System.Text.Json für JSON)
4. Schema-Validierung gegen IStrykerOptions
5. Merge: CLI-Args überschreiben Config-Werte
6. Defaults für nicht gesetzte Felder
7. Final Validation (z.B. Thresholds-Konsistenz)
8. Bei Fehler: klar lesbare Error-Message + Exit 1 (Fail-Fast)
```

### 5.3 Mutation-Injection-Fluss (pro Mutant)

```
1. Original Source-File lesen (TestableIO.System.IO.Abstractions)
2. Roslyn-SyntaxTree parsen
3. Original-Node durch Mutation-Replacement-Node ersetzen
4. Modified SyntaxTree zurück zu Source-Code (Roslyn-Formatter)
5. Modified Source-File schreiben (atomic mit Backup)
6. dotnet build (mit Buildalyzer-Helper) — schlägt evtl. fehl bei syntaktisch ungültigen Mutationen
7. dotnet test mit Filter auf Covering-Tests — Result → Mutant.ResultStatus
8. Original Source-File restore
9. Loop zu nächstem Mutant
```

### 5.4 Fehlerbehandlung-Fluss

```
1. Exception in beliebiger Phase
   ↓
2. Catch-Boundary (System.IO, Network, Roslyn)
   ↓
3. Log mit Stack-Trace + Korrelations-ID
   ↓
4. Entscheidung:
   - Validierungsfehler → Exit 1 mit klar lesbarer Message
   - Test-Runner-Fehler → Retry (max 3, exponential backoff)
   - Mutation-Inject-Fehler → Skip Mutation, weiter mit nächstem
   - Unerwartete Fehler → Stack-Trace + Exit 1
5. Bei Sprint-Ende: Reporter erhält finale Stats (incl. Skipped-Count)
```

---

## 6. Fehlerbehandlung

### 6.1 Fehler-Kategorien

| Kategorie | Basis-Typ | Exit-Code | Behandlung |
|-----------|-----------|-----------|-----------|
| Validierungsfehler (CLI/Config) | `StrykerInputException` | 1 | Sofort zurückgeben mit klarer Message |
| Solution/Project-Loading-Fehler | `StrykerProjectLoadingException` | 1 | Logging + Exit |
| Roslyn-Parse-Fehler | `StrykerCompilationException` | 1 | Logging + Exit (Code muss compilierbar sein) |
| Test-Runner-Fehler | `StrykerTestRunException` | (retry) | Retry max 3x, dann Exit 1 |
| Mutation-Inject-Fehler | `StrykerMutationException` | (skip) | Logging + Mutant als „Error" markieren, weiter |
| Network-Fehler (Dashboard, S3, Azure) | `StrykerNetworkException` | (retry) | Retry max 3x mit exponential backoff, dann Exit 1 |
| Unerwartete Fehler | `Exception` | 1 | Stack-Trace + Exit |

### 6.2 Exception-Hierarchie

```
Exception
├── StrykerException (base, in Stryker.Abstractions)
    ├── StrykerInputException (CLI/Config-Fehler)
    ├── StrykerProjectLoadingException (Solution/.csproj)
    ├── StrykerCompilationException (Roslyn-Compile)
    ├── StrykerTestRunException (Test-Execution)
    ├── StrykerMutationException (Mutation-Inject)
    └── StrykerNetworkException (Dashboard/S3/Azure)
```

> Hierarchie 1:1 zu Stryker.NET 4.14.1 (zu verifizieren in Phase 1 Pilot).

---

## 7. Sicherheitskonzept

| Maßnahme | Beschreibung | Verifizierung |
|----------|-------------|---------------|
| Input-Validierung | Alle CLI-Args + Config-File-Felder validiert | Unit Tests + Semgrep |
| Schema-Validierung | Config-File gegen Schema beim Start (Fail-Fast) | Schema-File + Unit Tests |
| Secrets Management | Dashboard-API-Key, S3/Azure-Credentials via Env-Vars (`STRYKER_DASHBOARD_API_KEY`, etc.) — niemals in Config-File | Semgrep `secrets.detected` |
| Dependency Audit | `dotnet list package --vulnerable` + Semgrep Supply-Chain | CI-Check pro PR |
| Defense-in-Depth | DataCollector läuft im VsTest-Process-Boundary (sandboxed) | Architektur-Eigenschaft |
| Output-Sanitization | HTML-Reporter escapet User-Input (Source-Code-Snippets) | XSS-Test |
| Network-TLS | Alle externen HTTP-Calls mit TLS 1.2+ | Default in .NET 10 |

---

## 8. Deployment & Operations

### 8.1 Build-Artefakte

| Artefakt | Format | Ziel |
|----------|--------|------|
| `dotnet-stryker-netx` | NuGet Tool Package | NuGet.org (Production) / lokales `./nupkg/` (Test) |
| `stryker-netx` | NuGet Library Package | NuGet.org (Library-Konsumenten) |
| `Stryker.Abstractions`, `Stryker.Core`, `Stryker.Configuration`, etc. | NuGet Library Packages | NuGet.org (transitive) |

### 8.2 Konfigurationsparameter (häufige Defaults)

| Parameter | Default | Beschreibung | Typ |
|-----------|---------|-------------|-----|
| `--reporter` | `["html", "progress", "cleartext"]` | Aktive Reporter | string[] |
| `--mutation-level` | `Standard` | Basic / Standard / Advanced / Complete | enum |
| `--threshold-high` | `80` | Warn-Schwelle Mutation Score | int (0-100) |
| `--threshold-low` | `60` | Untergrenze Mutation Score | int (0-100) |
| `--threshold-break` | `0` | Bricht Build wenn unterschritten | int (0-100) |
| `--log-level` | `INFO` | Log-Level | enum |
| `--concurrency` | `Environment.ProcessorCount / 2` | Parallel-Mutationen | int |
| `--timeout-ms` | `5000 + Initial-Test-Duration * 1.5` | Test-Timeout | int |

> Defaults 1:1 zu Stryker.NET 4.14.1 — vollständige Tabelle in Phase 1 Pilot ergänzen.

### 8.3 CI/CD-Empfehlung (für Konsumenten)

```yaml
# Beispiel GitHub Actions
- name: Install stryker-netx
  run: dotnet tool install -g dotnet-stryker-netx --version 1.0.0-preview.1
- name: Run Mutation Tests
  run: dotnet stryker-netx --reporter dashboard --reporter html
  env:
    STRYKER_DASHBOARD_API_KEY: ${{ secrets.STRYKER_DASHBOARD_API_KEY }}
- name: Upload Report
  uses: actions/upload-artifact@v4
  with:
    name: mutation-report
    path: StrykerOutput/**/mutation-report.html
```

---

## 9. Out-of-Scope für 1.0.0-preview.1

- Stryker.NET-Konfigurations-Migration von älteren Versionen (z.B. 4.13 → 4.14 → 1.0.0-netx)
- NativeAOT-Veröffentlichung (siehe ADR-006, Re-Eval Sprint 4)
- IDE-Integration (VS Extension, Rider Plugin) — out-of-scope, nicht in Stryker 4.14.1
- Visual Basic Source-Mutation — nicht in Stryker 4.14.1 (nur C#)
- Mutation Testing für F# / nicht-C#-Sprachen — out-of-scope
- Custom-Reporter-Plugin-Loading via AssemblyLoadContext — Stryker 4.14.1 hat das eingebettet, wir behalten diese Architektur (1:1)

---

## Änderungshistorie

| Version | Datum | Autor | Änderung |
|---------|-------|-------|----------|
| 0.1.0 | 2026-04-30 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Initiale Sprint-0-Version mit FR-01..09 + NFR-01..09 |
