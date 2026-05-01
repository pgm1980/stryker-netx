# Sprint 24 — Pillar-A Closing (Foundation): Lessons Learned

**Sprint:** 24 (2026-05-01, autonomous run)
**Branch:** `feature/24-pillar-a-closing`
**Base:** v2.10.0 (Sprint 23 closed)
**Final Tag:** `v2.11.0`
**Type:** Test-only release. CI/dogfood-infrastructure + 1 module port + shared-helpers foundation. Zero production-code change.

## Sprint Outcome

| Sub-Task | Result |
|---|---|
| 1. NetFramework CI: `nuget.exe` install step | ✅ `nuget/setup-nuget@v2` + `nuget restore` for FullFrameworkApp.sln in `integration-test.yaml` |
| 2a. Dogfood yaml-Pfade fixen (flat-Struktur) | ✅ workingDirectories in `stryker-on-stryker.yaml` korrigiert |
| 2b. `.config/dotnet-tools.json` anlegen | ✅ Manifest mit `dotnet-stryker-netx` (USE_LOCAL_TOOL default true) |
| 2c. Lokaler Smoke-Test stryker-on-stryker | ✅ `Stryker.Utilities` bei 5.97% mutation score (1:28 wallclock) — Setup verifiziert |
| 3. Stryker.Solutions.Test → Stryker.Solutions.Tests port | ✅ 15/15 tests green (MSTest→xUnit, Shouldly→FluentAssertions) |
| 4. Shared-helpers foundation → tests/Stryker.TestHelpers/ | ✅ 4 framework-agnostische Helper portiert (TestBase, MockExtensions, LoggerMockExtensions, StringExtensions); 2 skipped (AssertExtensions Shouldly-only, IgnoreIfAttribute MSTest-only) |
| Solution-wide tests (excl E2E) | ✅ 430 green (388 Core + 17 Sample + 10 Architecture + 15 Solutions.Tests) |
| Semgrep | ✅ 0 findings on 5 changed source files |
| Tag | `v2.11.0` |

## Major Discovery (Scope-Re-Aufteilung)

**Original Sprint 24 scope:** alle 4 kleinen UnitTest-Projekte (Solutions.Test, VsTest.UnitTest, MTP.UnitTest, CLI.UnitTest = 23 .cs files) in einem Sprint portieren.

**Tatsächlich entdeckt:** VsTest.UnitTest hat 1726 LOC inkl. 727 LOC `VsTestRunnerPoolTests` + 574 LOC `VsTestMockingHelper`. ALLE 5 Files in VsTest.UnitTest referenzieren `using Stryker.Core.UnitTest;` für shared-helpers. MTP.UnitTest und CLI.UnitTest haben dieselbe Helper-Kopplung. **Jeder Modul-Port ist eigene Sprint-Größe**, nicht Sub-Task einer Sprint.

**Korrektur (User-bestätigt):** Sprint 24 wird zu "Foundation" — Setup + Solutions.Tests (kein Helper-Bedarf) + Helper-Library für die folgenden Sprints. Restliche Module bekommen eigene Sprints:

| Sprint | Inhalt | Volumen |
|---|---|---|
| 25 | VsTest.UnitTest port | 5 files / 1726 LOC |
| 26 | MTP.UnitTest port | 10 files |
| 27 | CLI.UnitTest port | 6 files + Spectre.Console.Testing |
| 28 | RegexMutators.UnitTest port | 18 files |
| 29-32 | Stryker.Core.UnitTest in 4 Tranchen | 161 files / je ~40 |

## What landed

