# Sprint 2.1 — Code-Excellence Audit

**Datum:** 2026-04-30
**Sprint:** 2 (Code Excellence)
**Branch:** `feature/2-code-excellence`
**Base-Tag:** `v1.0.0-preview.1`
**Methode:** systematische Grep/Glob-Inventur über `src/` (Code-Symbole via Serena in den nachfolgenden Sub-Phasen, hier nur Counts)

## Ziel

Quantifizieren der Modernisierungs-Surface für die 8 nachfolgenden Sub-Phasen, damit Aufwand und Impact pro Phase realistisch eingeschätzt werden können.

## Befund-Tabelle

| # | Sub-Phase | Feature | Kandidaten | Files | Bemerkung |
|---|-----------|---------|------------|-------|-----------|
| A | 2.2 | `[GeneratedRegex]` Source Generator | **6** | 4 | alle `private static readonly Regex` mit konstantem Pattern — Source-Generator-tauglich |
| B | 2.3 | Extension Members C# 14 | **26 + 3** | 2 | `IProjectAnalysisExtensions` (26) + `IProjectAnalysisCSharpExtensions` (3) — primäre Refactoring-Targets |
| C | 2.4 | `CallerArgumentExpression` / `ArgumentNullException.ThrowIfNull` | **37** | 12 | `throw new ArgumentNullException(nameof(x))` — direkt durch `ArgumentNullException.ThrowIfNull(x)` (.NET 6+) ersetzbar |
| D | 2.5 | Raw String Literals `"""` (verbatim → raw) | **29** | 23 | `@"..."`-Sites — Audit pro Site, nicht pauschal konvertieren (manche sind Pfade ohne Mehrwert) |
| D' | 2.5 | bereits raw string literals | (9) | 9 | 8× `Stryker.RegexMutators` + 1× `SourceProjectNameInput` — Baseline aus Sprint 1 |
| E | 2.6 | `record struct` Audit | **15 sealed records** / **0 record struct** | 15 | Hauptkandidaten: `Stryker.TestRunner.MicrosoftTestPlatform.Models.*` (DTOs, Roundtrip-Heavy) |
| E' | 2.6 | `field` Keyword (C# 14) | **9 backing-field Patterns** | 8 | enge Kandidaten — viele bestehende Backing-Fields haben tatsächlich Logik (Validation/Side-Effects), nur einige sind Trivial-Ersatz |
| F | 2.7 | List Patterns `is [...]` | **0** | 0 | Greenfield-Opportunities (Sammlungs-Match-Sites, z. B. in Mutator-Pipelines, prüfen) |
| F' | 2.7 | Type Aliases `using X = Y;` | **6** | 4 | bereits selektiv genutzt (Roslyn vs. MSBuild Project, LibGit2 vs. MSLogger) — gezielt prüfen ob noch mehr Aliase Klarheit bringen |
| G | 2.8 | `JsonSerializerContext` Source Generator | **16 Calls** | 6 | `Stryker.Core/Reporters/Json/*` (14) + `Stryker.CLI/FileConfig*` (2) — komplette JSON-Pipeline AOT-tauglich machen |
| H | 2.7 | params Span/IList | **0** | 0 | Optional — primäre Hot-Paths sind bereits Collection-Expression-basiert (Sprint 1 Phase 10.3) |

## Prioritäts-Empfehlung (Aufwand × Impact)

| # | Sub-Phase | Aufwand | Impact | Priorität |
|---|-----------|---------|--------|-----------|
| 2.2 | `[GeneratedRegex]` (6 sites) | LOW (~30min) | HIGH (Performance + Trim-Safety + AOT-Ready) | **A1** |
| 2.4 | `ArgumentNullException.ThrowIfNull` (37 sites) | LOW (~45min) | MEDIUM (Boilerplate-Reduktion + bessere Stack Traces) | **A1** |
| 2.3 | Extension Members C# 14 (26 + 3) | MEDIUM (~90min) | HIGH (zukunftsfähige Syntax + IntelliSense-Konsistenz) | **A2** |
| 2.8 | `JsonSerializerContext` (16 Calls) | MEDIUM (~120min) | HIGH (AOT/Trim-Safety + Reporter-Performance) | **A2** |
| 2.5 | Raw String Literals (29 sites) | MEDIUM (~60min) | LOW–MEDIUM (Lesbarkeit, vor allem bei JSON-Snippets in Inputs) | **B** |
| 2.6 | `record struct` + `field` Keyword | LOW–MEDIUM (~60min) | MEDIUM (TestRunner-DTOs profitieren von record struct) | **B** |
| 2.7 | List Patterns + Type Aliases + Property Patterns | MEDIUM (~60min) | LOW–MEDIUM (selektiv, kein flächendeckender Refactor) | **B** |

