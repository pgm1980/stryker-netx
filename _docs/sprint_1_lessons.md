# Sprint 1 — Lessons Learned (PILOT-Modul Stryker.Abstractions)

**Phase 1 PILOT** abgeschlossen 2026-04-30. Modul: `Stryker.Abstractions` (61 Files, ~50 public Types/Members). Build grün 0/0, Semgrep 0 findings.

Diese Datei ist die **Pattern-Bibliothek** für Folge-Phasen 2–6 (DAG-Layer-Parallel via Subagents). Jeder Subagent muss diese Datei vor seiner Aufgabe lesen.

---

## 1. Reasoning-Provenance

- **Maxential** (sprint-1-phase-1 session): 6 Schritte (NuGet-Konflikt-Trade-off Schritte 1–3, Cleanup-Strategie Schritt 4, CS1591-Phasing Schritt 5, conclusion Schritt 6) — Tags `nuget-conflict`, `recommendation`, `options-analysis`, `cs1591-massen`, `phasing-strategy`, `sprint-1.5-plan`
- **NextGen ToT**: nicht für Phase 1 — Trade-offs waren ≤3 Optionen je Frage, kein ToT-Fall
- **Context7**: Buildalyzer 9.0 Doku (Workspaces-API)
- **Serena**: 8× `find_symbol(depth=1, include_body=true)` für Symbol-Bodies vor Edits, 9× `replace_symbol_body` für Code-Migrations, 1× `list_dir`
- **GHSA-Lookup via gh api**: System.Security.Cryptography.Xml CVE-Patches ermittelt (10.0.6 ist gepatchte Version)

---

## 2. NuGet-Konflikte (gelöst)

### NU1608 — Buildalyzer 9.0 zieht `Microsoft.CodeAnalysis.VisualBasic 4.0.0` transitive