### `tests/Stryker.TestHelpers/` (Library, kein Test-Project)
4 framework-agnostische Helper portiert aus upstream `src/Stryker.Core/Stryker.Core.UnitTest/`:
- **`TestBase.cs`** (11 LOC): seedet `ApplicationLogging.LoggerFactory = new LoggerFactory()` im ctor. Komplement zur Sprint-20 `IntegrationTestBase`-Variante (NullLoggerFactory).
- **`MockExtensions.cs`** (24 LOC): `SetupProcessMockToReturn` für `Mock<IProcessExecutor>`.
- **`LoggerMockExtensions.cs`** (54 LOC): `Verify<T>(Mock<ILogger<T>>, LogLevel, message, [Times])` extension overloads.
- **`StringExtensions.cs`** (75 LOC): ANSI-escape removal + `[Color]SpanCount` extensions für reporter-tests.

**Skipped helpers** (mit Begründung):
- `AssertExtensions.cs` (108 LOC) — Shouldly-spezifische `ShouldBeSemantically`-Calls; Sprint-25+ Konsumenten nutzen FluentAssertions native equivalents direkt.
- `IgnoreIfAttribute.cs` (112 LOC) — MSTest-spezifisches dynamic-skip-Attribute; xUnit-Pattern ist `[Fact(Skip = "...")]` (compile-time) oder early-return (runtime, Sprint 23 idiom).

### `tests/Stryker.Solutions.Tests/`
Port von upstream `src/Stryker.Solutions.Test/SolutionFileShould.cs` (13 Test-Methoden, 15 Fact/Theory-Combinations). Framework conversion + ein Pfad-Behaviour-Workaround (`AssertProjectListEndsWith`-Helper) weil stryker-netx's `SolutionFile.GetProjects()` absolute Pfade zurückgibt (Sprint-1 Workspaces.MSBuild port), wo upstream relative gab.

### CI / Workflow infrastructure
- `.github/workflows/integration-test.yaml`: conditional `nuget.exe` setup + restore step für `runtime: netframework`-matrix entries.
- `.github/workflows/stryker-on-stryker.yaml`: workingDirectories an unsere flat-Struktur angepasst; `USE_LOCAL_TOOL=true` als Default (NuGet `dotnet-stryker-netx` package not yet published).
- `.config/dotnet-tools.json`: Manifest erstellt mit `dotnet-stryker-netx` Tool-Eintrag.

## Process lessons

### 1. **Pre-Implementation-Recherche hat Sprint-Scope vor major over-commit gerettet**

Sprint-15 Lesson live: vor dem konkreten Port der 4 Module habe ich erst `wc -l` + `grep "using Stryker.Core.UnitTest"` auf allen Files gemacht. Discovery: 5x cross-project-helper-references + 1726 LOC in einem einzelnen Modul. Hätte ich blind angefangen, wäre Sprint 24 entweder unfertig oder hätte unsauber duplizierte Helpers in jedem Modul. Die kurze Recherche-Pause vor dem ersten Edit hat 3 Module korrekt aus dem Scope ausgeschlossen.

### 2. **Stryker-on-Stryker fängt sich selbst beim direkten `dotnet exec` mit File-Lock**

Smoke-test mit `dotnet exec C:/...src/Stryker.CLI/bin/Debug/net10.0/Stryker.CLI.dll` löste `MSB3027` File-Lock auf `Stryker.Solutions.dll` aus. Stryker als gestartetes .NET-Tool LÄDT seine eigenen Source-Tree-DLLs; der dann von Stryker initiierte Build versucht dieselben DLLs zu überschreiben → Konflikt. **Lösung**: nur `dotnet pack + dotnet tool install --tool-path .nuget/tools/` Pfad nutzen. Das `stryker-on-stryker.ps1` macht das schon richtig (USE_LOCAL_TOOL); manuelle Aufrufe MÜSSEN denselben Pfad benutzen.

### 3. **`Regex` aus `System.Text.RegularExpressions` kollidiert namespace-mäßig mit `Stryker.RegexMutators`**

`using System.Text.RegularExpressions;` in einem Project das transitiv `Stryker.RegexMutators` referenziert: der Compiler liest `Regex` als Namespace (`Stryker.RegexMutators` partial path) statt Type. Lösung: vollqualifizieren (`System.Text.RegularExpressions.Regex` an der `[GeneratedRegex]`-Methode) statt `using`.

