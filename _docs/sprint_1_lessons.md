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
| (Phase 5) Stryker.Core | TBD | Schätzung 200–500 | TBD | Hauptsession + Buildalyzer-9 + MsBuildHelper-Fix |
| (Phase 6) Stryker.CLI | TBD | Schätzung 100–200 | TBD | Hauptsession + Identity-Migration + Wrapper |

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