**Problem**: VB-4.0.0 verlangt `Microsoft.CodeAnalysis.Common (= 4.0.0)`, kollidiert mit unserem zentral gepinnten `Common 5.3.0` (für C# 14 Support).

**Lösung**: Direct `<PackageReference Include="Microsoft.CodeAnalysis.VisualBasic" />` in `Stryker.Abstractions.csproj` (CPM löst auf 5.3.0 → overrides transitive 4.0.0).

**Anwendbar auf**: Alle Module die Buildalyzer (oder transitiv via Solutions/Configuration/Core) referenzieren.

```xml
<!-- Pattern: VisualBasic-Override für Buildalyzer-9-CPM-Konflikt -->
<PackageReference Include="Microsoft.CodeAnalysis.VisualBasic" />
```

### NU1903 — `System.Security.Cryptography.Xml 9.0.0` mit High-Severity CVEs (×2)

**Problem**: Microsoft.* 10.0.x Familie zieht transitive `Crypto.Xml 9.0.0` ein. CVE-2026-33116 + CVE-2026-26171 (DoS-Vulnerabilities) sind in 9.0.0 nicht gepatcht. Pin auf 10.0.0 nicht ausreichend — auch dort ungepatcht.

**Lösung**: GHSA-API-Lookup ermittelte gepatchte Versionen — `10.0.6` (vulnerable: 10.0.0..10.0.5). In Directory.Packages.props pinnen + global PackageReference in Directory.Build.props (Condition für `!= netstandard2.0`).

**Methode für CVE-Mitigation**:
```bash
gh api advisories/<GHSA-ID> --jq '{summary, severity, vulnerabilities: [.vulnerabilities[] | select(.package.name=="<package>") | {vulnerable: .vulnerable_version_range, patched: .first_patched_version}]}'
```

**Anwendbar auf**: Alle Module — Direktive in Directory.Build.props gilt global (außer DataCollector mit netstandard2.0).

---

## 3. Build-Errors mit Roslynator + Sonar + Meziantou + TWAE — Pattern-Bibliothek

**25 errors** im PILOT-Modul (geschätzt linear ~150–500 für Stryker.Core in Phase 5). Klassifiziert in 4 Kategorien:

### Kategorie 1: API-Public — keine Code-Änderung (1:1-Spirit)

| Regel | Symptom | Lösung | Anzahl Phase 1 |
|-------|---------|--------|---------------|
| **CA1720** | "String", "Single", "Guid" als Mutator-Enum-Values / Property-Names | `.editorconfig`-Severity = none, dokumentiert | 3 |
| **CA1716** | "Static", "End", "Case" als Interface-Member-Names | `.editorconfig`-Severity = none, dokumentiert | 3 |

**Begründung**: Stryker-Public-API. Renaming wäre Breaking-Change für Library-Konsumenten.

### Kategorie 2: Code-Quality (Modernization erlaubt) — via Serena

| Regel | Symptom | Pattern-Lösung | Tools | Anzahl Phase 1 |
|-------|---------|----------------|-------|---------------|
| **CS8618** | Non-nullable property nicht initialisiert | `required` Modifier (C# 11+) | `mcp__serena__replace_symbol_body` | 5 |
| **CS8765** | Override-Parameter `object obj` ist nullable im Base | `object?` annotation | `replace_symbol_body` | 2 |
| **MA0006** | `==` für string-Equals | `string.Equals(s1, s2, StringComparison.Ordinal)` | `replace_symbol_body` | 2 |
| **MA0021** | `GetHashCode()` ohne expliziten Comparer | `StringComparer.Ordinal.GetHashCode(s)` | `replace_symbol_body` | 2 |
| **MA0016** | `Dictionary<K,V>`/`List<T>` als Property-Type | `IReadOnlyDictionary<K,V>`/`IReadOnlyCollection<T>` | `replace_symbol_body` | 1 |
| **MA0048** | File enthält 2+ Types | File-Splits via Write neuer Files + Edit alter File | `Write` + `Edit` (kein Symbol-Tool nötig) | 3 |
| **RCS1194** | Exception fehlt Standard-Konstruktoren | `()`, `(string)`, `(string, Exception)` Pattern | `replace_symbol_body` | 4 |

**Wichtige Gotchas**:
- Bei Edits via `replace_symbol_body`: **using-Statements ggf. ergänzen** (z.B. `using System;` für StringComparer/StringComparison wenn `<ImplicitUsings>disable</ImplicitUsings>` aktiv)
- Bei File-Splits: Folge-Files brauchen ihre eigenen `using`-Statements; im verbleibenden Original-File die nicht-mehr-genutzten usings entfernen
- Primary-Constructor-Klassen (z.B. `class Foo(string msg) : Exception(msg);`) müssen für RCS1194 in klassische Class-Form umgebaut werden

### Kategorie 3: XML-Doc-Massen-Problem (CS1591)

**447+ Errors** alleine in Stryker.Abstractions. Skalierung ~5000–8000 über alle 17 Module.

**Entscheidung (siehe ADR-013, Maxential Schritt 5)**: CS1591=none in `.editorconfig` mit dokumentiertem Plan für **Sprint 1.5 — Public API XML-Doc-Sprint** (dispatched-subagents pro Modul) NACH Phase 7.

**Wichtig**: Diese Disable ist temporär. Sprint 1.5 muss XML-Doc auf alle public Members ergänzen, dann CS1591 wieder auf `warning` schalten.

### Kategorie 4: Compile-Errors (echte Bugs)

Keine in Phase 1. Stryker-Code aus 4.14.1 ist kompilierbar — alle "errors" sind Quality-Findings der drei Analyzer.

### Kategorie 5: Mirror-Code-Files — Modernization PFLICHT (kein File-Scope-Pragma)

**Erkennungsmerkmal**: File-Header verweist auf Mono.Cecil, Testura.Mutation oder eine andere konkrete Upstream-Source UND die Klasse trägt `[ExcludeFromCodeCoverage]`.

**Phase 2 Beispiele**:
- `src/Stryker.Utilities/EmbeddedResources/CrossPlatformAssemblyResolver.cs` (Port von Mono.Cecil's `BaseAssemblyResolver`)
- `src/Stryker.Utilities/EmbeddedResources/EmbeddedResourcesGenerator.cs` (Port von Testura.Mutation's `EmbeddedResourceCreator`)

**ANTI-PATTERN (vom User abgelehnt — Phase 2 Iteration)**: File-scoped `#pragma warning disable` über 20+ Regeln mit dokumentiertem Header. War initial vom Subagent vorgeschlagen, vom User in Phase 2 ausdrücklich verworfen mit Begründung: „Quality-Direktive umgangen für ganze Files — User-Goal `Quality-Plus über Upstream` wird teilweise aufgehoben."

**RICHTIGE Pattern-Lösung**: **Mirror-Files genauso modernisieren wie Stryker-original Files** — Nullable-Annotations, `string.Equals(StringComparison.Ordinal)`, `CultureInfo.InvariantCulture`, `ArgumentNullException.ThrowIfNull`, `Array.Empty<T>()`, Method-Splits (MA0051), File-scoped namespaces. Quality-Niveau über Upstream ist eines der erklärten Sprint-Ziele.

**Was bleibt erlaubt — surgical-pragma**: Nur für **einzelne** Code-Zeilen mit konkreter technischer Notwendigkeit:
- `S3011` für Roslyn-internal-Reflection (Forking Roslyn ist alternativlos): siehe `EmbeddedResourcesGenerator.cs` `GetResourceDescriptionInternalName`
- `S3885` für Plugin-Loader `Assembly.LoadFrom` (Pfad-basiertes Laden, `Assembly.Load` löst nicht durch Pfad auf): siehe `IAnalyzerResultExtensions.cs` Plugin-Loader
- `MA0046` für externe Delegate-API (Mono.Cecil-Vertrag): siehe `CrossPlatformAssemblyResolver.cs` `ResolveFailure`-Event
Jede surgical-pragma muss eine **inline-Begründung** in derselben Zeile oder direkt darüber haben (CLAUDE.md-konform).

**Effort**: Mirror-File-Modernization ist substantieller (~30–60 min pro File für Method-Splits + Nullable-Annotations). Wert: einheitliches Quality-Niveau über alle Files. Sprint-2-Velocity wird dadurch realistisch geplant.

**Wichtig**: Bei Subagent-Dispatch IMMER explizit das Anti-Pattern erwähnen: „KEINE file-scope pragma-disables für Mirror-Files. Modernisiere wie Stryker-original Code."

### Kategorie 6: Logger-Pattern-Findings — PHASE 2 NEU

| Regel | Symptom | Pattern-Lösung | Phase 2 |
|-------|---------|----------------|---------|
| **CA2253** | Numeric placeholder `{0}` in LoggerMessage-Template | Benannter Placeholder: `{0}` → `{Analyzer}` | 1 |
| **S6668** | Exception-Argument als Parameter statt Exception-Slot | `LogWarning(message, ex)` → `LogWarning(ex, message)` | 1 |

### Kategorie 7: Locale/Format-Findings — PHASE 2 NEU

| Regel | Symptom | Pattern-Lösung | Phase 2 |
|-------|---------|----------------|---------|
| **MA0011** + **CA1305** | `int.Parse(s)` ohne IFormatProvider | `int.Parse(s, CultureInfo.InvariantCulture)` (mit `using System.Globalization`) | 1 |

### Kategorie 8: API-Signature mit `default`-Initializer — PHASE 2 NEU

| Regel | Symptom | Pattern-Lösung | Phase 2 |
|-------|---------|----------------|---------|
| **CS8625** | `string defaultValue = default` (= null bei String) | `string defaultValue = ""` (truthful non-null default), API-Sig bleibt erhalten | 1 |

### Kategorie 9: Path-Indexer-Returns mit nullable-Source — PHASE 2 NEU

**Symptom**: `FilePathUtils.NormalizePathSeparators(...)` gibt `string?` zurück, public API soll `string` zurückgeben (1:1 mit Stryker.NET 4.14.1).

**Pattern-Lösung**: Null-forgiving operator `!` am Aufrufende, weil:
- Properties-Dictionary-Indexer wirft bei missing key → bei Erfolg ist Wert non-null
- Stryker-API-Konsumenten erwarten non-null `string`-Rückgabe

```csharp
// Pattern 9
public static string GetAssemblyFileName(this IAnalyzerResult analyzerResult) =>
    FilePathUtils.NormalizePathSeparators(analyzerResult.Properties["TargetFileName"])!;
```

---

## 4. Convention-Ausnahmen (dokumentiert)

CLAUDE.md sagt: "Bash-Filesystem-Befehle per Konvention NICHT verwenden". Phase 1 hat zwei Ausnahmen:

### Ausnahme 1: `cp -r` für Bulk-Code-Migration aus _reference/

```bash
cp -r _reference/stryker-4.14.1/src/Stryker.Abstractions src/Stryker.Abstractions
```

**Begründung**: Bulk-Copy von 56 Files aus read-only Reference. Alternative wäre 112 Read+Write-Tool-Calls (Token-/Latenz-Verschwendung). One-shot-Operation, keine Folge-Wiederholung.

### Ausnahme 2: `mkdir -p` für initial directory tree

```bash
mkdir -p src tests benchmarks _docs
```

**Begründung**: Erstmaliges Anlegen der Top-Level-Struktur. `Write` erzeugt zwar implizit Verzeichnisse, aber wir wollen die Struktur explizit setzen vor dem Bulk-Copy.

**Beide Ausnahmen sind ONE-SHOT in Phase 1 dokumentiert.** Folge-Phasen 2–6 nutzen Symbol-Tools (Serena `replace_symbol_body`/`insert_after_symbol`) und Built-In `Write`/`Edit`.

---

## 5. Subagent-Prompt-Schablone für Phasen 2–6

Diese Schablone wird in Phasen 2 (DAG Layer 0 parallel) und 3 (DAG Layer 1 parallel) und 4 (DAG Layer 2 parallel) eingesetzt. Jeder Subagent bekommt:

```
## KONTEXT
Sprint 1 Phase {X}: PILOT (Phase 1, Stryker.Abstractions) ist abgeschlossen. Du bist Subagent für die Modul-Migration `Stryker.<Modul>` aus Layer {N}. Pilot-Lessons in `_docs/sprint_1_lessons.md` GELESEN — Pattern-Bibliothek für CA1720/CA1716/CS8618/CS8765/MA0006/MA0021/MA0016/MA0048/RCS1194/CS1591 bekannt. Build & Test-Kommandos in der Repo-Root via `dotnet build/test stryker-netx.slnx -c Debug`. Worktree-Isolation aktiv, eigener Branch.

## ZIEL
1. Code von `_reference/stryker-4.14.1/src/Stryker.<Modul>/` nach `src/Stryker.<Modul>/` kopieren (Bash `cp -r` als dokumentierte Convention-Ausnahme — siehe Phase 1 Lessons Sektion 4)
2. csproj anpassen: AssemblyName, RootNamespace, PackageId, Description (Pattern siehe `src/Stryker.Abstractions/Stryker.Abstractions.csproj`)
3. Falls Modul Buildalyzer/Roslyn nutzt: `<PackageReference Include="Microsoft.CodeAnalysis.VisualBasic" />` ergänzen (NU1608-Mitigation)
4. `stryker-netx.slnx` mit `<Project Path="src/Stryker.<Modul>/Stryker.<Modul>.csproj" />` ergänzen
5. `dotnet restore stryker-netx.slnx` — verifizieren grün
6. `dotnet build stryker-netx.slnx --no-restore` — Errors klassifizieren
7. Cleanup-Iteration: Pattern-Bibliothek (Sektion 3) systematisch anwenden via Serena-Symbol-Tools
   - **PFLICHT**: Erst `mcp__serena__find_symbol(depth=1, include_body=true)` auf jedes betroffene Symbol, dann `replace_symbol_body`
   - **PFLICHT**: Bei using-Statements für StringComparer/StringComparison: Edit auf File-Header (`using System;`)
   - **PFLICHT**: File-Splits via Write-neue-Files + Edit-alter-File
8. Wiederholt 5–7 bis 0 Errors / 0 Warnings
9. Wenn Test-Projekt für Modul existiert: NACH Production-Migration auch Tests migrieren (MSTest→xUnit, Shouldly→FluentAssertions, [DataRow]→[Theory]+[InlineData])
10. Lessons in dieses Dokument als Update committen falls neue Patterns auftauchen

## CONSTRAINTS (NICHT VERHANDELBAR — CLAUDE.md)
- **Serena-First**: JEDE Code-Symbol-Analyse via `find_symbol`/`replace_symbol_body`/`get_symbols_overview`. KEIN Grep/Read als Fallback für Symbole. (User-Feedback: das ist häufig vergessen worden — strikt einhalten!)
- KEIN `<NoWarn>` ohne dokumentierte Begründung im Code-Kommentar (analog zu `#pragma warning disable`)
- KEIN Public-API-Member-Rename — 1:1-Spirit zu Stryker.NET 4.14.1
- `sealed` default für nicht-vererbbare Klassen
- `ConfigureAwait(false)` auf allen `await`
- `catch (Exception ex) when (ex is not OperationCanceledException)` Pattern
- Keine NuGet-Versions-Änderungen (alles in Directory.Packages.props)
- Conventional Commits + DCO sign-off (`git commit -s`)
- `git add` SPECIFIC files (nicht `git add -A`)

## MCP-ANWEISUNGEN
- **Serena (PFLICHT)**: `find_symbol(depth=1, include_body=true)` vor jeder Code-Änderung; `replace_symbol_body` für Body-Replace; `insert_after_symbol`/`insert_before_symbol` für neue Members; `find_referencing_symbols` für Impact-Check vor Refactor
- **Context7**: Bei NEUEN APIs (z.B. wenn neue Microsoft.CodeAnalysis-Methode genutzt wird) — vor Code-Schreiben
- **Sequential Thinking (Maxential)**: ≥3 Schritte bei Trade-offs (z.B. „File-Split oder Suppress?"), ≥8 bei Algorithmen, ≥10 bei Architektur. Tags setzen.
- **NextGen ToT**: Wenn ≥3 valide Lösungs-Optionen
- **Semgrep**: Vor Subagent-Abschluss: `semgrep scan --config auto src/Stryker.<Modul>/`

## OUTPUT
- Liste der portierten Files (csproj + .cs)
- `dotnet build` Status: 0 Warnings, 0 Errors (Bestätigung)
- `dotnet test` Status (falls Test-Projekt: alle grün)
- Semgrep-Scan: 0 findings
- Liste neu hinzugefügter `.editorconfig`-Tunings (mit Begründung)
- Updates zu diesem Lessons-Doku falls neue Patterns
- Anzahl ausgelöste Analyzer-Errors initial (für Effort-Tracking)
- Conventional-Commit-Hash (mit DCO sign-off)
```

---

## 6. Effort-Tracking (für Phase-Schätzungen)

| Modul (Phase) | Files (.cs) | Initial Errors | Cleanup-Zeit | Notes |
|---------------|-------------|----------------|--------------|-------|
| Stryker.Abstractions (Phase 1, PILOT) | 56 (vor Splits) → 58 (nach Splits) | 25 (real) + 447 (CS1591) | ~2h Hauptsession | Pilot — Lessons-Aufbau |
| Stryker.Utilities (Phase 2) | 15 .cs (nach Phase-2-Splits) | 74 initial → 0 | ~3h: Hauptsession (kleine Files) + Subagent (3 große Files mit Mirror-Pragma) + Hauptsession (Mirror-Pattern abgelehnt → Option B → Mirror-Files manuell modernisiert mit Method-Splits, Nullable-Annotations, CultureInfo, StringComparer.Ordinal) | Worktree-Isolation für Subagent fehlte; Mirror-File-Pragma-Pattern initial vom Subagent vorgeschlagen, vom User verworfen → Sektion 5 als Anti-Pattern dokumentiert |
| Stryker.DataCollector (Phase 2, netstandard2.0) | 2 (Original) → 3 (nach ThrowingListener-Split) | 35 initial → 0 | ~30 min Hauptsession | Sonderfall netstandard2.0; CA1001 (IDisposable wegen TraceListener-Field); kein `required`-Modifier (netstandard2.0-Polyfill-Frei-Stil) — stattdessen nullable annotations |
| Stryker.Configuration (Phase 3) | 59 → 60 (3 file-splits: InputDefinition→3, StrykerInputs→2, IdProvider rename) | 148 → 220 (Welleneffekt durch IStrykerOptions nullable-Annotations) → 0 | ~30 min Subagent | Triggered IStrykerOptions.cs nullable-annotation update in Stryker.Abstractions (interface-properties string→string?); 1 surgical S1075-pragma für DashboardUrlInput; KEIN file-scope-pragma |
| Stryker.RegexMutators (Phase 3) | 22 (1 file-rename: WrappedGuidsEnumeration→WrappedIdentifierEnumeration) | 26 → 0 | ~15 min Subagent | RegexMutation hat 4× required + 1× nullable (ReplacementNode kann null sein); private helper-methods static (CA1822); KEIN pragma |
| Stryker.Solutions (Phase 3, ImplicitUsings=enable Sonderfall) | 4 → 6 (2 file-splits: ISolutionProvider + SolutionProvider) | 21 → 0 | ~15 min Subagent | csproj overrides ImplicitUsings=enable (Stryker-original choice); 24× MA0006 string.Equals; KEIN pragma |
| Stryker.TestRunner (Phase 3) | 7 (1 file-rename) | 4 → 15 (deeper layer) → 0 | ~10 min Subagent | CS0738 backing-field-pattern (Dictionary private + IReadOnlyDictionary public); MA0025 NotImplementedException→NotSupportedException; KEIN pragma |
| Stryker.TestRunner.MicrosoftTestPlatform (Phase 4, ImplicitUsings=enable) | 37 → 42 (5 file-splits) | **143 → 0** | ~30 min Subagent | InternalsVisibleTo-AssemblyAttributes preserved für UnitTest-Project; surgical VSTHRD003-pragma (TaskCompletionSource owned by class); Method-Splits via MutantTestSessionContext nested-class-Pattern |
| Stryker.TestRunner.VsTest (Phase 4) | 13 → 17 (4 file-splits) | **232 → 0** | ~45 min Subagent | EmbeddedResource für Microsoft.TestPlatform.Portable.nupkg + GeneratePathProperty preserved; 3 surgical pragmas (MA0158 ×2 für Monitor.Pulse/Wait; MA0099 ×1 für TestFrameworks-default-state; S1172 ×1 für closure-capture-false-positive); CA1873 + CA1711 als globale .editorconfig-Tunings (CA1873 = LoggerMessage-twin von CA1848 phased; CA1711 = Stryker EventArgs/EventHandler-Naming-Convention) |
| Stryker.Core (Phase 5) | 169 → 196 (24 MA0048 file-splits + 3 helper-files split incl. SseEventSerializerOptions) | **1500 → 0** | ~3.5h: 6 sequential Subagent-Pässe (3a File-Splits, 3b Locale-Cluster, 3c Logger-Cluster, 3d Async/Sealed/Static, 3e CS-Nullable, 3f Long-Tail) + Hauptsession (Setup, MsBuildHelper-Fix, Mutation.cs Description nullable, 2× Shortcut-Revert) | **Drei Subagent-Shortcuts vom User abgelehnt (Sektion 11 dokumentiert)**: Pass 1 `<Nullable>disable</Nullable>` + `null!` litter, Pass 2 `.editorconfig` 22 severity-deferrals, Pass 3 partial-progress mit Aufruf zu erweiterten Severity-Deferrals — alle reverted. ADR-010 MsBuildHelper-Fix (vswhere-Fallback raus, default `dotnet msbuild`). Mutation.Description: `required string` → `string?` (matcht upstream — C#-Mutators setzen Description nicht; nur RegexMutator füllt). 26× `null!` in 14 Files mit Lifecycle-Inline-Comments. 1 surgical pragma (S1696 in CsharpCompilingProcess für Roslyn-internal exception-catch). Buildalyzer-9 API-Migration: keine Breakages — alle Kompatibilitäts-Issues nur Nullable. 4 Stryker.Abstractions interface-Properties nullable-annotiert (cross-module impact, IBaselineProvider, IMutant, IReadOnlyMutant, IProjectComponent, IReadOnlyProjectComponent, IJsonReport, IJsonMutant, IJsonTestFile). |
| Stryker.CLI (Phase 6) | 14 → 27 (12 MA0048 file-splits: FileBasedInput→7, ConfigBuilder→2, StrykerNugetFeedClient→2, LoggingInitializer→2, CommandLineConfigReader→2, StrykerCli rename + ConsoleWrapper extract, FileConfigWriter→FileConfigGenerator rename) | **147 → 0** | ~30 min: 1 All-in-One Subagent-Pass (kleinerer Scope erlaubt monolithischen Pass) + Hauptsession (Setup, Identity-Migration, NuGet CVE-Patch 7.3.0→7.3.1) | **Identity-Migration**: PackageId + ToolCommandName `dotnet-stryker` → `dotnet-stryker-netx`; AssemblyName + RootNamespace bleiben `Stryker.CLI` (1:1 ADR-001). **NuGet GHSA-g4vj-cjjj-v7hg CVE-Patch** beim Restore aufgetaucht (NuGet.Protocol/Packaging 7.3.0 → 7.3.1, Defense-in-Depth package-ID/version validation). 0× `null!` (sauberer als Phase 5 — alle Nullables via `string.Empty`/`?? string.Empty`/nullable-types gelöst). 1 surgical pragma (CS0067 in ConsoleWrapper für IConsole.CancelKeyPress event-contract). CLI Smoke-Test: `--help` works (DoD ✓); `--version` ist project-version-input wie upstream (kein tool-version-flag, McMaster default überschrieben durch Stryker-spezifische Option auf Line 205 in CommandLineConfigReader). |
| Stryker.Architecture.Tests + Stryker.Benchmarks + Sample (Phase 7) | NEW: 1 Architecture.Tests + 1 Benchmarks + 2 Sample-Projekte | n/a (DoD-Setup, keine Cleanup-Errors) | ~1.5h Hauptsession (Setup-driven design, kein Subagent erforderlich) | **6 ArchUnit Layering-Rules** + **4 FsCheck Properties** auf FilePathUtils — alle 10 Tests grün. **3 BenchmarkDotNet Hot-Paths**: FilePathUtils.NormalizePathSeparators (3 Pfad-Varianten), RegexMutantOrchestrator.Mutate (4 Pattern-Komplexitäten), TextSpanHelper.Reduce (3 Span-Counts). **Sample.Library + Sample.Tests** (Calculator-Style mit Add/Subtract/Multiply/IsPositive, 17 xUnit-Tests grün). **`samples/Directory.Build.props`** isoliert Sample-Projekte vom production Directory.Build.props (TWAE + Roslynator + Sonar + Meziantou unnötig für Mutation-Test-Fixtures). **`.editorconfig` Test-Glob** `[{tests,benchmarks,samples}/**/*.cs]` zugefügt: CA1707=none nur in test/benchmark/sample-Dateien (Standard-.NET-Praxis: xUnit-Convention `Method_Should_Do_X` darf Underscores verwenden). 1 surgical Pragma (S2139 in StrykerRunner für DEBUG-only catch-and-log-and-rethrow Pattern, fired nur in Release). **CLI Smoke-Test BLOCKED**: stryker-netx läuft bis zur Buildalyzer-9-Project-Analyse, dann liefert Buildalyzer leere AnalyzerResults zurück ("No analyzer results to log. This indicates an early failure in analysis"). Tool-Pipeline funktioniert UP TO Project-Analysis; Mutation-Run wird in Phase 8 gefixt (Buildalyzer-9 + .NET-10-SDK Integration). |
| End-to-End Mutation-Run (Phase 8) | 5 modified Source-Files + 2 sample-csproj-Edits | n/a (Bug-Fixes, keine Cleanup-Errors) | ~1h Hauptsession (Root-Cause-Analysis via standalone BaDiag-Tool + 5 surgical fixes) | **5 distinct Bug-Fixes**: (1) **AssemblyResolve-Handler in Stryker.CLI/Program.cs** — defensive Microsoft.CodeAnalysis 4.0.0.0 → 5.3.0 Forward via AssemblyLoadContext.Resolving (Buildalyzer-9 EventProcessor strong-name reference). (2) **CommandLineConfigReader.cs** — `cliInput.Value() ?? string.Empty` Phase-6-Cleanup-Bug fixed: nur User-supplied values forwarden, sonst SuppliedInput=null lassen damit Input.Validate auf Default fällt. (3) **FileConfigReader.cs** — gleiche `?? string.Empty / Array.Empty<string>()` Bug in 22 SuppliedInput-Assignments fixed. (4) **Sample csproj `<TargetFramework>net10.0</TargetFramework>`** explicit gesetzt — Buildalyzer's EnvironmentFactory liest TargetFramework NUR aus csproj, nicht aus Directory.Build.props-Inheritance; ohne explicit-Setting fällt Buildalyzer auf .NET-Framework MSBuild.exe (VS BuildTools) zurück, der `Microsoft.NET.Sdk` nicht resolven kann. (5) **Stryker.Solutions/SolutionFile.cs** — Project-Pfade aus `Microsoft.VisualStudio.SolutionPersistence.SolutionModel` sind relativ zur Solution; Buildalyzer braucht aber absolute Pfade (CWD-Mismatch causes "MSB1009: Project file does not exist"). Resolved via `Path.GetFullPath(Path.Combine(solutionDirectory, ...))` in `AnalyzeSolution`. **End-to-End-Test**: `dotnet stryker-netx --solution samples/Sample.sln` → Sample.Library mutiert (Add/Subtract/Multiply/IsPositive Operatoren), 17 Sample.Tests killing ALLE 5 Mutationen, **100.00 % Mutation-Score**. HTML-Report unter `samples/StrykerOutput/.../reports/mutation-report.html` generiert. **Diagnostic-Approach**: standalone BaDiag-Tool in `/tmp/ba-diag` (Buildalyzer 9.0.0 isoliert + verbose ProcessRunner-Logging) hat das `MSB4276 / MSB1009` direkt sichtbar gemacht — ohne diesen Approach wäre der Bug deutlich schwerer zu finden gewesen. |

Grobe Effort-Heuristik: ~5 min pro nicht-trivialen Error, ~30 sec für File-Split, ~1 min pro `.editorconfig`-Tuning.

---

## 7. Sprint-1.5-Plan (XML-Doc-Sprint)

NACH Phase 7 (Sprint-1-DoD geschlossen) startet Sprint 1.5 mit dispatched-subagents (1 pro Modul) für systematische XML-Doc-Generation:

- 11 production-modules × ~50–500 public Members = ~5000–8000 Doc-Comments
- Subagent-Prompt: „Generiere XML-Doc für alle public Members von Stryker.<Modul>. Doc-Body basiert auf Symbol-Body (siehe Method-Signature, Property-Type, etc.). `<summary>` 1-2 Zeilen, `<param>` für jeden Parameter, `<returns>` für non-void, `<exception>` für documented throws. KEIN Re-Wording. KEIN Marketing."
- Nach Sprint 1.5: CS1591 zurück auf `warning` in `.editorconfig` schalten

---

## Änderungshistorie

| Datum | Änderung |
|-------|----------|
| 2026-04-30 | Initial-Erstellung nach Phase 1 PILOT (Stryker.Abstractions) — Build grün 0/0 |
| 2026-04-29 | Phase 2 (Stryker.Utilities) — Build grün 0/0; Sektion 3 erweitert um Kategorien 5–9 (Mirror-Files, Logger-Pattern, Locale/Format, API-Signature-default, Path-Indexer-Pattern) |
| 2026-04-30 | Phase 5 (Stryker.Core) — Build grün 0/0 solution-wide; 169→196 .cs Files; 1500→0 Errors über 6 sequenzielle Subagent-Pässe; ADR-010 MsBuildHelper-Fix angewendet; Mutation.cs Description nullable; 3× Subagent-Shortcuts abgelehnt + revertet (Nullable=disable, .editorconfig-deferrals, partial-progress-mit-Deferrals). Effort-Quintessenz: bei Mega-Modulen (>100 Files) sequentielle Subagent-Pässe pro Regel-Cluster (Locale, Logger, Async/Sealed, CS-Nullable, Long-Tail) deutlich produktiver als ein All-in-One-Subagent. |
| 2026-04-30 | Phase 6 (Stryker.CLI) — Build grün 0/0 solution-wide; 14→27 .cs Files; 147→0 Errors in 1 All-in-One Subagent-Pass; Identity-Migration `dotnet-stryker-netx` (PackageId + ToolCommandName); NuGet 7.3.0→7.3.1 CVE-Patch (GHSA-g4vj-cjjj-v7hg); CLI smoke-test `--help` ✓. Effort-Quintessenz: bei kleineren Modulen (<30 Files) ist ein All-in-One-Subagent-Pass effizienter als die für Mega-Module empfohlene Cluster-Pass-Strategie. |
| 2026-04-30 | Phase 7 (Integration & DoD-Setup) — `tests/Stryker.Architecture.Tests/` (10/10 grün: 6 ArchUnit Layering + 4 FsCheck Properties), `benchmarks/Stryker.Benchmarks/` (3 hot paths in Release), `samples/Sample.{Library,Tests}/` (17/17 grün). Sample-Isolation via `samples/Directory.Build.props` + `.editorconfig` Test-Glob für CA1707. 1 surgical Pragma S2139 (Release-only catch-and-log-and-rethrow in StrykerRunner). **CLI Smoke-Test BLOCKED durch Buildalyzer-9 silent failure** — wird in Phase 8 adressiert. Build solution-wide 0/0 Debug+Release. Test-projects, Benchmarks, Samples alle in `stryker-netx.slnx` integriert. |
| 2026-04-30 | **Phase 8 (E2E Mutation-Run + Buildalyzer-9-Fix)** — 5 distinct Bug-Fixes (AssemblyResolve, 2× empty-string-defaults von Phase-6-Cleanup, csproj TargetFramework, Solution absolute-paths). End-to-End-Test grün: `dotnet stryker-netx --solution Sample.sln` → 5/5 Mutanten in Sample.Library killed durch 17 Sample.Tests, **100% Mutation-Score**, HTML-Report generiert. Diagnostic via standalone BaDiag-Tool im `/tmp/ba-diag` Sandbox war essenziell für Root-Cause. Phase-6-Cleanup-Lehre: `?? string.Empty / Array.Empty` ist NICHT der richtige Fix für CS8601/CS8604-Warnungen, wenn der Konsument (hier `Input.Validate`) auf `null` als "nicht supplied" prüft — proper-fix ist conditional assignment `if (value is { } v) target = v;`. Sprint-1-DoD final closing kommt mit Phase 9. |
| 2026-04-30 | **Phase 9a (Workspaces.MSBuild Adapter parallel)** — Neue Layer-0-Schnittstelle `IProjectAnalysis` (ersetzt Buildalyzer-`IAnalyzerResult` als Public-API-Vertrag), neue `Stryker.Utilities/MSBuild/`-Schicht (`IMSBuildWorkspaceProvider`, `MSBuildWorkspaceProvider`, `RoslynProjectAnalysis`, `IProjectAnalysisExtensions` mit allen 30 Methoden 1:1 portiert). Microsoft.CodeAnalysis.Workspaces.MSBuild 5.3.0 + Microsoft.Build.Locator 1.11.2 hinzu. Microsoft.NET.StringTools 17.14.28 → 18.0.2 (NU1605 Nerdbank-Fix). MSBuildLocator-`buildTransitive`-Quirk: Targets ignorieren `PrivateAssets` → globale Guard-ItemGroup in `Directory.Build.props` für Microsoft.Build* + NuGet.Frameworks (Stryker.DataCollector als netstandard2.0 ausgeschlossen). 0/0 solution-wide, 27/27 Tests, 0 Semgrep-Findings. |
| 2026-04-30 | **Phase 9b (Buildalyzer-Removal + E2E)** — 13 Files migriert von `IAnalyzerResult` → `IProjectAnalysis` (Layer-0 Interfaces, Stryker.Core ProjectComponents + Initialisation + Compiling + MutationTest, Stryker.TestRunner.VsTest, DI-Registration). Stryker.Utilities/Buildalyzer/-Verzeichnis (4 Files) gelöscht. Buildalyzer + Buildalyzer.Logger NuGet-Refs aus Directory.Packages.props + Stryker.Utilities.csproj + Stryker.Core.csproj entfernt. Microsoft.CodeAnalysis.CSharp.Workspaces + .VisualBasic.Workspaces hinzugefügt (MSBuildWorkspace braucht language-services um C# zu laden — sonst Error "Sprache C# wird nicht unterstützt"). Microsoft.CodeAnalysis.Workspaces.MSBuild flow-thru fixed (PrivateAssets="all" verhindert Runtime-Loading bei Consumern). MSBuildLocator.RegisterDefaults() in Stryker.CLI.Program.cs eingebaut. Phase-8-Workarounds **#1 (AssemblyResolve) entfernt** und **#4 (csproj TargetFramework) entfernt** — beide nicht mehr nötig mit MSBuildWorkspace. Workarounds #2 + #3 (Phase-6-eigene empty-string-Bugs) bleiben gefixt. **End-to-End grün: 5/5 Mutanten killed, 100% Score** mit reiner Workspaces.MSBuild-Pipeline. Sprint-1 ist effektiv DoD-erfüllt. |
| 2026-04-30 | **Phase 10 (C# 14 / .NET 10 Modernisierungs-Sweep)** — 6 Sub-Phasen: **10.1 Audit** (233 logger calls, 94 primary-ctor candidates, 61 collection-expr sites, 10 frozen-coll, 23 lock(...)), **10.2 LoggerMessage** (alle 233 ILogger-Calls in 48 Files migriert auf C# 12+ `[LoggerMessage]` Source-Generator-Pattern, partial classes; CA1848 + CA1873 von suggestion → error reaktiviert), **10.3 Collection Expressions + Primary Constructors** (~80 collection-expression sites mechanisch via IDE0028/0300/0301/0302/0305/0306; 8 Klassen mit ≤4-Param-pure-init-Konstruktoren auf primary-constructor-Pattern konvertiert, ~30 Kandidaten skipped wegen Factory-default-Pattern oder multi-ctor), **10.4 Frozen Collections** (7 read-only static Lookups auf FrozenDictionary/FrozenSet — InputFileResolver.ImportantProperties, InitialisationProcess.TestFrameworks, AssignmentExpression/BinaryExpression/PrefixUnary/Statement-Mutators-Lookups; MutantPlacer skipped weil mutated-at-runtime), **10.5+10.6 Pattern Matching** (System.Threading.Lock-Pass no-op da bereits via MA0158-Cascade konvertiert; 7 Combinator-Pattern-Sites via IDE0019/0020/0038/0078: `x > 100 \|\| x < 0` → `x is > 100 or < 0`, `a != X && a != Y` → `a is not (X or Y)` etc.), **10.7 Sprint-Closing** (lessons + DoD + Tag v1.0.0-preview.1 + Issue #1 zu). E2E mutation-Score nach jeder Sub-Phase verifiziert: weiterhin 100% (5/5 mutants killed). 0 neue file-scope-pragmas, 0 neue NoWarn, 0 neue null!-Verwendungen über alle 6 Sub-Phasen hinweg. **Sprint-1 final geschlossen.** |
| 2026-04-30 | **Phase 10.8 (.slnx Validation — Kern-Wertversprechen)** — Phase-8-Era Sample.sln war ein historisches Workaround für Buildalyzer-9; nach Phase-9-Migration auf MSBuildWorkspace + Microsoft.VisualStudio.SolutionPersistence ist .slnx (XML-Format, .NET-8/9/10-Default) voll unterstützt. Verifikation: `samples/Sample.sln` gelöscht, `samples/Sample.slnx` (XML-Format via `dotnet new sln -f slnx`) als kanonisches Sample, E2E-Test `dotnet stryker-netx --solution Sample.slnx` → 5/5 Mutanten killed, **100.00 % Mutation-Score**. Damit ist das Kern-Wertversprechen von stryker-netx beweisen: **".slnx funktioniert wo upstream Stryker.NET 4.14.1 versagt"**. |