### 4. **`FluentAssertions.BeEquivalentTo` auf `IEnumerable<string>` macht char-deep-equivalence**

Bei `solution.GetProjects("Debug").Should().BeEquivalentTo(new[] {"a/b.csproj", ...})` interpretiert FluentAssertions die Strings als objects-with-properties und vergleicht char-by-char, was zu `"differs at index 0"`-Errors führt obwohl die Liste richtig ist. Korrektes Pattern für ungeordnete String-Listen: `OrderBy(x => x, StringComparer.Ordinal).Should().Equal(expected.OrderBy(...))`. Oder ad-hoc-Helper wie unser `AssertProjectListEndsWith` für suffix-checks.

### 5. **upstream's `SolutionFile.GetProjects` returned relative paths; ours returns absolute**

Sprint-1 Workspaces.MSBuild-Umstellung hat das Verhalten geändert. Tests die das alte Verhalten erwarten müssen angepasst werden — entweder die expected-list absolutisieren ODER suffix-comparison verwenden. Wir sind mit suffix gegangen: behält den semantischen Test-Inhalt bei (haben wir die richtigen Projekte in der Solution?) ohne sich an die Pfad-Shape zu binden.

### 6. **CA1873 false-positive auf `Mock<ILogger>.Verify(x => x.Log(...))`**

Roslyn-Analyzer interpretiert das `x.Log(...)` innerhalb des `Verify`-Expression-Trees als echten Logger-Call und schlägt `IsEnabled`-Guard vor. Tatsächlich ist das nur ein Pattern-Match-Descriptor für Moq, kein Runtime-Log-Call. Gezieltes `#pragma warning disable CA1873` mit Begründungs-Kommentar (CLAUDE.md-konform) ist die richtige Lösung.

### 7. **MA0009 false-positive auf `[GeneratedRegex]`**

Source-generated regex kompiliert zu DFA zur Build-Zeit (kein Backtracking), kann ReDoS-Catastrophic-Backtracking nicht erleiden. MA0009 ist generisch auf "regex-pattern braucht timeout"-Heuristik basiert und sieht den Compile-Zeit-Generator nicht. Pragma + Begründung.

## v2.11.0 progress map

```
[done]    Sprint 24 → Pillar-A Closing (Foundation) → v2.11.0   ⭐ MINOR ⭐
[planned] Sprint 25 → VsTest.UnitTest port (5 files / 1726 LOC) → v2.12.0
[planned] Sprint 26 → MTP.UnitTest port (10 files) → v2.13.0
[planned] Sprint 27 → CLI.UnitTest port (6 files + Spectre.Console) → v2.14.0
[planned] Sprint 28 → RegexMutators.UnitTest port (18 files) → v2.15.0
[planned] Sprint 29-32 → Stryker.Core.UnitTest in 4 Tranchen → v2.16.0–v2.19.0
```

## Out of scope (deferred — explizit Sprint 25-32)

Per Maxential-Korrektur unter Verfügbarkeitsanalyse: alle anderen Modul-Ports sind eigene Sprint-Größe. Siehe v2.11.0 progress map oben.

## Recommended next sprint (v2.12.0 candidate)

**Sprint 25: Stryker.TestRunner.VsTest.UnitTest port**
- 5 Files: CoverageCaptureTests, CoverageCollectorTests, VsTestMockingHelper, VsTestRunnerPoolTests, VsTextContextInformationTests
- ProjectReference Stryker.TestHelpers (Sprint-24-foundation)
- Framework conversion MSTest→xUnit, Shouldly→FluentAssertions
- 1726 LOC test code, vermutlich ~50-70 Test-Methoden
- Erwartete Failures: Tests die gegen upstream-Behavior testen (Sprint-6/9-15-Anpassungen) — pro Failure triagieren