## Out-of-Scope für Sprint 2

- **`params ReadOnlySpan<T>`** — derzeit kein klarer Hot-Path-Bedarf, .NET 9-Optimierung greift selten
- **C# 14 `extension` für instance properties** — noch in Vorschau, riskant für Production-Code-Pfad
- **UTF-8 String Literals** — keine Konsumenten in Reportern aktuell
- **`CollectionsMarshal.AsSpan()`** in Hot-Paths — Sprint-3-Kandidat, separates Performance-Sprint

## Risiken

- **2.3 Extension Members C# 14** — neue Syntax (`extension(IProjectAnalysis pa) { ... }`) ist ein Sprachfeature in C# 14 stable. Roslyn-Support in SDK 10.0.100 vorhanden, aber Roslynator/Sonar-Analyzers können noch keine Fixes anbieten. **Mitigation:** vor Refactor manueller Trockenlauf in einem Branch-Spike + `dotnet build` mit voller Analyzer-Cascade.
- **2.8 JsonSerializerContext** — Custom JsonConverter (PositionConverter, LocationConverter, JsonMutantConverter, etc.) müssen kompatibel bleiben. **Mitigation:** Roundtrip-Tests via FsCheck.Property erweitern, dann erst Source-Generator aktivieren.
- **2.5 Raw String Literals** — manche `@"..."`-Sites sind Windows-Pfade die KEIN raw literal brauchen — pauschale Konvertierung würde Diff-Lärm erzeugen. **Mitigation:** nur Sites mit ≥2 Backslashes oder eingebetteten Quotes konvertieren.

## Nächster Schritt

Sprint 2.2 — **`[GeneratedRegex]` Source Generators** (6 Kandidaten in 4 Files) als erster Quick-Win, dann 2.4 (`ArgumentNullException.ThrowIfNull`) als zweiter Quick-Win, dann die größeren Refactors 2.3 / 2.8.

## Roh-Counts (Beleg)

```
[GeneratedRegex] Kandidaten (private static readonly Regex):
- src/Stryker.Configuration/ExclusionPattern.cs:14, :15
- src/Stryker.Core/Initialisation/ProjectComponentsBuilder.cs:61
- src/Stryker.Core/MutantFilters/ExcludeFromCodeCoverageFilter.cs:15
- src/Stryker.Core/Mutants/CsharpNodeOrchestrators/CommentParser.cs:17, :18

Extension Members C# 14 — IProjectAnalysisExtensions (26 public static this):
- GetAssemblyFileName, BuildsAnAssembly, GetReferenceAssemblyPath, GetAssemblyDirectoryPath,
  GetAssemblyPath, GetAssemblyName, GetResources, AssemblyAttributeFileName,
  GetSymbolFileName, TargetPlatform, MsBuildPath, GetSourceGenerators,
  LoadReferences, GetNuGetFramework, TargetsFullFramework, GetLanguage,
  IsValid, IsValidFor, GetOutputKind, GetCompilerApiVersion,
  IsSignedAssembly, IsDelayedSignedAssembly, GetAssemblyOriginatorKeyFile,
  (analyzers method line 346), GetWarningLevel, GetPropertyOrDefault
+ private GetRootNamespace (line 392)

Extension Members C# 14 — IProjectAnalysisCSharpExtensions (3 public static + 1 private):
- GetPreprocessorSymbols, GetCompilationOptions, GetParseOptions,
+ private GetNullableContextOptions

ArgumentNullException(nameof()) Sites (37 across 12 files):
- StrykerCli.cs (6), MutationTestProcess.cs (4), StrykerRunner.cs (3),
  InitialisationProcess.cs (4), InputFileResolver.cs (4), ProjectOrchestrator.cs (6),
  InitialBuildProcess.cs (3), MutationTestExecutor.cs (1), CoverageAnalyser.cs (1),
  ProjectMutator.cs (2), NugetRestoreProcess.cs (2), InitialTestProcess.cs (1)

JsonSerializer Sites (16 across 6 files):
- Stryker.Core/Reporters/Json/JsonReportSerialization.cs (4)
- Stryker.Core/Reporters/Json/{TestFiles,SourceFiles}/*Converter.cs (10)
- Stryker.Core/Reporters/Html/RealTime/Events/SseEvent.cs (1)
- Stryker.CLI/FileConfigReader.cs (1) + FileConfigGenerator.cs (1)
```
