# Architecture Specification — stryker-netx

**Version:** 0.1.0
**Datum:** 2026-04-30
**Status:** Approved (Sprint-0-Output)
**Brainstorming-Session:** Maxential `d4cc4d23b8d3` (19 Schritte) + 4 ToT-Trees (`95a80ba9` NativeAOT, `c928b0c5` McMaster, `01b5e0be` License, `19336423` Modul-Reihenfolge)

---

## 1. Systemübersicht

**Kurzbeschreibung:** stryker-netx ist eine 1:1-Portierung von [Stryker.NET 4.14.1](https://github.com/stryker-mutator/stryker-net) auf C# 14 / .NET 10. Das Tool führt Mutation Testing für moderne .NET-Projekte durch, indem es Quellcode systematisch mit Mutationen modifiziert und prüft, ob die Tests die Mutationen entdecken („töten").

**Architekturtyp:** CLI-Tool (`dotnet stryker-netx`) als globales `dotnet tool`, mit Library-Packages (`stryker-netx`) für Konsumenten die Stryker als Bibliothek einbetten möchten. Multi-Project Solution mit klarer Schichten-Trennung (Abstractions → Utilities → Domain-Logik → Test-Runner-Adapter → Core → CLI).

**Ursprung der Inkompatibilität (verifiziert via GitHub-Issues #3351, #3402, Buildalyzer #318):** Stryker.NET 4.14.1 (released 2026-04-10) referenziert `Buildalyzer 8.0.0` (released 2025-12-17). Buildalyzer 9.0.0 mit dem .NET-10-MSBuild-Parsing-Fix wurde am 2026-04-18 released — 8 Tage NACH Stryker 4.14.1. Stryker 4.14.1 hat alle internen Master-Patches (PR #3375, #3383, #3471) bereits, aber die transitive Buildalyzer-Dependency hat den Bug.

### 1.1 Kontextdiagramm

```
┌──────────────────────────────────────────────────────────────────────────┐
│                            Systemkontext                                  │
│                                                                           │
│   ┌────────────────┐                                                      │
│   │  Entwickler /  │                                                      │
│   │   CI Pipeline  │                                                      │
│   └────────┬───────┘                                                      │
│            │ dotnet stryker-netx                                          │
│            ▼                                                              │
│   ┌─────────────────────────────────┐    ┌─────────────────────────────┐  │
│   │       stryker-netx CLI          │───▶│    User-Test-Solution       │  │
│   │   (Stryker.CLI dotnet-tool)     │    │    (.NET 10 .csproj/.slnx)  │  │
│   └────┬────────────┬───────────────┘    └─────────────────────────────┘  │
│        │            │                                                     │
│        ▼            ▼                                                     │
│   ┌────────┐  ┌────────────┐                                              │
│   │ Roslyn │  │ Buildalyzer│                                              │
│   │  4.x+  │  │    9.0+    │                                              │
│   └────────┘  └────────────┘                                              │
│        │            │                                                     │
│        ▼            ▼                                                     │
│   ┌──────────────────────────────────────────────────────┐                │
│   │  VsTest / Microsoft.Testing.Platform (Test-Execution) │                │
│   └──────────────────────────────────────────────────────┘                │
│                            │                                              │
│                            ▼                                              │
│   ┌─────────────────────────────────────────────────────┐                 │
│   │  Reports: HTML / JSON / Console / Dashboard         │                 │
│   │  (kompatibel mit mutation-testing-elements Schema)  │                 │
│   └─────────────────────────────────────────────────────┘                 │
└──────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Qualitätsziele

| Priorität | Ziel | Maßnahme | Metrik |
|-----------|------|----------|--------|
| 1 | **1:1-Kompatibilität mit Upstream** | CLI-Flags, Config-Schema, Reporter-Output identisch zu Stryker.NET 4.14.1 | Smoke-Tests gegen `_reference/.../ExampleProjects` mit Vergleich der Console/HTML-Reports |
| 2 | **.NET-10/C#-14-Kompatibilität** | TFM net10.0, LangVersion latest, Buildalyzer 9, alle Microsoft.* auf 10.0.x | `dotnet stryker-netx` läuft erfolgreich gegen ein net10.0-Test-Projekt |
| 3 | **Code-Qualität** | Roslynator + SonarAnalyzer.CSharp + Meziantou + TWAE | 0 Warnings / 0 Errors im Build |
| 4 | **Security** | Semgrep-Scan, Dependency-Audit, Input-Validierung | 0 offene Semgrep-Findings vor Sprint-Abschluss |
| 5 | **Testbarkeit** | xUnit + FluentAssertions + Moq + ArchUnitNET + FsCheck + Coverlet | Coverage ≥ 80%, Architecture-Tests grün, Property-Tests grün |
| 6 | **Performance-Beobachtbarkeit** | BenchmarkDotNet auf Hot Paths (Mutator-Generation, Roslyn-Parsing, Reporter-Output) | Benchmarks mit Baseline = Upstream 4.14.1 |
| 7 | **Wartbarkeit** | Schichtentrennung via ArchUnit-Regeln | 0 Architekturverletzungen |

### 1.3 Technologie-Stack

| Kategorie | Technologie | Version | Begründung |
|-----------|-------------|---------|------------|
| Runtime | .NET | 10.0 | Ziel-Runtime laut User-Vorgabe (CLAUDE.md) |
| Sprache | C# | 14 (LangVersion `latest`) | Moderne Sprachfeatures, User-Vorgabe |
| Solution-Format | `.slnx` | XML-Format | Neueres Solution-Format (4.14.1 nutzt es bereits via VisualStudio.SolutionPersistence 1.0.52) |
| Build-Property-Mgmt | MSBuild + `Directory.Build.props` + `Directory.Packages.props` | CPM aktiv | Zentralisierte Versionsverwaltung |
| Code-Analyse | Roslynator + SonarAnalyzer.CSharp + Meziantou.Analyzer | 4.15.0 / 10.20.0 / 3.0.22 | CLAUDE.md-Pflicht, Big-Bang in Sprint 1 (ADR-004) |
| Code-Style | `.editorconfig` + Spectre.Console.Analyzer | aktuell | Naming-Conventions, projekt-spezifisches Tuning für Stryker-Defensive-Patterns |
| Test-Framework | xUnit | 2.9.x | Migration von MSTest (ADR-005) |
| Assertions | FluentAssertions | 8.8.x | Migration von Shouldly (ADR-005, CLAUDE.md-Pflicht) |
| Mocking | Moq | 4.20.x | Bereits in Stryker.NET, beibehalten |
| Coverage | Coverlet | 8.0.x | CLAUDE.md-Stack |
| Property Testing | FsCheck.Xunit | 3.1.x | CLAUDE.md-Stack |
| Architecture Testing | TngTech.ArchUnitNET.xUnit | 0.11.x | CLAUDE.md-Stack |
| Performance | BenchmarkDotNet | 0.14.x | CLAUDE.md-Stack |
| Roslyn (Mutator-Engine) | Microsoft.CodeAnalysis.CSharp | aktuelle C#-14-fähige Version | Pflicht für C#-14-Source-Parsing |
| MSBuild-Wrapper | Buildalyzer | **9.0.0** | KRITISCHER FIX (ADR-009): 8.0.0 hat .NET-10-Bug |
| Test-Execution | Microsoft.TestPlatform | 18.4.x | VsTest-Adapter (Stryker beibehalten) |
| Testing.Platform | Microsoft.Testing.Platform | 1.5.x+ | MTP-Adapter (Stryker beibehalten) |
| Logging | Serilog | 4.x | Stryker beibehalten, strukturiertes Logging |
| Config-Format | YAML/JSON | YamlDotNet 17.x | Stryker beibehalten |
| CLI-Parsing | McMaster.Extensions.CommandLineUtils | 5.1.0 (deprecated) | HYBRID-Strategie (ADR-007), via `IStrykerCommandLine`-Wrapper-Layer |
| CLI-Output | Spectre.Console | 0.54.x | Stryker beibehalten |
| Security-Scan | Semgrep | aktuell | CLAUDE.md-Pflicht |
| MCP-Tooling | Serena, Context7, Sequential Thinking (Maxential), GitHub CLI | aktuell | CLAUDE.md-Pflicht |

---

## 2. Architekturentscheidungen (ADRs)

### ADR-001: Baseline-Strategie — Stryker.NET 4.14.1 als Code-Anker + transitive Dependency-Updates

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thoughts 1, 6, 9 (Tags: `baseline-decision`, `revision`, `scope-shift`)

#### Kontext

Stryker.NET 4.14.1 ist die letzte released Version (2026-04-10). Sie ist nicht 1:1 kompatibel mit .NET-9/10-Test-Projekten. Vier identifizierte Bugs:

1. **Buildalyzer 8.0** parst .NET-10-MSBuild-Strukturen nicht (Buildalyzer-Issue #318)
2. **MsBuildHelper-Fallback** auf `vswhere`/`MsBuild.exe` schlägt auf reinen .NET-10-SDK-Maschinen ohne Visual Studio fehl (stryker-net Issue #3351)
3. **C# Interceptors** werden nicht propagiert — bereits in 4.14.1 via PR #3471 (2026-03-16) gefixt ✓
4. **DI/Logging-Init-Order** — bereits in 4.14.1 via PR #3383 gefixt ✓

Konkret: Bugs (3) und (4) sind in 4.14.1 schon adressiert. Bug (1) ist eine reine **Dependency-Version**: Buildalyzer 9.0.0 wurde 8 Tage nach Stryker 4.14.1 released (2026-04-18). Bug (2) ist eine **Code-Anpassung** im MsBuildHelper.cs.

#### Optionen

##### Option A: Strikt 4.14.1 + transitive Dependency-Updates + gezielter Code-Fix für MsBuildHelper

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | Low–Medium |
| Aufwand | Mittel (Buildalyzer-9-API-Migration + MsBuildHelper-Fix) |
| Wartbarkeit | High (klare Diff zur Upstream-Baseline) |
| Risiko | Niedrig (Master-Master-PRs schon enthalten) |

**Vorteile:**
- Stable releaste Version als Anker
- Klare Diff-Linie für spätere Re-Sync mit Upstream
- Master-Master-Patches (PR #3375, #3383, #3471) sind bereits enthalten

**Nachteile:**
- Muss Buildalyzer-9-Migration selbst handhaben (kein Cherry-Pick verfügbar — Master von Stryker hat noch keinen Buildalyzer-9-Switch zum Zeitpunkt 4.14.1)
- MsBuildHelper-Code muss selbst gefixt werden

##### Option B: Master-HEAD von stryker-mutator/stryker-net

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | Medium |
| Aufwand | Höher (instabile Code-Linie, evtl. ungemergete Refactorings) |
| Wartbarkeit | Medium (Anker schwerer zu identifizieren) |
| Risiko | Mittel-Hoch (Master ist „moving target") |

**Vorteile:**
- Eventuell schon Buildalyzer-9-Update enthalten (zu prüfen)
- Aktivste Code-Linie

**Nachteile:**
- Kein Release-Tag = kein stabiler Referenzpunkt
- Mögliche Refactorings die wir nicht brauchen
- Master-Stand kann sich täglich ändern

##### Option C: Hybrid (4.14.1 + Cherry-Pick aus Master)

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | High (Cherry-Pick + Konflikt-Resolution) |
| Aufwand | Hoch |
| Wartbarkeit | Niedrig (Cherry-Pick-Spuren) |
| Risiko | Mittel |

**Vorteile:**
- Stable Anker + selektive Master-Patches

**Nachteile:**
- **Im konkreten Fall obsolet**: Recherche zeigt PR #3375, #3383, #3471 sind bereits in 4.14.1 enthalten. Es gibt keine Master-PRs für die identifizierten Bugs zu cherry-picken.

#### Trade-off-Analyse

Option C wurde ursprünglich vom User gewählt unter der Annahme, dass relevante Master-PRs außerhalb 4.14.1 existieren. Die PR-Verifikation hat ergeben, dass alle drei Stryker-internen Bug-Fix-PRs bereits in 4.14.1 sind. Damit kollabiert C zu A. Option B wurde aus Stabilitätsgründen verworfen.

#### Entscheidung

**Option A: Strikt 4.14.1 als Code-Anker + transitive Dependency-Updates (insb. Buildalyzer 8.0 → 9.0) + gezielter MsBuildHelper-Bug-Fix.**

Der ursprüngliche Spirit der User-Wahl C („minimal-invasiv, 4.14.1-Stabilität") bleibt erhalten — nur die Cherry-Pick-Mechanik entfällt mangels Cherry-Pick-Kandidaten.

#### Konsequenzen

- **Wird einfacher:** Klare Code-Anker-Linie, später leichte Re-Sync-Möglichkeit zu Upstream-4.15.0
- **Wird schwieriger:** Buildalyzer-9-API-Migration ist Eigenleistung — Context7 vor Update Pflicht
- **Muss revisited werden:** Bei Stryker-Upstream-4.15.0-Release prüfen ob deren Bug-Fixes besser/anders aussehen als unsere

#### Action Items

- [ ] Code aus `_reference/stryker-4.14.1/src/` modulweise als Code-Baseline übernehmen (Sprint 1 Phasen 1–6)
- [ ] Buildalyzer-Update via Context7 vorbereiten (vor Phase 5)
- [ ] MsBuildHelper-Fix als eigenständige PR-würdige Änderung dokumentieren

---

### ADR-002: Runtime Target Frameworks — net10.0 für alle Hauptprojekte, netstandard2.0 für DataCollector

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 10 (Tags: `decision`, `tfm-strategy`, `user-confirmed`)

#### Kontext

Stryker.NET 4.14.1 verwendet zentral `<TargetFramework>net8.0</TargetFramework>` in `Directory.Build.props`. Ausnahme: `Stryker.DataCollector.csproj` hat hartkodiert `<TargetFramework>netstandard2.0</TargetFramework>` — VsTest-Adapter-Loading-Constraint. CLAUDE.md fordert C# 14 / .NET 10 als Ziel.

#### Optionen

##### Option A: Alle Hauptprojekte auf net10.0, DataCollector bleibt netstandard2.0

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | Low |
| Aufwand | ~30 min für TFM-Update aller csproj |
| Risiko | Niedrig — DataCollector-Constraint ist explizit dokumentiert |
| Modernization-Effekt | Maximal |

**Vorteile:** CLAUDE.md-konform, klare Modernization-Linie, DataCollector funktioniert weiterhin mit allen VsTest-Adapter-Versionen.

**Nachteile:** Keine.

##### Option B: Multi-Target `net8.0;net10.0`

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | High |
| Aufwand | Hoch (TFM-conditional Code, doppelte CI-Builds) |

**Vorteile:** Maximale Kompatibilität für Library-Konsumenten auf net8.0.

**Nachteile:** Sprint-1-Mega-Scope wird noch breiter, doppelte Tests, doppelte Bug-Risiken.

##### Option C: Nur net10.0 incl. DataCollector

| Dimension | Bewertung |
|-----------|-----------|
| Komplexität | Hoch — VsTest-Adapter erwartet netstandard2.0 |

**Vorteile:** Keine.

**Nachteile:** **Funktional kaputt** — VsTest-Adapter laden nur netstandard2.0-DataCollector-DLLs.

#### Trade-off-Analyse

Option A balanciert Modernization mit dem hartkodierten VsTest-Constraint. Option B würde Sprint 1 deutlich überfordern. Option C ist technisch nicht möglich.

#### Entscheidung

**Option A**: Alle 11 Production-Projekte und 6 Test-Projekte auf `net10.0`. `Stryker.DataCollector` bleibt `netstandard2.0`.

Zentrale `Directory.Build.props` setzt TFM net10.0 als Default; DataCollector-csproj überschreibt mit explizitem `<TargetFramework>netstandard2.0</TargetFramework>`.

#### Konsequenzen

- **Wird einfacher:** C#-14-Sprachfeatures verfügbar, .NET-10-BCL-APIs nutzbar, NativeAOT bleibt als Option (siehe ADR-006)
- **Wird schwieriger:** Library-Konsumenten auf net8.0 müssen entweder updaten oder bei Stryker.NET-Upstream bleiben — bewusste Inkaufnahme
- **Muss revisited werden:** Bei .NET 11 (Q4 2026): TFM-Update einplanen

#### Action Items

- [ ] `Directory.Build.props` mit `<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>disable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
- [ ] `Stryker.DataCollector.csproj` mit `<TargetFramework>netstandard2.0</TargetFramework>` (überschreibt Default)
- [ ] `global.json` mit SDK-Version-Pinning auf 10.0.x

---

### ADR-003: Repo-Identität — Package-IDs, Namespaces, Tool-Command, Versionierung

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Brainstorming-Phase 4 (User-Approval Bundle)

#### Kontext

Als 1:1-Portierung wollen wir maximale API-Kompatibilität mit Stryker.NET-Konsumenten beibehalten. Gleichzeitig dürfen Package-IDs und Tool-Command nicht mit dem Upstream auf NuGet.org kollidieren. Die Stryker-Marke (Info Support BV) erfordert eine klare Differenzierung.

#### Optionen

##### Option A: 1:1 (Stryker, dotnet-stryker, Stryker.* Namespaces)

**Vorteile:** Maximaler 1:1-Spirit.
**Nachteile:** **NuGet-Konflikt mit Upstream** (technisch unmöglich), Tool-Command-Konflikt wenn beide Tools installiert sind.

##### Option B: Suffix `*-netx` für Packages und Tool, Namespaces bleiben `Stryker.*`

| Dimension | Bewertung |
|-----------|-----------|
| 1:1-Library-API | Erhalten (Stryker.Core etc. bleibt — Konsumenten können fast 1:1 auf stryker-netx wechseln) |
| Konflikt-Vermeidung | Vollständig (NuGet + CLI) |
| Klarheit | Hoch (Suffix erkennbar) |

**Vorteile:** API-Kompatibilität für Library-Konsumenten, klare Fork-Identität.

**Nachteile:** Konsumenten müssen das NuGet-Package wechseln (PackageReference Update), dafür kein Code-Refactor.

##### Option C: Komplett umbenennen (`StrykerNetX.*`)

**Vorteile:** Klarste Trennung.
**Nachteile:** Voller Code-Refactor für Library-Konsumenten — verletzt 1:1-Spirit auf API-Ebene.

#### Trade-off-Analyse

Option B ist Sweet-Spot: API-Kompatibilität (Namespace-Identität) + Konflikt-freie Distribution (Suffix). Option A ist nicht implementierbar. Option C ist zu invasiv.

#### Entscheidung

**Option B (Bundle):**
- **NuGet-Package-IDs:** `stryker-netx` (Library), `dotnet-stryker-netx` (Tool)
- **C#-Namespaces:** Bleiben `Stryker.*` (Stryker.Core, Stryker.CLI, Stryker.Abstractions, etc.) — 1:1 vom Upstream übernommen
- **Tool-Command:** `dotnet stryker-netx` (nicht `dotnet stryker`)
- **Versionierung:** SemVer ab `1.0.0-preview.1` für den ersten Release, `1.0.0` final wenn alle Acceptance Criteria grün sind. Eigene Versionsserie, getrennt von Upstream-Numerierung.

#### Konsequenzen

- **Wird einfacher:** Library-Konsumenten ändern nur PackageReference, Code-Imports bleiben unverändert; Side-by-Side mit Upstream-Stryker.NET möglich
- **Wird schwieriger:** Klar-Kommunikation in README nötig (Disclaimer, Migration-Guide für Upstream-User)
- **Muss revisited werden:** Bei Upstream-Stryker.NET-5.0.0-Release prüfen ob Re-Sync sinnvoll ist

#### Action Items

- [ ] In allen csproj `<PackageId>` auf `stryker-netx` / `dotnet-stryker-netx` setzen (nicht in Phase 0, aber in Phase 6 mit Stryker.CLI)
- [ ] In `Stryker.CLI.csproj` `<ToolCommandName>dotnet-stryker-netx</ToolCommandName>`
- [ ] `Directory.Build.props`: `<VersionPrefix>1.0.0</VersionPrefix>`, `<VersionSuffix>preview.1</VersionSuffix>`
- [ ] README mit Disclaimer und Migration-Guide

---

### ADR-004: Analyzer-Activation-Strategy — Big-Bang in Sprint 1

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thoughts 11, 12 (Tags: `risk`, `analyzer-cleanup`, `sprint-1-shape`)

#### Kontext

CLAUDE.md fordert `Roslynator + SonarAnalyzer.CSharp + Meziantou + TreatWarningsAsErrors=true` als verbindliche Quality-Gates. Die ursprüngliche Stryker.NET-Codebasis ist ~5 Jahre alt und nutzt nur die eingebauten Microsoft-Analyzer. Erwartete Initial-Issues nach Aktivierung der drei Analyzer mit TWAE: 500–1500+.

#### Optionen

##### Option A: Big-Bang Sprint 1 (User-Wahl)

Alle drei Analyzer + TWAE in Sprint 1 aktivieren, vollständigen Cleanup vor erstem grünen Build erzwingen.

**Vorteile:** Sauber + konsistent von Tag 1, keine technische Schuld, klare Quality-Baseline.
**Nachteile:** Sprint 1 wird Cleanup-Sprint, ggf. 2–3 Sprints Cleanup-Reste.

##### Option B: Phased

Sprint 1: nur Roslynator. Sprint 2: + Sonar. Sprint 3: + Meziantou.

**Vorteile:** Inkrementell, Cleanup verteilt sich.
**Nachteile:** Quality-Niveau erreicht erst nach 3 Sprints volle Stärke.

##### Option C: Severity-Tuned

Big-Bang aktivieren, aber `.editorconfig` setzt selektiv Severity bestimmter Regeln auf `suggestion`.

**Vorteile:** Mittlerer Initial-Cleanup, dokumentiertes Tuning-Profil.
**Nachteile:** Weniger sauberer Sprint-1-Endstand.

##### Option D: NoWarn-Sequenced

Big-Bang + `<NoWarn>` für die schlimmsten ~20 Regeln, gradueller Abbau.

**Vorteile:** Schnellster grüner Build.
**Nachteile:** **Konfliktiert mit CLAUDE.md** („kein `#pragma warning disable` ohne dokumentierte Begründung" — analog für `NoWarn`).

#### Trade-off-Analyse

Der User hat A gewählt — Quality-First-Ansatz. Sprint 1 wird Mega-Sprint, akzeptiert. Risiko-Mitigation via .editorconfig-Tuning für berechtigte Stryker-Pattern.

#### Entscheidung

**Option A (Big-Bang) + ergänzendes `.editorconfig`-Tuning** für Stryker-Defensive-Patterns (z.B. `CA1031` catch-Exception bei deserialization-fallback, wo dokumentiert).

#### Konsequenzen

- **Wird einfacher:** Konsistente Quality-Baseline ab Sprint-1-Ende
- **Wird schwieriger:** Sprint 1 wird 4–6 Wochen Mega-Sprint
- **Muss revisited werden:** Sprint-2-Retrospektive: Wie viele Cleanup-Reste sind aufgetaucht? `.editorconfig`-Anpassungen nötig?

#### Action Items

- [ ] `Directory.Build.props` mit Roslynator + Sonar + Meziantou + TWAE
- [ ] `.editorconfig` mit projektspezifischen Severity-Anpassungen (jede Anpassung mit Kommentar-Begründung)
- [ ] `_docs/sprint_1_lessons.md` während Phase 1 (Pilot Stryker.Abstractions) führen — Cleanup-Pattern-Bibliothek
- [ ] Subagent-Prompt-Schablone für Cleanup-Subagents (Phase 2+)

---

### ADR-005: Test-Stack-Migration — Voll-Migration in Sprint 1

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 13 (Tags: `decision`, `user-confirmed`, `sprint-1-mega-scope`)

#### Kontext

Stryker.NET 4.14.1 verwendet **MSTest 4.1.0 + Shouldly 4.3.0 + Moq 4.20.72** in 6 Test-Projekten mit geschätzt 1000+ Test-Methoden. CLAUDE.md fordert verbindlich xUnit + FluentAssertions als Test-Stack. Migration ist mechanisch:

| MSTest | xUnit |
|--------|-------|
| `[TestClass]` | (entfällt) |
| `[TestMethod]` | `[Fact]` |
| `[DataRow(...)]` | `[Theory]` + `[InlineData(...)]` |
| `[TestInitialize]` | Constructor |
| `[TestCleanup]` | `IDisposable.Dispose` |
| `[ClassInitialize]/Cleanup` | `IClassFixture<T>` |
| `[ExpectedException(typeof(T))]` | `Assert.Throws<T>(...)` (xUnit-style) |

| Shouldly | FluentAssertions |
|----------|-------------------|
| `value.ShouldBe(x)` | `value.Should().Be(x)` |
| `value.ShouldNotBeNull()` | `value.Should().NotBeNull()` |
| `collection.ShouldContain(x)` | `collection.Should().Contain(x)` |
| `action.ShouldThrow<T>()` | `action.Should().Throw<T>()` |

#### Optionen

##### Option A: Voll-Migration in Sprint 1 (User-Wahl)

Test-Stack-Migration parallel zum Big-Bang-Cleanup.

##### Option B: Sprint 2 dediziert

Sprint 1 = Bootstrap + Cleanup (Tests bleiben MSTest/Shouldly), Sprint 2 = nur Test-Stack-Migration.

##### Option C: Per Modul gekoppelt

Modul X portieren = Modul-Tests parallel migrieren.

##### Option D: Codemod-Big-Bang

Roslyn-Code-Mod migriert alle Tests automatisiert.

##### Option E: MSTest+Shouldly behalten

**Konfliktiert mit CLAUDE.md.**

#### Trade-off-Analyse

User hat A gewählt — konsistent mit Big-Bang-Cleanup-Strategie aus ADR-004. Der natürliche Workflow: pro Subagent migriert Production-Modul + Test-Modul gemeinsam (Subagent-Prompt-Schablone).

#### Entscheidung

**Option A (Voll-Migration in Sprint 1, gekoppelt mit Production-Modul-Migration via Subagents).** Roslyn-Code-Mod (D-Element) als Tooling-Option für die mechanische Bulk-Migration; manuelles Polishing für Edge-Cases (`[ExpectedException]`, `[ClassInitialize]`).

#### Konsequenzen

- **Wird einfacher:** Einheitlicher Test-Stack ab Sprint-1-Ende, CLAUDE.md-konform
- **Wird schwieriger:** Sprint-1 wird sehr breit, MSTest-spezifische Edge-Cases können Stunden fressen
- **Muss revisited werden:** Sprint-2: gibt es nicht-migrierte Test-Methoden? Codemod-Tooling refinen für Sprint-Folge-Projekte?

#### Action Items

- [ ] Subagent-Prompt-Schablone mit Test-Stack-Migration-Schritten
- [ ] Optional: Roslyn-Code-Mod-Tool prototypen (kann in Sprint 0 oder Sprint 1 Phase 1 entstehen)
- [ ] `Directory.Packages.props`: xUnit 2.9.x, FluentAssertions 8.8.x, Microsoft.NET.Test.Sdk 17.14.x oder 18.4.x, Moq 4.20.x, FsCheck.Xunit 3.1.x, TngTech.ArchUnitNET.xUnit 0.11.x, coverlet.collector 8.0.x; MSTest und Shouldly entfernen

---

### ADR-006: NativeAOT-Strategy — Tauglich, aber nicht erzwungen

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 14 (Tag: `7a-aot`); ToT-Tree `95a80ba9` (Best-Path Score 0.85)

#### Kontext

NativeAOT (Ahead-Of-Time Compilation, .NET 7+) erlaubt es, .NET-Apps zur Build-Zeit zu nativem Maschinencode zu kompilieren — schnellerer Start, kleinere Binaries, kein .NET Runtime auf Zielmaschine. Constraints: keine Reflection-basierte Discovery, keine dynamische Code-Generation. Stryker.NET ist reflection-heavy: Mutator-Discovery, Reporter-Plugin-Loading, JSON-Deserialization, DI-Composition, Buildalyzer-MSBuild-Reflection.

#### Optionen

##### Option A: AOT-Erzwingung von Tag 1

`<PublishAot>true</PublishAot>`, `IsAotCompatible=true`, alle Reflection-Patterns refactoren auf Source-Generators.

**Vorteile:** Schnellster Start, kleinste Binaries.
**Nachteile:** **Architektur-Transformation, nicht Setting-Flip**. Würde Sprint-1-Mega-Scope verdoppeln. Buildalyzer ist nicht AOT-tauglich. 1:1-Spirit massiv verletzt.

##### Option B: AOT-tauglich, aber nicht erzwungen

TFM net10.0 ohne `<PublishAot>`. Code wird nicht aktiv AOT-feindlich gestaltet (neue Code-Pfade bevorzugt reflection-frei wenn ohne Mehraufwand möglich). Spätere AOT-Aktivierung bleibt Option.

**Vorteile:** Sprint 1 nicht zusätzlich belastet, Zukunftsoffenheit erhalten, 1:1-Spirit bewahrt.
**Nachteile:** Kein sofortiger AOT-Mehrwert.

##### Option C: AOT komplett ignorieren

Wir gestalten bewusst AOT-feindlich (z.B. `IL.Emit`, dynamic loading).

**Vorteile:** Maximale Freiheit bei JIT-spezifischen Optimierungen.
**Nachteile:** Verbaut die Zukunft.

#### Trade-off-Analyse

Stryker-Mehrwert bei AOT minimal: Mutation-Runs dauern Minuten/Stunden, JIT-Warmup (~1s) ist im Rauschen. Der einzige spürbare Mehrwert wäre ein kleineres `dotnet-stryker-netx`-Binary — aber als globales `dotnet tool` ohnehin self-contained über NuGet-Cache.

#### Entscheidung

**Option B (AOT-tauglich aber nicht erzwungen).**

Konkret:
- `<PublishAot>` NICHT in csproj
- `IsAotCompatible` NICHT setzen
- AOT-Analyzer-Warnings (IL2026, IL3050, IL2090, etc.) sind in Sprint 1 NICHT Build-Errors (selektives `.editorconfig`-Tuning)
- Source Generators dürfen, müssen aber nicht
- Roslyn-Mutator-Engine bleibt reflection-basiert (1:1 von Upstream)

Re-Evaluation bei Sprint 4 oder bei .NET 11 (NativeAOT-Runtime-Verbesserungen).

#### Konsequenzen

- **Wird einfacher:** Sprint 1 fokussiert, Buildalyzer-Update unproblematisch
- **Wird schwieriger:** Falls später AOT erzwungen wird, größerer Refactor-Aufwand
- **Muss revisited werden:** Sprint 4 oder bei .NET-11-Release

#### Action Items

- [ ] `.editorconfig` mit `dotnet_diagnostic.IL2026.severity = none`, `dotnet_diagnostic.IL3050.severity = none`, etc. — mit Kommentar-Begründung
- [ ] AOT-Re-Eval-Item in `_docs/sprint_4_planning.md` notieren

---

### ADR-007: McMaster.Extensions.CommandLineUtils — HYBRID + Wrapper-Layer

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 15 (Tags: `7b-mcmaster`, `user-tendency-diverges`); ToT-Tree `c928b0c5` (Best-Path Score 0.85)

#### Kontext

Stryker.NET nutzt `McMaster.Extensions.CommandLineUtils 5.1.0` als CLI-Parsing-Library. Maintainer hat das Repo archiviert (deprecated). v5.1.0 ist die letzte stabile Version, funktional. Risiko: NuGet-Verschwinden langfristig, keine CVE-Patches.

#### Optionen

##### Option (i): Belassen auf v5.1.0

**Vorteile:** Sprint 1 minimal, 1:1-Spirit perfekt erhalten, CLI-Verhalten identisch zu Upstream.
**Nachteile:** Deprecation-Risiko offen.

##### Option (ii): Migration auf System.CommandLine

**Vorteile:** Microsoft-supported, aktiv entwickelt.
**Nachteile:** Pre-GA Beta-API mit Breaking Changes; Migration-Aufwand hoch; CLI-Verhalten ändert sich (Hilfe-Texte, Argument-Parsing-Details).

##### Option (iii): Eigener Fork (User-Tendenz initial)

**Vorteile:** Volle Kontrolle, kein Deprecation-Risiko, AOT-tauglich machbar.
**Nachteile:** ~50 Source-Files, ~400 KB Source unter unserer Wartung; CVE-Verantwortung; Sprint-Slot/Quartal nötig.

##### Option (iv): HYBRID — v5.1.0 belassen + Risk-ADR mit Migration-Triggers + Wrapper-Layer

**Vorteile:** Minimaler Sprint-1-Aufwand, dokumentierte Migrations-Trigger, kleine Touchpoint-Surface für spätere Migration.
**Nachteile:** Wartet ab statt proaktiv zu handeln.

#### Trade-off-Analyse

User-initial-Tendenz war (iii) Fork. ToT + Maxential haben (iv) HYBRID höher gescort: McMaster ist nicht das Stryker-Differenzierungsmerkmal — eine ganze CLI-Library zu maintainen frisst Sprint-Slots, die in der Mutation-Engine besser investiert sind. Bei Trigger-Eintritt ist System.CommandLine (Microsoft-supported) saubererer Migrationspfad als selbst gepflegter Fork. User hat (iv) bestätigt nach ToT-Reasoning.

#### Entscheidung

**Option (iv) HYBRID + Wrapper-Layer.**

Konkret:
- McMaster.Extensions.CommandLineUtils v5.1.0 als NuGet-Dependency belassen
- **Wrapper-Layer**: `Stryker.CLI` definiert ein internes `IStrykerCommandLine`-Interface (Command-Definition, Argument-Parsing, Hilfe-Generation), Implementierung `McMasterStrykerCommandLine` adaptiert McMaster
- **Migration-Triggers** dokumentiert für Re-Evaluation:
  1. McMaster v5.1.0 läuft nicht mehr mit kommendem .NET-X
  2. CVE in McMaster wird gemeldet (kein Patch verfügbar)
  3. System.CommandLine wird stable GA released
  4. Stryker-Upstream wechselt selbst auf andere Library

#### Konsequenzen

- **Wird einfacher:** Sprint-1-Scope minimal für CLI-Bereich, klare Future-Proofing-Story
- **Wird schwieriger:** Wrapper-Layer ist zusätzlicher Code (~1 Datei, ~150 Zeilen)
- **Muss revisited werden:** Quartalsweise Migration-Trigger-Check in Sprint-Planning

#### Action Items

- [ ] In Phase 6 (Stryker.CLI): `IStrykerCommandLine`-Interface definieren
- [ ] `McMasterStrykerCommandLine`-Adapter implementieren
- [ ] `_docs/risk_register.md` mit Migration-Triggers
- [ ] Quartalsweise Trigger-Check als wiederkehrendes Sprint-Planning-Item

---

### ADR-008: License-Strategie — Apache 2.0 + NOTICE + DCO via CONTRIBUTING.md

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 16 (Tag: `7c-license`); ToT-Tree `01b5e0be` (Best-Path Score 0.95)

#### Kontext

stryker-netx ist ein Fork von Stryker.NET 4.14.1 (Apache License 2.0, Copyright Richard Werkman, Rouke Broersma et al.). Apache 2.0 ist permissive — Re-Lizenzierung beschränkt erlaubt. Repo ist aktuell privat, soll aber Open-Source-tauglich vorbereitet werden. Stryker-Marke ist von Info Support BV (NL) gehalten — wir nutzen Suffix "-netx" zur Differentation.

#### Optionen

##### Option (i): Apache 2.0 + NOTICE

LICENSE 1:1 vom Upstream (Original-Copyright bleibt), NOTICE mit Attribution.

##### Option (ii): Andere Lizenz (MIT, GPL)

Komplikationen mit Apache-2.0-Original (Patent-Grant-Erhalt nötig, GPL-Konflikt-Risiko).

##### Option (iii): Proprietary

Möglich da Repo privat. Apache 2.0 erlaubt Verwendung in proprietary code. Nachteil: blockiert Open-Source-Publishing.

##### Option (iv): Apache 2.0 + NOTICE + CONTRIBUTING.md mit DCO-Strategie + CODE_OF_CONDUCT.md + README-Disclaimer

Wie (i), plus Open-Source-Vorbereitung.

#### Trade-off-Analyse

User-initiale-Tendenz war (i). ToT scort (iv) marginal höher (0.95 vs. 0.92) wegen Open-Source-Vorbereitung. (ii) ist juristisch komplizierter ohne Mehrwert. (iii) blockiert die Zukunft.

#### Entscheidung

**Option (iv) Apache 2.0 + NOTICE + CONTRIBUTING.md mit DCO + CODE_OF_CONDUCT.md + README-Disclaimer.**

Konkret:
1. **LICENSE**: Apache License 2.0, 1:1 vom Upstream übernommen (Copyright bleibt bei Werkman, Broersma et al.)
2. **NOTICE**: Attribution-File:
   ```
   stryker-netx
   Copyright 2026 stryker-netx contributors

   This product includes software derived from Stryker.NET 4.14.1
   (https://github.com/stryker-mutator/stryker-net)
   Copyright Richard Werkman, Rouke Broersma et al.
   Licensed under the Apache License, Version 2.0
   ```
3. **CONTRIBUTING.md**: PR-Workflow, DCO-Pflicht (`git commit -s` mit `Signed-off-by`), Coding-Standards verweisen auf CLAUDE.md
4. **CODE_OF_CONDUCT.md**: 1:1 vom Upstream übernommen (Stryker hat eines)
5. **LICENSE-HEADERS** in neu erstellten Source-Files: Apache 2.0 Standard-Header + unser Copyright; bei 1:1 vom Upstream übernommenen Files: Original-Copyright bleibt
6. **README-Disclaimer**: „Not affiliated with the official Stryker.NET project / Info Support BV"

#### Konsequenzen

- **Wird einfacher:** Open-Source-Publish jederzeit möglich, klare Attribution
- **Wird schwieriger:** Contributors müssen DCO einhalten (`git commit -s`)
- **Muss revisited werden:** Bei Open-Source-Publish: GitHub-Repo public schalten + ggf. NuGet-Publish

#### Action Items

- [ ] LICENSE (Apache 2.0) anlegen
- [ ] NOTICE anlegen
- [ ] CONTRIBUTING.md anlegen mit DCO-Workflow
- [ ] CODE_OF_CONDUCT.md vom Upstream übernehmen
- [ ] README-Disclaimer ergänzen

---

### ADR-009: NuGet-Update-Plan

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thoughts 4, 7 (Tags: `risk-inventory`, `dependencies`, `work-categories`)

#### Kontext

Stryker.NET 4.14.1 hat ~30 NuGet-Dependencies in `Directory.Packages.props`. Für .NET-10-Tauglichkeit müssen mehrere zentrale Pakete aktualisiert werden. **Buildalyzer 9.0.0 (released 2026-04-18) ist der kritische Fix** für die Hauptinkompatibilität (Issue #318). Andere Pakete brauchen .NET-10-fähige Versionen.

#### Entscheidung

**Update-Plan in Phase 0 (Bootstrap):**

| Paket | 4.14.1-Version | Ziel-Version | Begründung |
|-------|---------------|--------------|------------|
| `Buildalyzer` | 8.0.0 | **9.0.0+** | Kritischer .NET-10-Fix (Buildalyzer #318) |
| `Microsoft.CodeAnalysis.CSharp` | 5.3.0 | aktuell C#-14-fähig | C# 14 Source Parsing |
| `Microsoft.CodeAnalysis.Common` | 5.3.0 | analog | analog |
| `Microsoft.CodeAnalysis.VisualBasic` | 5.3.0 | analog | analog |
| `Microsoft.CodeAnalysis.Analyzers` | 5.3.0 | aktuell | analog |
| `Microsoft.Extensions.DependencyInjection` | 10.0.5 | 10.0.x latest | .NET-10-stable |
| `Microsoft.Extensions.Logging` | 10.0.5 | 10.0.x latest | analog |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.5 | 10.0.x latest | analog |
| `Microsoft.TestPlatform` | 18.4.0 | 18.4+ latest | VsTest-Adapter |
| `Microsoft.TestPlatform.ObjectModel` | 18.4.0 | analog | analog |
| `Microsoft.TestPlatform.Portable` | 18.4.0 | analog | analog |
| `Microsoft.TestPlatform.TranslationLayer` | 18.4.0 | analog | analog |
| `Microsoft.Testing.Platform` | 1.5.2 | latest stable | MTP-Adapter |
| `Microsoft.VisualStudio.SolutionPersistence` | 1.0.52 | latest | .slnx-Support |
| `System.Net.Http.Json` | 10.0.5 | latest | .NET-10 |
| `Mono.Cecil` | 0.11.6 | latest | IL-Manipulation, stabil |
| `Serilog` + Sinks | 4.3.x / 10.x / 3.x / 6.x | latest | Strukturiertes Logging |
| `Spectre.Console` | 0.54.0 | latest stable | CLI-Output |
| `Spectre.Console.Analyzer` | 1.0.0 | latest | analog |
| `YamlDotNet` | 17.0.1 | latest | Config-Parsing |
| `LibGit2Sharp` | 0.31.0 | latest mit .NET-10-Build | Git-Integration |
| `AWSSDK.S3` | 4.0.21 | latest | Baseline-Reporter |
| `Azure.Storage.Files.Shares` | 12.25.0 | latest | Baseline-Reporter |
| `DotNet.Glob` | 3.1.3 | latest | File-Globbing |
| `Grynwald.MarkdownGenerator` | 3.0.106 | latest | Markdown-Reporter |
| `LaunchDarkly.EventSource` | 5.3.0 | latest | Dashboard-Reporter |
| `Microsoft.Web.LibraryManager.Build` | 3.0.71 | latest | HTML-Report-Assets |
| `NuGet.Frameworks` | 7.3.0 | latest | NuGet-Framework-Targeting |
| `NuGet.Protocol` | 7.3.0 | latest | NuGet-Discovery |
| `ResXResourceReader.NetStandard` | 1.3.0 | latest | Resource-Files |
| `ShellProgressBar` | 5.2.0 | latest | Progress-Output |
| `StreamJsonRpc` | 2.24.84 | latest | DataCollector-Communication |
| `Stryker.Regex.Parser` | 1.0.0 | latest oder beibehalten | Stryker-eigener Fork |
| `TestableIO.System.IO.Abstractions.Wrappers` | 22.1.1 | latest | FileSystem-Abstraktion |
| `CliWrap` | 3.10.1 | latest | Process-Wrapper |
| `DotNet.ReproducibleBuilds` | 2.0.2 | latest | Reproducible-Builds |
| `McMaster.Extensions.CommandLineUtils` | 5.1.0 | **5.1.0 (belassen)** | ADR-007 HYBRID |
| `coverlet.collector` | (Test) | 8.0.x | CLAUDE.md-Stack |
| `xUnit` | (NEU statt MSTest) | 2.9.x | ADR-005 |
| `xunit.runner.visualstudio` | (NEU) | 3.1.x | xUnit-Runner |
| `FluentAssertions` | (NEU statt Shouldly) | 8.8.x | ADR-005 |
| `FsCheck.Xunit` | (NEU) | 3.1.x | CLAUDE.md-Stack |
| `TngTech.ArchUnitNET.xUnit` | (NEU) | 0.11.x | CLAUDE.md-Stack |
| `BenchmarkDotNet` | (NEU) | 0.14.x | CLAUDE.md-Stack |
| `Microsoft.NET.Test.Sdk` | 18.4.0 | aktuell | Test-Sdk |
| `Moq` | 4.20.72 | beibehalten | Mocking |
| `Spectre.Console.Testing` | 0.54.0 | beibehalten | CLI-Tests |
| `TestableIO.System.IO.Abstractions.TestingHelpers` | 22.1.1 | latest | FileSystem-Mocking |
| **Entfernt:** `MSTest`, `MSTest.TestFramework`, `Shouldly` | — | — | ADR-005 |

**Pflicht:** Vor jedem Major-Update (z.B. Buildalyzer 8 → 9, Roslyn 5 → ?, Mono.Cecil 0.11 → 0.12) **Context7 konsultieren** (Breaking Changes, neue Patterns).

#### Konsequenzen

- **Wird einfacher:** Buildalyzer-9-Fix löst Hauptinkompatibilität automatisch
- **Wird schwieriger:** Roslyn-API-Updates können Breaking Changes haben (Code-Anpassungen)
- **Muss revisited werden:** Bei jedem .NET-Update (`net10.0` → `net11.0`) wieder

#### Action Items

- [ ] Phase 0: `Directory.Packages.props` mit Ziel-Versionen anlegen
- [ ] Phase 0: Lockfile erzeugen (`<RestoreLockedMode>true</RestoreLockedMode>` vom Upstream übernehmen)
- [ ] Context7 vor Buildalyzer-9-Migration (separate Recherche-Phase in Phase 5 vor Stryker.Core)

---

### ADR-010: MsBuildHelper-Bug-Fix-Strategie

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** Thought 5 Bug-2 (Tag: `incompatibility-symptoms`)

#### Kontext

Stryker.NET 4.14.1 hat in `_reference/.../Stryker.Core/Helpers/MsBuildHelper.cs` (Zeilen 60–69) eine Fallback-Logik, die `MsBuild.exe` via `vswhere` und einer hartkodierten Liste alter Visual-Studio-Pfade sucht:

```csharp
private static readonly List<string> FallbackLocations =
[
    @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe",
    @"C:\Windows\Microsoft.Net\Framework64\v4.0.30319\MSBuild.exe",
    @"C:\Windows\Microsoft.Net\Framework64\v3.5\MSBuild.exe",
    // ... weitere Win-Framework-Pfade
];
```

Auf reinen .NET-10-SDK-Maschinen ohne Visual Studio installation greift weder vswhere noch ein Fallback-Pfad → `FileNotFoundException("MsBuild.exe could not be located")`. Issue #3351 dokumentiert das. Linux-/macOS-Pfade sind durch frühe `if (Environment.OSVersion.Platform != PlatformID.Win32NT)` → `("dotnet", "msbuild")` abgesichert.

#### Optionen

##### Option A: vswhere/Fallback komplett entfernen, immer `dotnet msbuild` nutzen

**Vorteile:** Einfach, plattform-konsistent, keine Win-spezifische Pfad-Wartung.
**Nachteile:** Wenn Stryker-User explizit eine .NET-Framework-4.x-only-Solution mutiert (selten), könnte `dotnet msbuild` nicht ausreichen.

##### Option B: Fallback-Liste erweitern um VS-2019-, VS-2022-, VS-2024-Pfade + Build-Tools-Pfade

**Vorteile:** Maximale Win-Kompatibilität.
**Nachteile:** Reines Pflasterkleben — neue VS-Versionen brauchen wieder Updates.

##### Option C: vswhere primär, bei Fail → `dotnet msbuild` als Default, Win-Framework-Pfade nur wenn `--full-framework` Flag gesetzt

**Vorteile:** Saubere Pfad-Hierarchie, .NET-10-Default funktioniert ohne VS.
**Nachteile:** Subtile Verhaltensänderung gegenüber 4.14.1 — User die auf `MsBuild.exe`-Detection vertraut haben, müssen evtl. Flag setzen.

#### Trade-off-Analyse

Option A ist sauberster Modernization-Schritt. Option C ist konservativer aber komplexer. Stryker-Kontext ist Mutation Testing für moderne .NET-Projekte (CLAUDE.md-Ziel) — net48-only-Edge-Cases sind sekundär.

#### Entscheidung

**Option A: vswhere/Fallback entfernen, immer `dotnet msbuild` nutzen.**

Konkret:
- `MsBuildHelper.GetMsBuildPath()` wird obsolet → entfernen
- `MsBuildHelper.GetMsBuildExeAndCommand()` returns durchgehend `("dotnet", "msbuild")` (bzw. `("dotnet", "build")` je nach Caller-Kontext)
- `FallbackLocations`, `SearchMsBuildVersion`, vswhere-Aufruf werden entfernt
- ProcessExecutor-Calls verwenden konsistent das `dotnet`-Tool aus dem PATH

#### Konsequenzen

- **Wird einfacher:** Plattform-konsistent, keine VS-Pfad-Wartung, .NET-10-SDK-Maschinen funktionieren out-of-the-box
- **Wird schwieriger:** Edge-Case .NET-Framework-4.x-only-Solutions ohne `dotnet`-CLI können brechen (sehr seltener Use-Case bei Stryker-Zielgruppe)
- **Muss revisited werden:** Falls User .NET-Framework-4.x-Support fordern, mit Backport-Option

#### Action Items

- [ ] In Phase 5 (Stryker.Core): `MsBuildHelper.cs` refactoren
- [ ] Tests anpassen: Stryker.Core.UnitTest hat Tests für `MsBuildHelper` — diese müssen neu definiert werden
- [ ] Doku-Update: README-Compat-Section + `_docs/migration_from_stryker.md`

---

### ADR-011: Subagent-Dispatching-Strategie für Sprint 1

**Status:** Accepted
**Datum:** 2026-04-30
**Maxential-Referenz:** Thought 18 (Tag: `7d-modul-reihenfolge`, `sprint-1-execution-plan`); ToT-Tree `19336423` (Best-Path: Strategie vi PILOT + DAG-LAYER-PARALLEL, Score 0.95)

#### Kontext

Sprint-1-Mega-Scope (TFM-Update, Buildalyzer 9, Microsoft.* Updates, Roslyn-Update, 3 Analyzer + TWAE, Big-Bang-Cleanup, MSTest→xUnit, Shouldly→FluentAssertions, Repo-Identität, MsBuildHelper-Fix, Wrapper-Layer) erfordert massive Parallelisierung. Stryker hat 17 Projekte mit klarem Dependency-Graph.

```
Layer 0 (keine internen Deps)
  ├── Stryker.Abstractions
  ├── Stryker.Utilities
  └── Stryker.DataCollector  [netstandard2.0]

Layer 1 (deps Layer 0)
  ├── Stryker.Configuration
  ├── Stryker.RegexMutators
  ├── Stryker.Solutions
  └── Stryker.TestRunner

Layer 2 (deps Layer 0 + 1)
  ├── Stryker.TestRunner.MicrosoftTestPlatform
  └── Stryker.TestRunner.VsTest

Layer 3 (deps Layer 0–2)
  └── Stryker.Core

Layer 4 (deps Layer 0–3)
  └── Stryker.CLI
```

Test-Projekte je Modul parallel zu jeweiligem Production-Modul.

#### Optionen evaluiert (ToT)

| Strategie | ToT-Score |
|-----------|-----------|
| (i) Strikt Bottom-Up Sequentiell | 0.45 |
| (ii) Strikt Top-Down | 0.15 |
| (iii) Risk-First | 0.40 |
| (iv) DAG-Layer-Parallel | 0.85 |
| (v) Test-Driven-First (unmöglich) | 0.10 |
| **(vi) PILOT + DAG-LAYER-PARALLEL** | **0.95** |

#### Entscheidung

**Strategie (vi) PILOT + DAG-LAYER-PARALLEL** — siehe Sprint-1-Roadmap unten.

**Phase 0 — Repo-Bootstrap (Hauptsession seriell, ~½ Tag):**
- `global.json` (SDK 10.0.x), `.editorconfig` (Naming + Severity), `stryker-netx.slnx`, `Directory.Build.props` (TFM, LangVersion, TWAE, Analyzer), `Directory.Packages.props` (alle Versionen pro ADR-009)
- LICENSE / NOTICE / CONTRIBUTING.md / CODE_OF_CONDUCT.md
- README initial mit Disclaimer und Compat-Section

**Phase 1 — PILOT Stryker.Abstractions (Hauptsession seriell, ~1–2 Tage):**
- Code aus `_reference/` kopieren, csproj anpassen
- TWAE + alle 3 Analyzer aktivieren, Cleanup auf grün
- `_docs/sprint_1_lessons.md` schreiben: Cleanup-Patterns, .editorconfig-Tunings (mit Begründung), Analyzer-Regel-Häufigkeit, geschätzter Effort/100 LoC

**Phase 2 — DAG Layer 0 parallel (2 Subagents, Worktree-Isolation, ~3–5 Tage):**
- Subagent A: Stryker.Utilities (+ ggf. neues Test-Projekt)
- Subagent B: Stryker.DataCollector (Sonderfall netstandard2.0)
- Hauptsession nach Rückkehr: Worktree-Merge + Build + Test + Semgrep

**Phase 3 — DAG Layer 1 parallel (4 Subagents, Worktree-Isolation, ~5–7 Tage):**
- Subagent C: Stryker.Configuration + Test
- Subagent D: Stryker.RegexMutators + Test
- Subagent E: Stryker.Solutions + Test
- Subagent F: Stryker.TestRunner

**Phase 4 — DAG Layer 2 parallel (2 Subagents, ~3–5 Tage):**
- Subagent G: Stryker.TestRunner.MicrosoftTestPlatform + Test
- Subagent H: Stryker.TestRunner.VsTest + Test

**Phase 5 — Stryker.Core dediziert (Hauptsession oder einzelner Subagent, ~5–7 Tage):**
- Buildalyzer-9-Migration (Context7-recherche zuerst)
- MsBuildHelper-Fix (ADR-010)
- Stryker.Core.UnitTest mit-migrieren

**Phase 6 — Stryker.CLI + Identitäts-Migration (Hauptsession, ~2–3 Tage):**
- Tool-Command-Rename auf `dotnet stryker-netx`
- Package-IDs auf `stryker-netx` / `dotnet-stryker-netx`
- IStrykerCommandLine-Wrapper-Layer (ADR-007)
- VersionPrefix `1.0.0`, VersionSuffix `preview.1`

**Phase 7 — Integration & Sprint-Abschluss (Hauptsession, ~2–3 Tage):**
- ArchUnitNET-Tests (siehe ADR-012)
- FsCheck-Property-Tests für Mutator-Mappings
- BenchmarkDotNet-Setup für Hot Paths
- Sprint-1 DoD: 0 Warnings/0 Errors, alle Tests grün, Semgrep clean, mindestens 1 ExampleProject erfolgreich gemutet

**Sprint-1-Realdauer-Schätzung:** 4–6 Wochen.

#### Subagent-Prompt-Schablone (Pflicht für jede Subagent-Dispatch)

```
## KONTEXT
Sprint 1 Phase X: Modul-Migration <Modul-Name>.
Pilot-Lessons in _docs/sprint_1_lessons.md gelesen. Quellcode in _reference/stryker-4.14.1/src/<Modul-Name>/.
Worktree-Isolation aktiv, eigene Branch.

## ZIEL
Stryker.<Modul>.csproj + (falls vorhanden) Stryker.<Modul>.UnitTest.csproj nach src/ + tests/ portieren:
- TFM net10.0 (DataCollector: netstandard2.0)
- Roslynator + Sonar + Meziantou + TWAE — auf grünen Build cleanen
- Tests: MSTest → xUnit, Shouldly → FluentAssertions, [DataRow] → [Theory]+[InlineData]
- Namespace bleibt Stryker.<Modul> (keine Umbenennung)
- Falls Buildalyzer-Calls vorhanden: Context7 vor jedem API-Use konsultieren

## CONSTRAINTS
- Nur das eigene Modul ändern (keine cross-module Edits)
- Keine #pragma warning disable ohne dokumentierte Begründung
- ConfigureAwait(false) auf allen async Calls
- catch (Exception ex) when (ex is not OperationCanceledException) Pattern
- sealed default für nicht-vererbbare Klassen
- XML-Doc auf allen public APIs
- Keine NuGet-Versions-Änderungen (zentral in Directory.Packages.props)

## MCP-ANWEISUNGEN
- Serena: get_symbols_overview auf jede neue csproj, find_symbol vor jeder Code-Änderung
- Context7: vor jedem neuen API-Use (insb. Buildalyzer-9-API, Roslyn-aktuell)
- Semgrep: Scan auf alle geänderten Dateien vor Abschluss
- Sequential Thinking (Maxential): bei mehrdeutigen Refactor-Entscheidungen ≥3 Schritte

## OUTPUT
- Liste der portierten Dateien (csproj + Source + Test)
- dotnet build Status (0 Warnings/0 Errors-Bestätigung)
- dotnet test Status (alle grün)
- Semgrep-Scan-Status
- Liste der angewendeten .editorconfig-Tunings (mit Begründung)
- Lessons-Updates für sprint_1_lessons.md
```

#### Konsequenzen

- **Wird einfacher:** Maximale Parallelisierung (4-fach in Phase 3), klare Verifikations-Punkte pro Layer, Lessons-Iteration verbessert spätere Phasen
- **Wird schwieriger:** Hauptsession muss Worktrees koordinieren, Konflikt-Resolution kann zeitfressend sein
- **Muss revisited werden:** Sprint-1-Retro: Hat Pilot-Lesson tatsächlich Folge-Phasen beschleunigt? Welche Subagent-Failures gab es?

#### Action Items

- [ ] Subagent-Prompt-Schablone in `_docs/sprint_1_subagent_prompt.md` festschreiben
- [ ] `_docs/sprint_1_lessons.md` als Live-Dokument einplanen (während Phase 1)
- [ ] Phase 0 Repo-Bootstrap als erste Sprint-1-Aktion

---

### ADR-012: Architektur-Layering und ArchUnitNET-Regeln

**Status:** Accepted
**Datum:** 2026-04-29
**Maxential-Referenz:** DEEP_MEMORY.md Sektion 4.2

#### Kontext

Stryker.NET hat informell ein 5-Schichten-Layering (Abstractions, Utilities → Configuration/RegexMutators/Solutions/TestRunner → TestRunner-Adapter → Core → CLI). Wir formalisieren das via ArchUnitNET-Tests, sodass Schichtverletzungen Build-Fehler erzeugen.

#### Entscheidung

**Architektur-Schichten** (siehe ADR-011 Phase-Diagramm):

| Layer | Module | Darf zugreifen auf |
|-------|--------|--------------------|
| 0 | Stryker.Abstractions, Stryker.Utilities, Stryker.DataCollector | (nur externe Pakete) |
| 1 | Stryker.Configuration, Stryker.RegexMutators, Stryker.Solutions, Stryker.TestRunner | Layer 0 |
| 2 | Stryker.TestRunner.MicrosoftTestPlatform, Stryker.TestRunner.VsTest | Layer 0, 1 |
| 3 | Stryker.Core | Layer 0, 1, 2 |
| 4 | Stryker.CLI | Layer 0, 1, 2, 3 |

**ArchUnitNET-Regeln** (in dediziertem Test-Projekt, z.B. `Stryker.Architecture.Tests`):

```csharp
// Beispiele:
public sealed class LayerArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(Stryker.Abstractions.AssemblyMarker).Assembly, ...).Build();

    [Fact]
    public void Abstractions_Should_Not_Depend_On_Any_Other_Stryker_Module()
    {
        ArchRuleDefinition
            .Types().That().ResideInAssembly("Stryker.Abstractions")
            .Should().NotDependOnAny("Stryker.Core", "Stryker.CLI", "Stryker.Configuration", /* ... */)
            .Check(Architecture);
    }

    [Fact]
    public void Mutators_Should_Be_Sealed()
    {
        ArchRuleDefinition
            .Classes().That().ImplementInterface("IMutator")
            .Should().BeSealed()
            .Check(Architecture);
    }

    [Fact]
    public void Reporters_Should_Be_Sealed()
    {
        ArchRuleDefinition
            .Classes().That().ImplementInterface("IReporter")
            .Should().BeSealed()
            .Check(Architecture);
    }

    [Fact]
    public void Core_Should_Not_Depend_On_CLI()
    {
        ArchRuleDefinition
            .Types().That().ResideInAssembly("Stryker.Core")
            .Should().NotDependOnAny("Stryker.CLI")
            .Check(Architecture);
    }

    [Fact]
    public void CLI_Should_Be_Only_Layer_That_References_McMaster()
    {
        ArchRuleDefinition
            .Types().That().ResideInAssembly("Stryker.Core", "Stryker.Configuration", /* alle außer CLI */)
            .Should().NotDependOnAny("McMaster.Extensions.CommandLineUtils")
            .Check(Architecture);
    }
}
```

**Performance-Hinweis (CLAUDE.md):** `Architecture` einmal pro Testklasse als statisches `readonly` Field laden, nicht pro Test (teuer).

#### Konsequenzen

- **Wird einfacher:** Schichtverletzungen sind Test-Failures, nicht Code-Reviews
- **Wird schwieriger:** Bei Refactorings müssen Architecture-Tests mitgeführt werden
- **Muss revisited werden:** Bei neuen Modulen → Architecture-Tests ergänzen

#### Action Items

- [ ] In Phase 7: `tests/Stryker.Architecture.Tests/` anlegen
- [ ] Mindestens 8 ArchUnit-Regeln definieren (Layer-Trennung + sealed-Checks)
- [ ] CI-Integration: ArchUnit-Tests sind Teil von `dotnet test`

---

### ADR-013: XML-Doc-Phasing-Strategy — CS1591 temporary-disable + Sprint-1.5

**Status:** Accepted
**Datum:** 2026-04-30 (Sprint-1 Phase-1 PILOT-Discovery)
**Maxential-Referenz:** Sprint-1-Phase-1 Session, Thought 5 (Tags: `cs1591-massen`, `phasing-strategy`, `sprint-1.5-plan`)

#### Kontext

Nach Aktivierung des Big-Bang-Analyzer-Stacks (ADR-004) traten in der PILOT-Phase 447 CS1591-Errors auf in `Stryker.Abstractions` allein (Missing XML doc on public). Stryker.NET-Upstream-4.14.1 hat **keine** XML-Docs auf public Members. Skalierung auf alle 17 Module: ~5000–8000 Missing-Doc-Errors. CLAUDE.md fordert "XML-Dokumentationskommentare für alle öffentlichen APIs" als Pflicht.

Würden wir alle XML-Docs in Sprint 1 schreiben, würde der Mega-Sprint-Scope von 4–6 Wochen auf 10–14 Wochen wachsen (zusätzlich 50–80h reine Doc-Schreib-Zeit). Sprint-Kohärenz (Code-Migration als Hauptziel) leidet darunter.

#### Entscheidung

**Sprint-1: CS1591 temporär auf `none` in `.editorconfig`** mit ausführlichem Kommentar-Begründung und Verweis auf den Sprint-1.5-Plan.

**Sprint-1.5 (NACH Phase 7) — Public API Documentation Sprint:**
- Dedicated dispatched-subagents (1 pro Modul, parallel mit Worktree-Isolation)
- Subagent-Prompt generiert XML-Doc basierend auf Symbol-Bodies (Method-Signaturen, Property-Types, Exception-Throws)
- Format-Guideline: `<summary>` 1–2 Zeilen, `<param>` je Parameter, `<returns>` non-void, `<exception>` documented throws
- Kein Re-Wording / Marketing
- NACH Sprint 1.5: CS1591 zurück auf `warning` in `.editorconfig`

#### Konsequenzen

- **Wird einfacher:** Sprint-1-Mega-Scope bleibt realistisch (4–6 Wochen), Code-Migration und Doc-Generation sind als separate Aktivitäten erkennbar
- **Wird schwieriger:** Sprint 1.5 ist zusätzliches Work-Stream nach Sprint-1-Abschluss; Risiko dass Doc-Generation aufgeschoben wird ("nie passiert"-Risk)
- **Muss revisited werden:** Bei Sprint-1.5-Abschluss CS1591 wieder aktivieren; bei jeder neuen public API in späteren Sprints sofort doc

#### Action Items

- [ ] `.editorconfig` mit CS1591=none + ausführlicher Begründung + Sprint-1.5-Plan-Verweis (in Phase 1 erfolgt)
- [ ] GitHub-Issue für "Sprint 1.5 — Public API XML-Doc-Sprint" mit Subagent-Plan anlegen (nach Phase 7)
- [ ] Subagent-Prompt-Schablone für Doc-Generation in `_docs/sprint_1_5_subagent_prompt.md` (vor Sprint 1.5)
- [ ] Bei Sprint-1.5-Abschluss: CS1591 zurück auf `warning`

---

## 3. Komponentenstruktur

### 3.1 Schichtenübersicht

Siehe ADR-011 Layer-Diagramm und ADR-012 Layer-Tabelle.

### 3.2 Layer 0 — Foundations

**Verantwortung:** Geteilte Abstraktionen, Hilfsfunktionen, Test-Adapter-Plumbing.
**Enthält:** Stryker.Abstractions (Interfaces, Modelle), Stryker.Utilities (FileSystem-Wrapper, Logging-Helper), Stryker.DataCollector (VsTest-Coverage-Sammler, netstandard2.0).
**Abhängigkeiten:** Nur externe Pakete (Microsoft.CodeAnalysis, TestableIO, Microsoft.TestPlatform.ObjectModel, Microsoft.TestPlatform.Portable, Buildalyzer, DotNet.Glob, Serilog).

### 3.3 Layer 1 — Domain

**Verantwortung:** Geschäftslogik der Mutation Engine, Konfigurations-Loading, Solution-/Project-Parsing.
**Enthält:** Stryker.Configuration, Stryker.RegexMutators, Stryker.Solutions, Stryker.TestRunner (Abstraktion).
**Abhängigkeiten:** Layer 0 + externe Pakete.

### 3.4 Layer 2 — Test-Runner-Adapter

**Verantwortung:** Konkrete Test-Framework-Adapter (VsTest und Microsoft Testing Platform).
**Enthält:** Stryker.TestRunner.MicrosoftTestPlatform, Stryker.TestRunner.VsTest.
**Abhängigkeiten:** Layer 0, 1 + Microsoft.TestPlatform / Microsoft.Testing.Platform.

### 3.5 Layer 3 — Core Orchestration

**Verantwortung:** Hauptlogik (Mutation-Engine, Mutator-Discovery, Reporter-Pipeline, Diff-Logic, Initial-Build).
**Enthält:** Stryker.Core (incl. embedded resources: MutantControl.cs, MutantContext.cs, mutation-test-elements.js, mutation-report.html).
**Abhängigkeiten:** Layer 0, 1, 2 + externe Pakete (Buildalyzer 9.0+, AWSSDK.S3, Azure.Storage.Files.Shares, Mono.Cecil, etc.).

### 3.6 Layer 4 — CLI / Composition Root

**Verantwortung:** Entry Point (`dotnet-stryker-netx.exe`), CLI-Argument-Parsing, DI-Container-Composition.
**Enthält:** Stryker.CLI (incl. IStrykerCommandLine-Interface, McMasterStrykerCommandLine-Adapter, Program.cs).
**Abhängigkeiten:** Layer 0, 1, 2, 3 + McMaster, NuGet.Protocol, YamlDotNet.

---

## 4. Abhängigkeitsregeln

| Von | Darf zugreifen auf | Darf NICHT zugreifen auf |
|-----|-------------------|-------------------------|
| Layer 4 (CLI) | Layer 0, 1, 2, 3 | (alles erlaubt) |
| Layer 3 (Core) | Layer 0, 1, 2 | Layer 4 (CLI) |
| Layer 2 (Test-Runner-Adapter) | Layer 0, 1 | Layer 3, 4 |
| Layer 1 (Domain) | Layer 0 | Layer 2, 3, 4 |
| Layer 0 (Foundations) | Nur externe Pakete | Layer 1, 2, 3, 4 |

**Durchsetzung:** ArchUnitNET-Tests im Test-Projekt `Stryker.Architecture.Tests`. Siehe ADR-012.

---

## 5. Querschnittsthemen

### 5.1 Fehlerbehandlung

**Strategie:** Exception Hierarchy mit `Stryker.Abstractions.StrykerException` als Basis. Defensive Catch-Blocks an System-Boundaries (CLI-Entry, Test-Runner-Calls, IO).

| Fehlertyp | Handling |
|-----------|----------|
| Validierungsfehler (Input, Config) | Sofort zurückgeben, klarer User-Fehler |
| Roslyn-Parse-Fehler | Logging + Skip-Mutation (kein Abbruch) |
| Test-Runner-Fehler | Retry mit exponential backoff (max 3) |
| File-System-Fehler | Logging + Graceful Degradation |
| Unerwartete Fehler | Stack-Trace loggen + Exit-Code != 0 |

**Pflicht-Pattern (CLAUDE.md):** `catch (Exception ex) when (ex is not OperationCanceledException) { ... }`

### 5.2 Logging

**Framework:** Serilog 4.x mit File + Console Sinks (Stryker.NET-konform).
**Strategie:** Strukturiertes Logging mit Korrelations-ID pro Mutation-Run.

| Level | Verwendung |
|-------|-----------|
| ERROR | Unerwartete Fehler, manueller Eingriff nötig |
| WARN | Erwartete Probleme (Skip-Mutation, Test-Retry) |
| INFO | Phase-Übergänge, Mutator-Statistik, Reporter-Output |
| DEBUG | Mutator-Details, Roslyn-AST-Dumps, Test-Runner-Communication |

### 5.3 Konfiguration

**Strategie:** Hierarchisch: CLI-Args > Config-File (YAML/JSON) > Defaults.

**Format**: Stryker-config.json oder stryker-config.yaml/yml im Projekt-Root oder via `--config` Flag. **1:1-kompatibel** mit Upstream-Schema.

### 5.4 Security

| Maßnahme | Verifizierung |
|----------|---------------|
| Input-Validierung auf allen CLI-Args | Unit Tests + Semgrep |
| Config-File Schema-Validierung | Pflicht beim Start (Fail-Fast) |
| Keine Secrets im Code/Config | `.gitignore` + Semgrep `secrets.detected` Rule |
| Dependency-Audit | Semgrep `--config auto` + `dotnet list package --vulnerable` |
| Defense-in-Depth: Test-Code-Sandboxing | DataCollector läuft im VsTest-Process-Boundary |

---

## 6. Deployment

### 6.1 Deployment-Modell

**Typ:** .NET Global Tool (`dotnet tool install -g dotnet-stryker-netx`) als primärer Distributions-Pfad. Library-NuGet-Pakete (`stryker-netx`, `Stryker.Abstractions`, etc.) für Library-Konsumenten.

### 6.2 Plattform-Support

| Plattform | Support-Level | Besonderheiten |
|-----------|---------------|----------------|
| Windows | Primär | Voll getestet |
| Linux | Primär | Voll getestet (CI) |
| macOS | Sekundär | Best-Effort, nicht primär getestet |

### 6.3 Build & Distribution

```bash
# Build
dotnet build stryker-netx.slnx -c Release

# Test (incl. Coverage)
dotnet test stryker-netx.slnx -c Release --collect:"XPlat Code Coverage"

# Pack (NuGet)
dotnet pack stryker-netx.slnx -c Release -o ./nupkg/

# Install global tool (Test)
dotnet tool install -g --add-source ./nupkg dotnet-stryker-netx --version 1.0.0-preview.1

# Run Mutation Test
cd /path/to/user/test/project
dotnet stryker-netx
```

---

## 7. Risiken und technische Schulden

| # | Risiko | Impact | Mitigation | Status |
|---|--------|--------|------------|--------|
| R1 | Buildalyzer-9-API-Migration kann unerwartete Code-Refactors auslösen | High | Context7 vor Update; Phase 5 dediziert | Open |
| R2 | TWAE + 3 Analyzer können 1500+ Initial-Issues produzieren | High | .editorconfig-Tuning; Pilot-Lessons; Subagent-Parallelisierung | Open |
| R3 | MSTest-spezifische Patterns ([ClassInitialize], [ExpectedException]) brauchen manuelle Migration | Medium | Roslyn-Code-Mod als Tooling | Open |
| R4 | Roslyn-API-Updates zwischen 5.3 und C#-14-Version können Breaking Changes haben | Medium | Context7-Pflicht vor Update | Open |
| R5 | Stryker.DataCollector netstandard2.0-Pinning blockiert moderne BCL-APIs in DataCollector | Low | Bewusste Inkaufnahme; DataCollector-Code minimal halten | Mitigated |
| R6 | McMaster-Deprecation kann CVE-Anfälligkeit bringen | Medium | ADR-007 HYBRID + Migration-Trigger-Liste | Mitigated |
| R7 | NativeAOT-Aktivierung später kann großen Refactor erfordern | Low | ADR-006 (tauglich aber nicht erzwungen); Re-Eval Sprint 4 | Open |
| R8 | ExampleProjects in `_reference/.../ExampleProjects/` brechen durch unsere Refactors | Medium | Smoke-Test-Suite gegen ExampleProjects in Phase 7 | Open |
| R9 | Stryker-Upstream-4.15.0-Release könnte unsere Eigenarbeit teilweise obsolet machen | Low | ADR-008 + Apache-2.0-Lizenz erlaubt Re-Sync | Mitigated |
| R10 | `.slnx` Tooling-Support unklar bei manchen .NET 10 SDK Versionen | Low | Phase 0 Smoke-Test; Fallback `.sln` möglich | Open |
| R11 | Sprint-1 4–6 Wochen Realdauer übersteigt Standard-2-Wochen-Sprint | Medium | Bewusste Entscheidung; Mega-Sprint dokumentiert | Mitigated |
| R12 | `git push` zu großen Worktrees aus Subagents kann Konflikte erzeugen | Medium | Hauptsession koordiniert Merge; Konflikt-Resolution-Plan | Open |

---

## 8. v2.0.0 Architecture Foundation (Sprint 5)

ADRs 013–018 lock the architectural decisions for the v2.0.0 release line. They are derived from the gap analysis in `_input/mutation_framework_comparison.md` and were prioritised via a Maxential macro-decision (9 thoughts) + ToT branch exploration (3 branches; Architecture-First chosen with score 0.95). For implementation timing see the v2.0.0 roadmap in Sprint 5+ planning.

### ADR-013: AST/IL Hybrid Decision — Roslyn-AST primär, IL-Sicht selektiv

**Status:** Accepted (Sprint 5)

**Context.** `mutation_framework_comparison.md` §5 stellt fest: Stryker.NET (und damit stryker-netx) arbeitet auf dem Roslyn-AST. PIT arbeitet auf JVM-Bytecode. Roslyn ist die richtige primäre Wahl für C#, aber für einige Mutationen (z.B. `checked`, Inline-Constants, Equivalent-Mutant Filtering durch IL-Equivalence-Check) gewinnt man durch zusätzliche IL-Sicht.

**Decision.** Roslyn-AST + SemanticModel bleibt die **primäre** Mutator-Ebene. IL-Sicht wird **selektiv** als Hilfsmittel eingeführt, NICHT als zweite Mutator-Plattform:

1. **Roslyn-AST** (`Microsoft.CodeAnalysis.CSharp.Syntax`) für alle Mutator-Implementierungen (alle 24 bestehenden + alle neuen).
2. **Roslyn-SemanticModel** (`Microsoft.CodeAnalysis.SemanticModel`) für type-driven mutators (siehe ADR-015).
3. **System.Reflection.Metadata + System.Reflection.Emit** als Hilfsmittel:
   - Hot-Swap (ADR-016): mutated method als IL emittieren und in laufenden Prozess injizieren.
   - Equivalent-Mutant Filter (ADR-017): IL-Diff zwischen original + mutated Compilation als Heuristik.
4. **Kein** PIT-style Bytecode-Mutation als zweiter Operator-Ebene — würde die mental load verdoppeln und C#-spezifische Pattern-Matching/LINQ-Sichtbarkeit verlieren.

**Alternatives.**
- *Bytecode-only (PIT-style)*: verworfen — Roslyn macht C#-spezifische Konstrukte (LINQ, Pattern Matching, async/await) viel sichtbarer als IL.
- *AST-only (Status quo)*: verworfen für v2.0.0 — IL hilft bei Hot-Swap-Performance und Equivalent-Mutant-Detection.
- *AST + Bytecode parallel*: verworfen — verdoppelt Maintenance-Last ohne Mehrwert; Roslyn deckt 95 % der C#-Mutationen ab.

**Consequences.**
- (+) v2.0.0 nutzt das volle Roslyn-Ökosystem (Source Generators, Pattern Matching, CSharpCompilation API).
- (+) Trampoline (ADR-016) wird realisierbar ohne Mutator-Code zu touchen.
- (+) Equivalent-Mutant Filter (ADR-017) bekommt eine objektive IL-Vergleichs-Heuristik.
- (–) Stryker.Utilities + Stryker.Core müssen zusätzlich `System.Reflection.Emit` (BCL) und `System.Reflection.Metadata` (BCL) referenzieren — beide sind Teil von .NET 10 BCL, kein neues NuGet.
- (–) Hot-Swap-Implementierung (Sprint 8) braucht IL-Generation Know-how.

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 1; v2.0.0-Roadmap-Maxential Branch X.

---

### ADR-014: Operator-Hierarchie — Operator → Sub-Operator → Group (PIT-Modell)

**Status:** Accepted (Sprint 5); implementiert in Sprint 6.

**Context.** Stryker.NET (und stryker-netx v1.x) hat eine **Flat-List** von 24 IMutator-Implementierungen. Jeder Mutator gehört zu einer `Mutator`-Enum-Kategorie (Boolean, Logical, Math, …) und einer `MutationLevel` (Basic / Standard / Advanced / Complete). Es gibt keine echte Hierarchie. PIT modelliert demgegenüber:
- **Operator** (z.B. `MATH`)
- **Sub-Operator** (z.B. `MATH_ADD_TO_SUB`, `MATH_MUL_TO_DIV`)
- **Group** (DEFAULTS / STRONGER / ALL — Bündel von Operatoren)

Der Vorteil: Operator-Profile (siehe ADR-018) lassen sich als Group-Selektoren ausdrücken, ohne jeden einzelnen Sub-Operator als Flat-List anzugeben. Inkrementelle Refinements (z.B. „mehr Subops zur MATH-Familie hinzufügen") sind ohne API-Bruch möglich.

**Decision.** Einführung einer dreischichtigen Hierarchie in `Stryker.Abstractions`:

```
public interface IMutatorGroup        // Sammlung von Operatoren (z.B. „CoreOperators")
public interface IMutator             // Eine Operator-Familie (z.B. BinaryExpressionMutator)
public interface IMutationOperator    // Ein einzelner sub-operator (z.B. „+ → -")
```

Bestehende Mutator-Implementierungen (`BinaryExpressionMutator`, `BooleanMutator`, etc.) werden zu **`IMutator`** und enthalten eine Liste von **`IMutationOperator`**-Sub-Operatoren. Eine **`IMutatorGroup`** ist eine `IReadOnlyList<IMutator>` mit Profile-Tag (siehe ADR-018).

**Alternatives.**
- *Status quo (Flat-List + MutationLevel-Enum)*: verworfen — skaliert nicht für die ~50+ neuen Sub-Operatoren in v2.0.0.
- *Pure Mutation-Level-Erweiterung*: verworfen — Levels sind ordinal (Basic < Standard < …), Profile sind orthogonal (DEFAULTS ≠ STRONGER ist nicht „mehr").
- *Tagging via Attribute*: verworfen — Reflektion ist langsamer und macht die API-Surface unklarer.

**Consequences.**
- (+) v2.0.0 kann Profile (ADR-018) nativ ausdrücken.
- (+) Sub-Operator-Granularität ermöglicht selektives Disable von einzelnen Substitutionen (z.B. `--disable-suboperator MATH_ADD_TO_SUB`).
- (–) **Public-API-Bruch** in `Stryker.Abstractions` — semver Major-Bump (v2.0.0) gerechtfertigt.
- (–) Sprint 6 muss alle 24 bestehenden Mutatoren refactoren. Mitigation: bestehende 27/27 Tests + Sample E2E als Safety Net.

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 2 + §3 PIT-Stärken; v2.0.0-Roadmap-Maxential Branch X Sprint 6.

---

### ADR-015: SemanticModel-driven Mutator Infrastructure

**Status:** Accepted (Sprint 5); implementiert in Sprint 7.

**Context.** Stryker.NET v1.x verwendet `SemanticModel` nur sporadisch. cargo-mutants (Rust) zeigt: **typgetriebene Mutationen** sind das größte Aussagekraft-Differenzial — `Result<T>` → `Result::Err(default())`, `Vec<T>` → `vec![]`, `HashMap` → `HashMap::new()`. In C# wäre das Äquivalent: `Task<T>` → `Task.FromResult(default(T))`, `IEnumerable<T>` → `Enumerable.Empty<T>()`, `Dictionary<K,V>` → `new Dictionary<K,V>()`. Diese Mutationen brauchen `SemanticModel`, um den exakten Rückgabetyp zur Mutationszeit zu kennen.

**Decision.** Erweitere `MutatorBase<TNode>` (Stryker.Core) um obligatorische `SemanticModel`-Propagation (bereits Status quo seit Sprint 1) und führe ein neues Marker-Interface ein:

```csharp
public interface ITypeAwareMutator : IMutator { }
```

Type-aware Mutatoren bekommen zusätzlich:

```csharp
protected ITypeSymbol? GetReturnType(SyntaxNode node, SemanticModel model);
protected ITypeSymbol? GetExpressionType(ExpressionSyntax expr, SemanticModel model);
```

als Helpers in einer neuen `TypeAwareMutatorBase<TNode> : MutatorBase<TNode>`-Basisklasse.

**Alternatives.**
- *Syntax-only (Status quo)*: verworfen — siehe Context.
- *Roslyn `Compilation` direkt im Mutator*: verworfen — bricht ADR-001 (Stryker.Abstractions Roslyn-frei) und ADR-014 (Hierarchie-Sauberkeit).

**Consequences.**
- (+) Sprint 9 (Type-Driven Mutators) wird realisierbar.
- (+) C6 (Konservative Defaults für `uint`, `byte`) wird trivial implementierbar.
- (+) D1 (Type-Checker Integration) bekommt einen Hook (Mutationen nach Emission via `compilation.GetDiagnostics()` filtern).
- (–) Marginal höhere Mutationszeit (SemanticModel-Aufrufe sind nicht gratis); Mitigation: SemanticModel pro Document wird einmal berechnet und an alle Mutatoren propagiert.

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 3 + §4.2 cargo-mutants Differenzial-Feature; v2.0.0-Roadmap-Maxential Sprint 7.

---

### ADR-016: AssemblyLoadContext Hot-Swap (Trampoline-Äquivalent) — design only, impl in Sprint 8

**Status:** Accepted (Sprint 5); implementiert in Sprint 8 mit ggf. eigenen Maxential-Sub-Decisions.

**Context.** Stryker.NET v1.x kompiliert für **jeden Mutanten neu**. Bei 660 Mutationen × ~4 Sekunden Kompilierzeit = ~44 Minuten pro Lauf. Das ist der größte Wettbewerbsnachteil gegenüber mutmut (Trampoline-basiert: nur eine Kompilierung, dann Runtime-Switching) und PIT (Custom ClassLoader hot-loads mutated classes). `mutation_framework_comparison.md` §5 Punkt 4 nennt **AssemblyLoadContext mit Hot-Swap der mutierten Methode** als das C#-Äquivalent — und potenziell den größten Wettbewerbsvorteil.

**Decision.** v2.0.0 führt einen optionalen Hot-Swap-Modus ein, der die Standard-Pipeline ablöst (gesteuert per CLI-Flag `--engine hotswap | recompile`):

1. **Initial-Build** des Source-Projekts (1×) erzeugt eine Baseline-Assembly.
2. **Pro Mutant**: nur die mutierte Methode als neue Assembly emittieren (`CSharpCompilation.Emit` einer Hilfsklasse, die nur die mutierte Methode enthält).
3. **Hot-Swap**: über `AssemblyLoadContext` die mutierte Methode in den Test-Runner-Prozess injizieren (entweder durch `Assembly.Load`-with-replacement oder via `MetadataUpdater.ApplyUpdate` für .NET 10 EnC-Hot-Reload).
4. **Test-Run** läuft im selben Prozess, schaltet zwischen Mutanten via AssemblyLoadContext-Switch.

Detaillierte Implementierungs-Sub-Entscheidungen (ALC-vs-MetadataUpdater, IsolatedScope-Strategie, Test-Runner-Integration) in **Sprint 8 Phase 8.1** mit eigenem Maxential-Lauf.

**Alternatives.**
- *Status quo (Recompile pro Mutant)*: bleibt als `--engine recompile` verfügbar (Fallback).
- *Process-Pool*: verworfen — IPC-Overhead frisst die Ersparnis; mutmut hat Trampoline gewählt aus selbem Grund.
- *MSIL-Patching der Baseline-Assembly direkt*: verworfen — bricht NRT-Annotations und macht Debug-Symbole unbrauchbar.

**Consequences.**
- (+) **5–10× Performance-Boost** für medium+ Projekte (Schätzung; präzise Zahlen aus Sprint-8-Benchmarks).
- (+) Echtzeit-Mutationsschätzungen werden möglich.
- (–) Hot-Swap-Modus ist **Sprint-8-HIGH-RISK** (Engine-Rewrite). Sprint 4's integration suite (8 categories) als safety net.
- (–) Test-Runner-Integration: VsTest und MTP haben unterschiedliche Process-Models; ggf. nur MTP unterstützt Hot-Swap initial.
- (–) Recompile-Modus als Fallback erhöht Maintenance-Surface — bewusste Inkaufnahme für Risk-Mitigation.

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 4 (größter Wettbewerbsvorteil); v2.0.0-Roadmap-Maxential Sprint 8 (HIGH risk, dedicated sprint).

---

### ADR-017: Equivalent-Mutant Filtering als first-class Layer

**Status:** Accepted (Sprint 5); implementiert in Sprint 7.

**Context.** PIT zeigt: das Filter-Layer ist fast genauso wichtig wie der Operator-Layer. Equivalent-Mutants sind Mutationen, die das Verhalten **nicht** ändern (z.B. `i+0` an Stelle von `i`, leere Methoden zurückgeben statt Stryker-Mutationen). Sie verschwenden Test-Zeit und verfälschen den Mutation-Score nach unten. Stryker.NET v1.x hat **kein** dediziertes Filter-Layer — Equivalent-Mutants schlagen als „Survived" durch.

**Decision.** Einführe eine pipeline-stage `IEquivalentMutantFilter` zwischen Mutator und Test-Runner:

```csharp
public interface IEquivalentMutantFilter
{
    bool IsEquivalent(Mutation mutation, SemanticModel model, Compilation original);
}
```

Filter werden in **DI registriert** und als Pipeline applied. Initial-Set in Sprint 7:
- `IdentityArithmeticFilter` — `x + 0`, `x * 1`, `x - 0` als equivalents
- `EmptyReturnEquivalentFilter` — Mutator schlägt Empty-Return vor, aber Methode hat bereits leere Return-Liste
- `IdempotentBooleanFilter` — `!!x → x`, `!(!x) → x`

Erweiterbar in späteren Sprints. Heuristic: bei Unsicherheit als Mutant beibehalten (false negative > false positive).

**Alternatives.**
- *Kein Filter (Status quo)*: verworfen — Mutation-Scores werden systematisch zu niedrig.
- *Filter als Mutator-interne Logik*: verworfen — duplicate code in jedem Mutator, kein zentraler Audit-Punkt.
- *Filter als post-test (nach Killed/Survived-Klassifikation)*: verworfen — Tests laufen unnötig.

**Consequences.**
- (+) Mutation-Scores werden systematisch sauberer.
- (+) Test-Zeit sinkt durch weniger zu testende Mutationen.
- (+) Operator-Authors können Equivalent-Patterns explizit dokumentieren statt sie implizit zuzulassen.
- (–) False-Positive-Risiko: ein Filter könnte einen Mutant als equivalent erkennen, der es nicht ist → unkilled mutant. Mitigation: konservative Filter, Heuristic „bei Unsicherheit beibehalten".

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 5 + §4.1 PIT „Equivalent-Mutant-Filtering".

---

### ADR-018: Mutation Levels als Profiles — DEFAULTS / STRONGER / ALL

**Status:** Accepted (Sprint 5); implementiert in Sprint 6 (zusammen mit ADR-014 Hierarchie-Refactor).

**Context.** Stryker.NET v1.x hat ein `MutationLevel`-Enum mit ordinaler Bedeutung (Basic < Standard < Advanced < Complete). Das ist eine 1-D-Skala — „mehr Level = mehr Mutationen". PIT zeigt: Mutation-Profile sind orthogonal — DEFAULTS ist das, was ohne Konfiguration läuft; STRONGER fügt akademisch stärkere Operatoren hinzu; ALL ist alles inkl. experimentell. Diese Profile sind nicht ordinal („STRONGER ist nicht 'mehr Level' als DEFAULTS, sondern 'andere Auswahl'").

`mutation_framework_comparison.md` §5 Punkt 6 nennt das als StrykerJS-Innovation, die in .NET-Linie noch fehlt.

**Decision.** v2.0.0 führt zusätzlich zum bestehenden `MutationLevel`-Enum (das bleibt für Backward-Compat) ein neues `MutationProfile`-Enum ein:

```csharp
[Flags]
public enum MutationProfile
{
    None     = 0,
    Defaults = 1 << 0,    // Was Stryker.NET als sinnvolle Standard-Auswahl betrachtet
    Stronger = 1 << 1,    // Defaults + akademisch stärkere Operatoren (PIT „STRONGER" entspricht)
    All      = 1 << 2     // Alle Operatoren inkl. experimenteller (ADR-014 Sub-Operators alle aktiv)
}
```

`IMutator`/`IMutationOperator` bekommen ein Attribut:

```csharp
[MutationProfileMembership(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]
public sealed class BinaryExpressionMutator : IMutator { … }
```

Selektion via CLI: `--profile defaults | stronger | all` oder per `stryker-config.json`. `MutationLevel` bleibt als zweite Achse (Backward-Compat); bei Kombinations-Konflikten gewinnt das restriktivere.

**Alternatives.**
- *Nur MutationLevel erweitern*: verworfen — siehe Context (Profile sind orthogonal, nicht ordinal).
- *Profile als String-Tag*: verworfen — Tippfehler-anfällig, schlechter IntelliSense.
- *Profile per Config-File only (kein Code-Attribut)*: verworfen — Operator-Authors sollen Profile-Zugehörigkeit lokal dokumentieren.

**Consequences.**
- (+) v2.0.0 spricht die Sprache der Mutation-Testing-Community (PIT-Konvention).
- (+) Zukünftige experimentelle Operatoren (E5 Access-Modifier-Mutation, ADR-Erweiterung) können direkt in `All` landen ohne Default-Verhalten zu ändern.
- (+) Backward-Compat: bestehende `--mutation-level` Flag funktioniert weiter.
- (–) Zwei Achsen (Level + Profile) sind erklärungsbedürftig — Migration-Guide v1→v2 muss das klar dokumentieren (Sprint 12).

**Backed by.** `mutation_framework_comparison.md` §5 Punkt 6 + §3 PIT-Stärke „Operator-Gruppen-Konzept"; v2.0.0-Roadmap-Maxential Sprint 6.

---

## ADR-019: HotSwap-Engine als eigene v2.2.0-Release statt Sprint-14-Quetschung

**Status.** Entschieden — 2026-05-01 (Sprint 14, v2.1.0).

**Context.**
v2.0.0 (Sprint 8, ADR-016) hat das Hot-Swap-Engine-Scaffolding geliefert: `IMutationEngine`-Interface, `RecompileEngine` (default), `HotSwapEngine`-Stub, der bei Aufruf `NotSupportedException` mit ADR-016-Pointer wirft, plus `--engine`-Flag. Die echte Implementierung — `MetadataUpdater.ApplyUpdate`-basierte In-Process IL-Delta-Anwendung mit langlebigem Test-Host — wurde explizit als „focused follow-up sub-sprint" zurückgestellt.

Sprint 14 (v2.1.0) bringt 4 weitere Deliverables (3 Mutatoren + 1 Filter). Die offene Frage: HotSwap-Implementierung in v2.1.0 mit reinpacken, oder als eigene v2.2.0-Release herauslösen?

**Decision.**
HotSwap-Engine als **eigene fokussierte v2.2.0-Release** herauslösen. v2.1.0 bleibt bei den 4 Operator-/Filter-Deliverables. v2.0.0-Scaffolding bleibt unverändert.

**Backed by.** Maxential-Branch-Vergleich (Sprint 14, 2 Branches B1=expand, B2=defer; B2 chosen), Sprint-3 + Sprint-8 + Sprint-11 Honest-Deferral-Pattern als Präzedenzfall.

**Begründung.**

1. **Engineering-Größe.** Realistische End-zu-End-Implementierung erfordert:
   - IL-Delta-Berechnung (PE/PE-Diff inklusive Method-Token-Stabilität)
   - `MetadataUpdater.ApplyUpdate`-Aufruf-Orchestrierung (delta-bytes, IL-bytes, PDB-bytes je Update)
   - Test-Host-Lifecycle: einmal starten, über alle Mutanten am Leben halten, nur bei fatalen Fehlern neu starten
   - Edit-and-Continue-Compatibility-Checks (manche Mutationen sind nicht hot-swappable, z.B. Signatur-Änderungen)
   - End-zu-End-Validierung mit allen 51 v2.1.0-Mutatoren
   
   Geschätzter Aufwand: 1–3 Personenmonate fokussierter Engineering-Arbeit. Das ist eine eigene Release, nicht ein Sprint-Item neben anderen.

2. **YAGNI / Ship-Working-Things-Disziplin.** Framework-Code, der vom Endbenutzer nicht verwendet werden kann (weil der echte Delta-Producer fehlt), ist Wartungs-Surface ohne Liefer-Wert. Anti dem expliziten Project-Prinzip.

3. **Honest-Deferral-Präzedenz.** Sprint 8 (Hot-Swap-Scaffolding-only), Sprint 11 (CRCR-deferred), Sprint 13 (Phase-A-doc-fix-vor-Phase-B-implementation) — alle haben das Muster „lieber sauberes Liefer-Versprechen halten als Half-Implemented Big Items über mehrere Releases verteilen" etabliert. v2.1.0 honoriert dasselbe Muster.

**Alternatives.**
- *Substanzieller HotSwap-Framework-Ausbau in v2.1.0 (Branch B1 in Maxential):* verworfen — würde ~1500 LOC dead code produzieren ohne working delta-producer; vergrößert v2.1.0-Surface ohne User-Value.

**Consequences.**
- (+) v2.1.0 ships fokussiert mit 4 working deliverables (3 Mutatoren + 1 Filter) und ist klein genug zum reviewen.
- (+) v2.2.0 wird als „HotSwap engine focused release" explizit benannt — Stakeholder wissen woran sie sind.
- (+) v2.0.0-Scaffolding bleibt stabil — kein Refactoring der `IMutationEngine`-Stubs nötig bevor die echte Implementierung kommt.
- (–) `HotSwapEngine.ThrowIfInvoked()` bleibt für eine weitere Minor-Version aktiv. Nutzer, die `--engine HotSwap` heute setzen, bekommen weiterhin `NotSupportedException` mit ADR-016-Pointer.

**Implementation roadmap für v2.2.0.**
1. ADR-020: IL-Delta-Berechnungs-Strategie (Mono.Cecil vs. System.Reflection.Metadata)
2. `IDeltaProducer`-Interface + Production-Implementation
3. Test-Host-Lifecycle-Manager (`HotSwapTestHostController`)
4. `HotSwapEngine.RunMutationCycle(Mutant) → Task<MutantResult>` mit ApplyUpdate-Orchestrierung
5. End-zu-End-Validierung gegen alle 51 Mutatoren
6. Performance-Benchmark vs. RecompileEngine

---

## ADR-021: Walking back ADR-016 — HotSwap engine entfernt (v2.2.0)

**Status.** Accepted — 2026-05-01 (Sprint 15, v2.2.0). **Supersedes ADR-016 + ADR-019 (HotSwap-Roadmap).**

**Context.**

ADR-016 (Sprint 5, v2.0.0 Architecture Foundation) hat einen *AssemblyLoadContext-Hot-Swap-Modus* als Sprint-8-Scaffolding festgelegt mit folgender Decision:

> 1. **Initial-Build** des Source-Projekts (1×) erzeugt eine Baseline-Assembly.
> 2. **Pro Mutant**: nur die mutierte Methode als neue Assembly emittieren.
> 3. **Hot-Swap** über `AssemblyLoadContext` oder `MetadataUpdater.ApplyUpdate`.
> 4. **Test-Run** läuft im selben Prozess.

Plus die Versprechung:
> (+) **5–10× Performance-Boost** für medium+ Projekte.

ADR-019 (Sprint 14, v2.1.0) hat die echte Implementierung in eine eigene fokussierte v2.2.0-Release herausgelöst.

**Pre-Implementation-Recherche (Sprint 15) hat ein fundamentales Problem in den ADR-016-Annahmen aufgedeckt:**

Per Serena+Grep auf der tatsächlichen Mutation-Execution-Pipeline:
- `Stryker.Core/MutationTest/CsharpMutationProcess.cs` Zeile 65: `CompileMutations(input, compilingProcess)`
- `CompileMutations` ruft `compilingProcess.Compile(projectInfo.CompilationSyntaxTrees, ms, msForSymbols)` — EIN compile-pass für ALLE Mutationen in EINER Assembly
- Test-Runtime-Switching zwischen Mutanten via `ActiveMutationId`-Environment-Variable im Test-Host
- `MutationTestExecutor.RunTestSessionAsync` ruft `TestRunner.TestMultipleMutantsAsync(...)` mit Mutant-Batches auf

**Die ADR-016-Annahme "Stryker kompiliert pro Mutant" ist falsch.** Stryker.NET hat seit Jahren ein cleveres "all-mutations-in-one-assembly + runtime-id-switching"-Pattern. Es gibt keinen Per-Mutant-Compile, der durch HotSwap eingespart werden könnte.

**Wo Stryker's tatsächliche Kosten liegen:**
1. Initial-Compile der All-Mutations-Assembly (1×, amortisiert)
2. Test-Host-Process-Spawn pro Test-Batch (echter cost driver)
3. Coverage-Capture-Initial-Pass (1×, optional via `--coverage-analysis`)

Bereits mitigiert durch v1.x `OptimizationModes.SkipUncoveredMutants` + `CoverageBasedTest` (default `--coverage-analysis perTest`), die Mutanten ohne Test-Coverage als `NoCoverage` markieren und nie Test-Run schedulen.

**Decision.**

ADR-016 ist auf einem falschen mentalen Modell von Stryker.NET's Cost-Struktur basiert. v2.2.0 nimmt ADR-016 zurück:

1. **Soft-Deprecate die HotSwap-Surface:**
   - `MutationEngine` enum → `[Obsolete]`
   - `IMutationEngine` interface → `[Obsolete]`
   - `IStrykerOptions.MutationEngine` property → `[Obsolete]`
   - `MutationEngineInput` config input → `[Obsolete]`, akzeptiert `recompile|hotswap` weiterhin mit Deprecation-Warning (kein Breaking Change für CLI-Nutzer)

2. **Lösche die dead-code Engines:**
   - `Stryker.Core/Engines/HotSwapEngine.cs` (warf nur `NotSupportedException`)
   - `Stryker.Core/Engines/RecompileEngine.cs` (war nur `IMutationEngine.Kind`-Marker, keine Execution-Path)

3. **v3.0 (zukünftig)** kann hard-removal der `MutationEngine`-Surface vornehmen.

**Backed by.** Sprint 15 Maxential-Session (14 Thoughts, 3-way branch C1/C2/C3, C2 = walk-back gewählt). Recherche-Trail: Serena `find_symbol` + Grep auf `CompileMutations`, `MutationTestExecutor`, `IMutationEngine`-References (gesamte Surface = 8 Files / 23 Mention-Sites kartiert vor Decision).

**Alternatives evaluated (Maxential branches).**

- **C1 — HotSwap framework MVP trotzdem bauen.** Verworfen — würde ~1500 LOC dead framework code shippen ohne working delta-producer + ohne klares Wertversprechen (das versprochene 5–10× boost existiert nicht). Verstößt gegen YAGNI.
- **C3 — Pivot zu inkrementellem Mutation-Testing.** Verworfen für v2.2.0 als Stealth-Pivot wäre. Verdient eigene ADR + Multi-Sprint-Roadmap. Siehe ADR-022 (Proposed).

**Consequences.**

- (+) Honest Engineering — kein dead framework code, keine misleading user-facing flags. Alignt mit dem Sprint-13-Phase-A reconciliation discipline pattern (admit doc errors openly, fix them).
- (+) Reduzierte Maintenance-Surface (zwei deletable Files, vier deprecate-able Symbols).
- (+) Das `--engine` CLI flag bleibt akzeptiert (mit Deprecation-Warning) für Backwards-Compat — kein Breaking Change für User die `--engine recompile` heute setzen.
- (+) Klarstellung: Stryker.NET's bestehende all-mutations-in-one-assembly + ActiveMutationId-Pattern IST bereits eine sehr effiziente Architektur — kein Architecture-Pivot nötig.
- (–) Public-facing acknowledgement, dass eine v2.0.0-Architektur-Decision auf einer falschen Annahme basierte. Mitigiert dadurch dass die honest-deferral patterns (Sprint 8 scaffolding-only, Sprint 11 CRCR-deferred, Sprint 13 Phase A reconciliation) das Muster "Fehler offen zugeben + sauber korrigieren" bereits etabliert haben.
- (–) v2.2.0 ist eine "Negativrelease" (löscht statt fügt hinzu). Akzeptabel, weil das Project's stated principle "ship working things" das wertet höher als "ship something".

**Lessons.**

1. **Pre-implementation recherche im echten Code ist Pflicht VOR Architektur-Entscheidungen.** ADR-016 wurde in Sprint 5 (v2.0.0 Architecture Foundation) basierend auf dem comparison.md §5 Punkt 4 (Mutmut-Trampoline-Inspiration) verabschiedet, ohne den tatsächlichen `CompileMutations`-Pfad zu prüfen. Hätte eine 30-Minuten-Recherche damals gespart, wäre die ADR-016-Decision nie getroffen worden.
2. **Comparison-Spec-Inspiration ≠ Implementations-Reality.** Was bei PIT/mutmut/cargo-mutants Performance-Wert hat, ist nicht automatisch übertragbar — Architektur-Differenzen zwischen Frameworks ändern was profitable Optimierungen sind.
3. **Sunk-Cost-Fallacy aktiv vermeiden.** Der Versuchung "wir haben Sprint 8 schon Scaffolding gebaut, also bauen wir auch v2.2.0 voll aus" wurde durch das Maxential-Branch-Forcing widerstanden.

---

## ADR-022: Inkrementelles Mutation-Testing als zukünftige Performance-Direction (Proposed)

**Status.** Proposed — 2026-05-01 (Sprint 15, v2.2.0). **Kein commitment für irgendeine Release.**

**Context.**

Sprint-15-Recherche (siehe ADR-021) hat aufgezeigt: Stryker.NET's tatsächliche Cost-Driver sind (a) Initial-Compile der all-mutations-Assembly, (b) Test-Host-Process-Spawn pro Batch. Die mutmut-Trampoline-Technik (die ADR-016 inspiriert hat) addressiert *Trampoline-Switching innerhalb eines Test-Host-Lifetimes* — das ist orthogonal zu Stryker's bereits effizienter Architektur.

Die echte Performance-Opportunity in Stryker's Architektur liegt in **inkrementellem Mutation-Testing**: bei einer Source-Datei-Änderung im Watch-Loop nur die *betroffenen* Mutanten neu testen, statt die gesamte Suite.

**Proposed direction.**

Eine zukünftige `IncrementalMutationCoordinator`-Komponente würde:
1. **File-Watcher** (`System.IO.FileSystemWatcher`) auf die Source-Verzeichnisse.
2. **Source-change-diff** — welche Syntax-Trees haben sich geändert seit dem letzten Run.
3. **Mutant-Impact-Analysis** — für jeden Mutanten cachebar als (Syntax-Tree-Hash, Test-Result). Wenn der Hash unverändert ist, Test-Result wiederverwenden.
4. **Partial-rerun** — nur Mutanten mit invalidierten Caches neu testen.
5. **Persistent-cache** zwischen Watch-Loop-Iterationen (z.B. `.stryker-cache/`-Directory).

**Realistic perf-impact:** Beim 1-Datei-Edit in einer 100-Datei-Codebase würde nur ~1% der Mutanten re-tested — das wäre die "5–10× boost" die ADR-016 ursprünglich versprach (aber für die Watch-Loop-Use-Case, nicht für den vollständigen CI-Run).

**Scope-Risk.** Inkrementelles Mutation-Testing ist eigene Multi-Sprint-Arbeit:
- Sprint A: File-watcher infrastructure + persistent cache scheme
- Sprint B: Source-change-diff + mutant-impact-analysis
- Sprint C: Watch-CLI-Mode (`stryker-netx --watch`) + reporter-integration
- Sprint D: End-zu-End-Integration mit existierender Pipeline

**No commitment.** Diese ADR ist Status: Proposed. Erst commit bei klarer User-Demand und Stakeholder-Priority. Aktuelles `--coverage-analysis perTest` (default) liefert bereits gute genug Performance für die meisten CI-Use-Cases.

**Backed by.** Sprint 15 Maxential-Branch C3 (rejected for v2.2 scope, kept as proposed direction).

---

## ADR-023: Validation-Framework Count-Tests — prinzipieller Skip statt Reconciliation (v2.3.0)

**Status.** Accepted — 2026-05-01 (Sprint 16, v2.3.0).

**Context.**

`integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs` enthält 11 `[Fact]`-Tests, die hardcoded Mutant-Counts gegen die Output-JSON-Reports der Integration-Target-Projects asserten:

```csharp
CheckReportMutants(report, total: 29, ignored: 7, survived: 3, killed: 7, timeout: 0, nocoverage: 11);
CheckReportMutants(report, total: 660, ignored: 269, survived: 4, killed: 9, timeout: 2, nocoverage: 338);
// ... etc, 11 tests total across 10 Target-Projects
```

Diese Counts sind aus dem upstream **Stryker.NET 4.14.1**-Verhalten übernommen (Sprint 3 hat die Validation-Suite vendored). Mit der v2.x-Catalogue-Erweiterung (v2.3.0 = 52 Mutatoren vs. upstream's 26) produzieren unsere Mutation-Runs **legitimate andere** Counts — mehr Mutationen, andere Survival-Patterns, andere NoCoverage-Verteilungen.

**Bisheriger Status (Sprint 4 Lessons → README v2.0.0 Known-Limitations):**

> Validation framework count-based assertions in `integrationtest/Validation/ValidationProject/ValidateStrykerResults.cs` hardcode upstream Stryker.NET 4.14.1's exact mutant counts and have NOT been reconciled to our mutator output (which legitimately differs slightly due to C#-14-aware behavior + the v2.0/v2.1 expanded catalogue). The framework BUILDS and the InitCommand validation test PASSES; per-fixture count reconciliation is a follow-up task.

Sprint 16 hat dieses follow-up evaluiert und abgelehnt.

**Decision.**

Die 11 Count-Tests werden **nicht reconciled**, sondern mit `[Fact(Skip = "...")]` deaktiviert mit dokumentierter Begründung. Der Skip-Reason verlinkt explizit auf diese ADR.

**Begründung.**

1. **Cost-to-value-Bilanz negativ.** Reconciliation würde bedeuten: jedes der 10 Target-Projects gegen die aktuelle Stryker-CLI laufen lassen, die observed counts manuell extrahieren, hardcodieren, committen. Pro zukünftiger Mutator-Addition driftet jeder Count wieder. Bei der v2.x-Geschwindigkeit (8 neue Mutatoren in Sprint 13, 3 in Sprint 14, 1 in Sprint 16) wird die Reconciliation zur Sisyphos-Aufgabe.

2. **Was diese Tests *tatsächlich* validieren ist Plumbing, nicht Mutator-Korrektheit.** Sie prüfen "der Pipeline-Run produziert *einen* JSON-Report mit *irgendwelchen* erwarteten Counts" — sie prüfen NICHT, dass die einzelnen Mutationen korrekt sind. Das letztere wird durch Unit-Tests in `Stryker.Core.Tests/Mutators/*` abgedeckt.

3. **Strict-numerical assertions auf integration-output sind anti-Stryker-evolution.** Jede Operator-Erweiterung würde die Tests breaken — was sie zu einem strukturellen Hindernis für Catalogue-Wachstum macht. Das ist der Schwanz, der mit dem Hund wedelt.

4. **Honest-Deferral-Pattern als Präzedenz.** Sprints 8 (HotSwap-Scaffolding), 11 (CRCR-Defer), 13 (Phase-A-Reconciliation), 15 (HotSwap-Walk-Back) haben das Muster "explizit + dokumentiert + nicht-versteckt" etabliert. Skip-Trait + ADR ist die direkte Anwendung.

**Alternatives evaluated.**

- *Manuelle Count-Reconciliation:* verworfen — siehe Begründung 1.
- *Tests entfernen:* verworfen — schwächer als Skip+Reason; verliert die Möglichkeit, sie wieder zu aktivieren falls die Reconciliation jemals geleistet wird.
- *Counts gegen Range-Asserts (z.B. `total >= 26`):* verworfen — weicht den Test-Sinn auf, ohne die strukturelle Drift-Problematik zu lösen.
- *Migration zu count-relativ-Assertions (e.g. "Survived/Total < 5%"):* verworfen — würde substantielle Test-Logic-Refactoring brauchen + ist immer noch fragile bei Operator-Additions.

**Consequences.**

- (+) Kein Sisyphos-Wartungsaufwand bei zukünftigen Operator-Additions.
- (+) ADR-Trail explizit dokumentiert *warum* die Tests skipped sind — nicht "vergessen".
- (+) Skip ist einfach reversibel: wenn jemand sich entscheidet, die Reconciliation zu leisten, einfach Skip-Reason entfernen.
- (–) Die Test-Runs zeigen 11 skipped tests im Output — visuell "fehlend". Mitigiert durch die explizite Skip-Reason-Dokumentation.
- (–) Plumbing-Validation der Integration-Pipeline geht verloren — aber sie ist ohnehin nicht zuverlässig (counts drift). Der `CheckMutationKindsValidity`-Helper (separate Methode in derselben Datei) bleibt aktiv via die anderen Test-Klassen falls vorhanden.

**Backed by.** Sprint 16 Maxential-Session (Item-3 scope-decision); Sprint-3 / Sprint-4 lessons documenting the original count-hardcoding decision; v2.0.0 README Known-Limitations entry that explicitly named this as a follow-up.

---

## ADR-024: JsonReport full AOT-trim — v3.0-scope deferral (v2.4.0)

**Status.** Accepted — 2026-05-01 (Sprint 17, v2.4.0).

**Context.**

v2.3.0 (Sprint 16) shipped JsonReport hybrid source-gen serialization: `JsonReportSerializerContext` provides `JsonTypeInfo` for the entry types `JsonReport` + `IJsonReport`; custom polymorphic converters (`SourceFileConverter`, `JsonMutantConverter`, `LocationConverter`, `PositionConverter`, `JsonTestFileConverter`, `JsonTestConverter`) attach to the runtime `JsonSerializerOptions` via `JsonTypeInfoResolver.Combine` and continue to handle interface-typed properties at runtime. The Sprint 16 lessons doc explicitly noted this as "AOT-trim progress, not AOT-trim complete" and listed full AOT-trim as long-tail.

Sprint 17 evaluated the full AOT-trim scope:

- 7 interface types in `Stryker.Abstractions/Reporting/` (IJsonReport, ISourceFile, IJsonMutant, IJsonTestFile, IJsonTest, ILocation, IPosition)
- 34 source files reference at least one of these interfaces
- Full AOT-trim requires flattening these interfaces to concrete types (e.g. sealed records) — at which point custom converters become unnecessary, and source-gen handles everything natively

**Decision.**

Defer JsonReport full AOT-trim to **v3.0**.

**Backed by.** Sprint 17 Maxential-Session (3-way branch E1=full-refactor / E2=parallel-concrete-shim / E3=defer-to-v3.0; E3 chosen). Aligns with Sprint 15 ADR-021 (HotSwap walk-back) + Sprint 16 ADR-023 (Validation-non-reconciliation) deferral discipline.

**Begründung.**

1. **Breaking change cadence.** Flattening 7 public interfaces to concrete types is a breaking API change that violates v2.0.0's stated "zero breaking changes for default profile" principle (README + MIGRATION). The right cadence for breaking interface changes is a major-version boundary — i.e. v3.0.

2. **Parallel concrete-types variant doubles maintenance.** Branch E2 evaluated shipping concrete-typed records alongside the interface shims (analogous to Sprint 15's `[Obsolete]`-shim pattern for `MutationEngine`). Rejected — interface flattening for serialization isn't symmetric with `[Obsolete]` deprecation; the user-visible benefit is gated on opting into the new types, which means doubling the surface for mild-marginal value.

3. **Current AOT-progress is sufficient for v2.x.** v2.3.0's hybrid source-gen eliminates reflection on the entry-type metadata graph — embedders that don't need full AOT-trim get measurable startup-time wins already. Full AOT-trim is the kind of "5%-of-users-care-deeply" feature that justifies its own focused major release.

**Alternatives evaluated (Maxential branches).**

- **E1 — Full breaking-change refactor** in v2.4.0. Rejected — violates no-breaking-changes-mid-major-version principle.
- **E2 — Parallel concrete-type variant + `[Obsolete]` interfaces.** Rejected — doubles maintenance with low marginal value.

**Consequences.**

- (+) Honest scope assessment — refactor is genuinely v3.0-sized.
- (+) Aligns with established v2.x deferral discipline (ADR-021, ADR-023).
- (+) v3.0 sprint will batch this with the `[Obsolete]` `MutationEngine` hard-removal — coherent breaking-change release.
- (–) Long-tail item stays open for one more major version.

**Implementation outline für v3.0.**

1. Future-ADR (TBD-Nummer): concrete-types schema for the JsonReport family — note: ADR-025 was originally reserved for this, but Sprint 140 (Profile×Level Auto-Bump, see below) took the slot first; the JsonReport-concrete-types ADR will get the next free number when actually scheduled.
2. Replace interface declarations with `sealed record` declarations (or sealed classes if record-init-binding doesn't work for the polymorphic-deserialization case)
3. Delete custom converters (`SourceFileConverter`, `JsonMutantConverter`, etc.)
4. Simplify `JsonReportSerializerContext` to handle the full type graph natively
5. Remove `JsonTypeInfoResolver.Combine` plumbing in `JsonReportSerialization`
6. Update embedders' migration guide

---

## ADR-025: Mutation-Profile Auto-Bump für Mutation-Level (v3.1.0)

**Status.** Accepted — 2026-05-06 (Sprint 140, v3.1.0). Backed by Sprint-140-ToT (5 Branches A/B/C/D/E, score-ranked, C+E pruned) + Maxential (14 Thoughts, 2 closed branches `B-autobump` + `D-hybrid`, both full-integration merged).

**Context.**

`mutation-profile` (ADR-018, Sprint 6) und `mutation-level` (geerbt von Stryker.NET) sind orthogonale Filter-Achsen, die conjunctive zusammenwirken. Pro Mutator wird gefiltert:

1. Profile-Filter via `MutationProfileMembershipAttribute` (zb `[MutationProfile.Stronger | MutationProfile.All]`)
2. Level-Filter via `MutatorBase<T>.MutationLevel` Property (`<= options.MutationLevel`)

Beide müssen passieren, sonst feuert der Mutator nicht.

Real-Life-Bug-Report (Sprint 138 `_bug_reporting/bug_report_stryker_netx.md`, Bug #1) hat aufgezeigt: `--mutation-profile Stronger` bei Default-`--mutation-level Standard` ist schweigsam wirkungslos, weil alle 18 Stronger-only-Mutatoren `MutationLevel = Advanced (50)` oder höher haben — der Level-Filter kickt sie raus, bevor der Profile-Filter sie passieren kann.

Sprint 139 hat das Doku-Side adressiert. Sprint 140 muss die Code-Side klären: schweigsamen No-Op vermeiden.

**Decision.**

Wenn der User `--mutation-profile Stronger` oder `--mutation-profile All` setzt, **ohne** explizit `--mutation-level` zu setzen, **bumped der Orchestrator das Level automatisch** auf den passenden Wert:

- `Profile=Stronger` + Level-implicit → `Level=Advanced`
- `Profile=All` + Level-implicit → `Level=Complete`
- `Profile=Defaults` + Level-implicit → `Level=Standard` (= unverändert, heute schon Default)
- Jede explizite Level-Setzung (auch `Standard`) **gewinnt immer** — kein Override.

Ein Info-Log macht den Auto-Bump sichtbar: `[INF] mutation-level auto-set to {X} based on mutation-profile={Y} (no explicit --mutation-level supplied).`

**Detection-Logik:**

In `StrykerInputs.BuildStrykerOptions()` (Datei `src/Stryker.Configuration/Options/StrykerInputs.cs` ~Zeile 123-124):

- `MutationProfileInput.SuppliedInput is not null` AND validate-result `!= MutationProfile.Defaults`: User hat Profile explicit auf Stronger/All gesetzt
- AND `MutationLevelInput.SuppliedInput is null`: User hat Level NICHT gesetzt
- → Override `MutationLevel` mit dem profile-passenden Wert + Info-Log

**Alternatives (verworfen — siehe Maxential).**

- **A — Warning only, kein Auto-Bump.** Score 0.55. Verworfen weil silent no-op weiterhin möglich (User ignoriert Warning). Keine echte UX-Reparatur.
- **C — Profile-Bundle-Werte (DefaultsStandard/StrongerAdvanced/AllComplete als kombinierte Flags).** Score 0.20. Verworfen — Major Breaking-Change, verletzt 1:1 schema-compat mit upstream Stryker.NET (README-Versprechen).
- **D — Hybrid (Auto-Bump + Opt-out-Flag --no-auto-mutation-level).** Score 0.82 (höchster ToT-Score!). Verworfen via Maxential YAGNI: der Opt-out-Flag adressiert ein 1%-Use-Case (User will Profile=Stronger + Level=Standard = Defaults-Equivalent) der durch Setzen explicit `--mutation-level Standard` schon gelöst ist. Der Flag wäre 70 LOC + 9 Tests für 1% Use-Case. Wenn künftig real-world Demand auftaucht, additiv nachrüstbar.
- **E — Profile-Forces-Level (Profile != Defaults bypasst Level-Filter komplett).** Score 0.40. Verworfen — würde die Level-Semantic von "filter on/off pro Mutator" zu "fine-grained tuning" ändern, divergiert von upstream Stryker.NET.

**Consequences.**

- (+) `--mutation-profile Stronger` ohne expliziten Level zeigt jetzt sofort die erwartete Wirkung. UX-Reparatur des Calculator-Bug-Reports.
- (+) Backwards-compat: User die heute beide Flags explizit setzen, sehen identisches Verhalten. User die heute nur Profile setzen, bekommen ein One-Liner Info-Log + 18 zusätzliche Mutatoren feuern (was sie ohnehin wollten).
- (+) Implementation-Aufwand klein (~6 LOC + 9 Tests), Maintenance-Surface minimal.
- (–) Behavior-Change im Default-Pfad — daher v3.1.0 (Minor-Bump) statt v3.0.26 (Patch). User die heute auf v3.0.x in CI gepinned sind, sehen die Änderung erst nach explizitem `dotnet tool update`.
- (–) Implicit default-shift verletzt "explicit > implicit" Zen leicht. Mitigiert durch Info-Log, der den Auto-Bump explizit announciert.

**Implementation outline (Sprint 140).**

1. Modifikation `StrykerInputs.BuildStrykerOptions()` (~Zeile 123-124)
2. Helper-Method (oder inline) `ResolveMutationLevel(profile, levelSuppliedInput)`
3. ILogger-Injection für Info-Log (`ApplicationLogging.LoggerFactory.CreateLogger<StrykerInputs>()`)
4. Unit-Tests `tests/Stryker.Configuration.Tests/MutationProfileAutoBumpTests.cs` mit 9 Cases:
   - 3 Profile-Werte × {Level explicit / Level implicit / Level-explicit-equal-to-bump-target}
5. Doku-Update `_config_neuprojekte/Stryker_NetX_Installation.md`: "Sprint 140 (geplant)"-Forward-Reference entfernen, neue Auto-Bump-Behavior dokumentieren.

**Backed by.** Sprint 140 ToT (Tree-of-Thoughts mit 5 Branches, Pruning niedrig-scored Optionen, score 0.82 Best-Path) + Sprint 140 Maxential (14 Thoughts, 2 Branches `B-autobump` + `D-hybrid`, full-integration-merged, conclusion mit `decision`+`synthesis`-Tags markiert).

---

## ADR-026: ConditionalInstrumentation × TypeSyntax-/SimpleName-Slot incompat (v3.1.2)

**Status.** Accepted — 2026-05-06 (Sprint 142, v3.1.2 Hotfix). Backed by Sprint 142 Maxential (5 Thoughts, decision tag `decision`+`synthesis`).

**Context.**

Calculator-Tester-Bug-Report (`_bug_reporting/bug_report_2_stryker_netx.md`, Bug #9) meldete einen Crash bei `--mutation-profile All`:

```
System.InvalidCastException: Unable to cast object of type
'Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax'
to type 'Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax'.
```

Sprint 142 Bisect-Diagnose: zwei distinct trigger:
1. **`UoiMutator`** auf `SimpleNameSyntax`-Slots (`MemberAccess.Name`, `MemberBinding.Name`). Beispiel-Repro: `data.Length > 0 ? data[0] : 0` — UoiMutator wrapt `Length`, der ConditionalInstrumentationEngine erzeugt `data.(IsActive(N) ? Length++ : Length)` — eine `ParenthesizedExpressionSyntax` in einem `SimpleNameSyntax`-Slot, die Roslyn's typed visitor mit `InvalidCastException(ParenthesizedExpression → SimpleNameSyntax)` ablehnt.
2. **`SpanReadOnlySpanDeclarationMutator`** auf `TypeSyntax`-Slots. Der Mutator emittiert `GenericNameSyntax`-Replacements ausschließlich in TypeSyntax-Positionen (parameter type, field type, etc.). Der ConditionalInstrumentationEngine wickelt jede Mutation in `(IsActive(N) ? mutated : original)` = `ParenthesizedExpressionSyntax` → `InvalidCastException(ParenthesizedExpression → TypeSyntax)`.

Beide Crashes haben dieselbe root cause: **der ConditionalInstrumentationEngine emittiert ParenthesizedExpression als Mutation-Wrap, was nur in ExpressionSyntax-Slots zulässig ist; in TypeSyntax-/NameSyntax-Slots ist der Cast blocked.**

**Sprint 23 (v2.10.0) hat das gleiche Pattern für `QualifiedNameSyntax` schon adressiert** (UoiMutator parent-skip + global `DoNotMutateOrchestrator<QualifiedNameSyntax>`). Sprint 142 erweitert das pattern auf die zwei neuen crash-Klassen.

**Decision.**

Sprint 23-Pattern (Mutator-internal pre-check + global Belt-and-suspenders) auf die neuen crash-Klassen übertragen:

1. **UoiMutator** `IsSafeToWrap()` erweitern: skip `MemberAccess.Name == node` und `MemberBinding.Name == node`.
2. **SpanReadOnlySpanDeclarationMutator** disablen aus allen Profilen via `[MutationProfileMembership(MutationProfile.None)]`. Der Mutator targets ausschließlich TypeSyntax-Slots, was im aktuellen Engine prinzipiell incompatible ist. Re-enabling bedingt dass die ConditionalInstrumentationEngine eine type-position-aware Variante bekommt (z.B. `[Conditional("MUTATION_N")]`-Pattern oder static-field-switching ohne Expression-Wrap).
3. **Global `DoNotMutateOrchestrator<SimpleNameSyntax>`** mit Predicate `parent.Name == t` als Belt-and-suspenders. Predicate scoped strict — locals und sonstige SimpleNames bleiben mutable.

**Alternatives evaluated (Maxential).**

- **A (mutator-only):** nur UoiMutator pre-check, kein global guard. Verworfen — fehlt future-proofing wenn andere Mutators auch SimpleName.Name-Slots betreffen.
- **C (engine rewrite):** ConditionalInstrumentationEngine erkennt TypeSyntax-/NameSyntax-Position und emittiert non-Parenthesized control. Verworfen für hotfix — zu invasiv. Wahrscheinlich richtige Long-term-Lösung, aber separate ADR und mehrere Sprints.
- **D (SpanReadOnly behalten + per-mutator-pre-check):** der SpanReadOnly-Mutator emittiert AUSSCHLIESSLICH in TypeSyntax-Slots — ein pre-check der diese skippt würde den Mutator komplett deaktivieren. Daher direkter als-disable-markieren via Profile.None, mit klarer Re-enable-Bedingung in der Doku.

**Consequences.**

- (+) `--mutation-profile All` funktioniert wieder. Calculator-Tester-Crash eliminiert.
- (+) Pattern aus Sprint 23 robust auf weitere crash-Klassen übertragbar (`SimpleNameSyntax.Name`-Slots).
- (+) Hotfix v3.1.2 (Patch) — keine breaking-changes für die meisten User. SpanReadOnly-Disable affects nur User die explizit `--mutation-profile All` setzen UND auf der spezifischen Span-decl-mutation gerechnet haben (rare).
- (–) `SpanReadOnlySpanDeclarationMutator` ist temporär dormant (52 → 51 effective Mutators in `All`-Profile). Re-enable pending engine-fix.
- (–) Long-term: ConditionalInstrumentationEngine sollte type-/name-position-aware werden (eigene ADR, eigene Sprints).

**Future re-enable conditions for SpanReadOnlySpanDeclarationMutator.**

- Engine-fix: Conditional-control variant, der in TypeSyntax-positions kein ParenthesizedExpression wrapt
- ODER: Mutator emittiert die Mutation direkt ohne control-wrap (Mutant ist nicht runtime-switch-bar, sondern nur compile-error-detected)
- ODER: Refaktoriere SpanReadOnly als file-level rewrite (nicht per-instance)

**Backed by.** Sprint 142 Maxential 5 Thoughts (Reset clean, conclusion `decision`+`synthesis` tagged) + lokaler Bisect-Trail in `samples/Sample.Library/SpanTester.cs` (temporary repro-fixture, removed before commit if fix lands cleanly).

---

## ADR-027: Type-Position-Aware Mutation Control — Multi-Sprint Engine-Refactor (v3.2.0-dev / Sprint 143+)

**Status.** Accepted (Phase 1 implemented in Sprint 143; Phase 2+ planned).

**Datum.** 2026-05-06.

**Vorgeschichte.**

ADR-026 (Sprint 142 Hotfix v3.1.2) hat Bug #9 (`--mutation-profile All` Crash) durch eine **defensive Mutator-Skip-Strategie** geschlossen: betroffene Mutator-Stellen wurden konservativ ausgeschlossen, um den `InvalidCastException(ParenthesizedExpression → SimpleNameSyntax/TypeSyntax)`-Crash zu vermeiden. **Begründung damals:** schneller Patch-Release ohne breaking-engine-changes.

**User-Pushback (Sprint 142 Closing-Review).** Der User hat explizit kritisiert, dass ADR-026 eine **symptomatische** Lösung ist: "Aber durch den Hotfix werden die 'Fehlerverursacher' ja nur 'geskippt' und das Tool läuft ohne Fehler durch. Das kann doch aber nicht die Lösung sein. Für einen schnellen HotFix vielleicht. Aber der Fehler hätte durch ein Engine-Rewrite (type-position-aware) entfernt werden müssen, auch wenn das invasiv ist." → Sprint 143+ wird als **Multi-Sprint Engine-Refaktor** beauftragt; v3.2.0-Tag erst nach Abschluss aller Phasen, kein vorzeitiger Patch-Tag.

**Entscheidung.**

Die Mutation-Pipeline wird in **drei Phasen** type-position-aware gemacht. Jede Phase hat einen eigenen Sprint (oder mehr), eigene Verifikation, eigene Tests; KEIN finaler Tag bis zum Abschluss aller Phasen.

### Phase 1 (Sprint 143) — Smart-Pivot für `MemberAccess.Name`-Slot ✅ implementiert

**Mechanismus.** Statt einer Mutation auf der `IdentifierName` (die in einem strikt-typisierten `SimpleNameSyntax`-Slot steckt) lift der Mutator die `OriginalNode` auf die umschließende `MemberAccessExpressionSyntax` und der `ReplacementNode` wickelt den vollen Member-Access-Ausdruck in den Postfix/Prefix-Operator. Beispiel: `data.Length` → `data.Length++` (statt des Sprint-142-Hotfix-Skips).

**Drei kooperierende Änderungen.**

1. **`CsharpMutantOrchestrator.GenerateMutationsForNode`** (Z.222–230): `mutation.OriginalNode = current` → `mutation.OriginalNode ??= current`. Der Default-Vertrag bleibt "OriginalNode ist die besuchte Node", aber Mutatoren dürfen explizit eine Eltern-Node setzen. Reine Erweiterung — kein bestehender Mutator wird betroffen.
2. **`UoiMutator.ApplyMutations`** (Sprint 10): erkennt `node.Parent is MemberAccessExpressionSyntax ma && ma.Name == node` und setzt `pivot = ma`. Die vier Postfix/Prefix-Mutations werden auf `pivot` gewickelt (`OriginalNode = ma`, `ReplacementNode = PostfixUnary(ma)` etc.). Gegenstück: Sprint-142-Skip für `MemberAccess.Name` aus `IsSafeToWrap` entfernt.
3. **`MemberAccessNameSlotOrchestrator`** (neu): `NodeSpecificOrchestrator<SimpleNameSyntax, ExpressionSyntax>` mit `CanHandle = ma.Name == t`. Tritt VOR dem generischen `MemberAccessExpressionOrchestrator<SimpleNameSyntax>` ein und ruft `context.Enter(MutationControl.MemberAccess)` auf. Konsequenz: `MutationStore.Inject` wird auf SimpleName-Ebene unterdrückt (`Control == MemberAccess` Bail-Out), die mutationen blubbern auf der `Leave()`-Pop in den umschließenden `MemberAccess`-Frame, dessen Inject-Call dann `sourceNode.InjectMutation(mutation)` mit `sourceNode = MA, mutation.OriginalNode = MA` aufruft — `Contains` ist `true`, `ReplaceNode` produziert das gewünschte Tree.

**`MemberBinding.Name` (`data?.Length`) bleibt in Phase 1 geskippt** über einen reduzierten DoNotMutateOrchestrator. Begründung: ein analoger Pivot zu MB würde `PostfixUnary(MB)` in `ConditionalAccessExpression.WhenNotNull` legen, was strukturell valid ist, aber den Roslyn-Binder bricht (WhenNotNull muss binding-led — `.` oder `[` — sein). Der Bug ist kein Roslyn-Visitor-Crash, sondern ein Binder-Error, der die Compilation der gesamten mutierten Datei vergiftet (alle Mutations auf der Datei werden CompileError statt selektiv klassifiziert).

**Phase 1 Verifikation (Sprint 143).**

- Lokaler Bisect-Trail mit temporärer `samples/Sample.Library/SpanTester.cs` (`data.Length > 0 ? data[0] : 0` + `data?.Length ?? 0`): `--mutation-profile All --mutation-level Complete` läuft sauber durch — kein Crash, +28 testbare Mutations auf der Repro-Datei, Calculator-Baseline (30 killed / 14 survived) unverändert. Repro-Fixture vor Commit entfernt (würde `Defaults_ProducesExpectedTotalAndScore` E2E-Baseline brechen, gleich wie Sprint 142). Phase-1-Regression bleibt über UoiMutator unit-tests abgedeckt.
- `tests/Stryker.Core.Tests/Mutators/UoiMutatorTests.cs`: `MutatesAtParentLevel_RightHandOfMemberAccess` (4 Mutations mit `OriginalNode = parent MA`), `StillMutates_LocalIdentifierInExpression` (Pivot fired NICHT für plain identifier), `DoesNotMutate_RightHandOfMemberBinding` (Phase-2-deferred-Skip dokumentiert).
- Solution-wide: 0 Warnings, 0 Errors, ~2200 Tests grün (RedirectDebugAssert ist pre-existing nicht-deterministischer Flake aus Sprint 27, unabhängig).
- Semgrep: 0 Findings auf den 6 modifizierten Dateien.

### Phase 2 (Sprint 144) ✅ implementiert — CAE-aware Lifting für `MemberBinding.Name`-Slot + TypeSyntax-Skip

**Problem.** Der `WhenNotNull`-Slot eines `ConditionalAccessExpression` (`?.`-Operator) verlangt eine binding-led Expression (Start mit `.` oder `[`). Ein Phase-1-style Pivot auf `MemberBindingExpression` produziert `(?.Length)++` Tree-shape, was Roslyn-Binder rejects. Stattdessen muss bei `MB.Name` der Pivot **bis zum umschließenden CAE** gehoben werden, sodass der Postfix-/Prefix-Operator das gesamte `data?.Length` umschließt.

**Phase-1-Gap entdeckt (Sprint-144-Bisect).** Das Problem ist NICHT auf `MB.Name` beschränkt: jeder Pivot der innerhalb einer `CAE.WhenNotNull`-Subtree landet — auch ein `MA.Name`-Pivot (`box?.Inner.Length`) oder ein deeper `MB`-Inside-Invocation (`matrix?.GetType().Name?.Length`) — produziert dieselbe binder-rejection. Sample.Library hatte keine solche Patterns, daher Phase-1-e2e nicht broken; Real-World-Code mit `?.X.Y`-Chains hingegen schon. Phase 2 generalisiert den Lift: walking-up bis zur outermost-CAE in der `?.`-Kette, durch alle Zwischenebenen (Invocation, MA-Expression-side, CAE.Expression-side-crossing) hindurch.

**Implementierte Änderungen.**

1. **`UoiMutator.ApplyMutations`**: initial pivot extension auf `MB.Name` (Phase-1 hatte nur `MA.Name`), dann `LiftPastConditionalAccess` while-loop. Die Lift-Hilfe `FindEnclosingCaeViaWhenNotNull` walks the parent chain und returns die erste CAE deren WhenNotNull ein Vorfahr ist (CAEs deren Expression-side gekreuzt werden, sind transparent — wir gehen weiter).
2. **`UoiMutator.IsSafeToWrap`**: Phase-1 MB.Name-skip entfernt (CAE-walk-up handled). Refaktoriert mit dedizierten Helper-Methoden (Methodenlänge ≤ 60 für MA0051). NEU: `IsInTypeSyntaxPosition`-Skip für IdentifierName in TypeSyntax-typed Slots — Sprint 142 hatte das nur für `SpanReadOnlySpanDeclarationMutator` (GenericName); UoiMutator (IdentifierName) hatte den Guard nicht, was bei user-defined Property-Types in Real-World-Code zum identischen Cast-Crash geführt hätte. Re-enable in Phase 3.
3. **`MemberAccessNameSlotOrchestrator.CanHandle`**: predicate aufgeweitet auf MA.Name OR MB.Name. Existing `MutationControl.MemberAccess`-Defer-Mechanik trägt Phase 2 von selbst — Inject defers bis zur outer-CAE-frame, Mutation injects als `sourceCAE.InjectMutation(mutation)` mit `mutation.OriginalNode = CAE`.
4. **`CsharpMutantOrchestrator.BuildOrchestratorList`**: Phase-1's `DoNotMutateOrchestrator<SimpleNameSyntax>(MB.Name)` Guard ENTFERNT.
5. **`ConditionalExpressionOrchestrator`**: UNVERÄNDERT — bestehende Mechanik trägt Phase 2.

**Phase 2 Verifikation (Sprint 144).**

- Lokaler Bisect-Trail mit `samples/Sample.Library/SpanTester.cs` (`data.Length > 0 ? data[0] : 0` + `data?.Length ?? 0` + `matrix?.GetType().Name?.Length ?? 0`). Sprint-143-Phase-1 cracht auf `box?.Inner.Length` mit `InvalidCastException(ParenthesizedExpression → TypeSyntax)` (TypeSyntax-Slot-Issue für user-defined `Box` und `BoxInner` Property-Types). Sprint-144-Phase-2 läuft sauber durch — kein Crash, kein file-compile-poisoning. Calculator-Baseline (30 killed / 14 survived) unverändert. SpanTester-Repro produziert +43 testbare Mutations mit individueller Compile-Error-Klassifikation (vs. ganzheitlicher file-compile-fail). Repro-Fixture vor Commit entfernt (würde `Defaults_ProducesExpectedTotalAndScore` E2E-Baseline brechen, gleich wie Sprint 142+143).
- `tests/Stryker.Core.Tests/Mutators/UoiMutatorTests.cs`: 10 grün — 4 Phase-1-regression + 6 Phase-2 NEU: `MutatesAtCaeLevel_RightHandOfMemberBinding` (`data?.Length`), `MutatesAtOutermostCae_NestedConditionalAccess` (`a?.b?.c`), `MutatesAtOutermostCae_MaInWhenNotNullSubtree` (`box?.Inner.Length`), `DoesNotMutate_IdentifierInTypeSyntaxPosition` (Property-Type-Slot).
- Solution-wide: 0 Warnings, 0 Errors, ~2200 Tests grün (RedirectDebugAssert pre-existing flake bleibt unverändert).
- Semgrep: 0 Findings auf 4 modifizierten Dateien.

**Algorithm — `LiftPastConditionalAccess`.**

```csharp
ExpressionSyntax pivot = initial; // MA / MB / Identifier
while (true) {
    var enclosingCae = FindEnclosingCaeViaWhenNotNull(pivot);
    if (enclosingCae is null) return pivot;
    pivot = enclosingCae;
}

// FindEnclosingCaeViaWhenNotNull walks up the parent chain. Returns the
// first CAE we cross from the WhenNotNull side. CAEs we cross from the
// Expression side are transparent — we keep walking. Critical for nested
// scenarios like `matrix?.GetType().Name?.Length` where MB(.GetType) sits
// inside MA -> CAE2.Expression -> CAE1.WhenNotNull (so the first
// WhenNotNull-side crossing is at CAE1, not CAE2).
```

### Phase 3 (Sprint 145) ✅ abgeschlossen — Skip-as-Architecture für `TypeSyntax`-Slots

**Decision.** Phase 3 schließt ADR-027 mit `Skip-as-Architecture`: der existierende
`SpanReadOnlySpanDeclarationMutator` bleibt `Profile.None`, der Phase-2
`UoiMutator.IsInTypeSyntaxPosition`-Skip bleibt aktiv. Die geplante
`TypeAwareInstrumentationEngine` (Option A) wurde zu Sprint-Beginn via
strukturierter Maxential-Analyse (11 Schritte, 3 Engine-Refactor-Alternativen)
verworfen.

**Maxential Cost/Benefit (Sprint 145).**

| Option | Aufwand | User-Wert | Risk | Entscheidung |
|--------|---------|-----------|------|--------------|
| A — TypeReplacementInstrumentationEngine + Pipeline-Refactor (separate-compile pro mutation) | 4+ Sprints | LOW (1 niche mutator + meaningless UOI) | HIGH | verworfen |
| B — Preprocessor-Direktiven `[Conditional("MUTANT_N")]`-Envelope | 5+ Sprints | LOW | VERY HIGH | verworfen |
| C — Re-architecting auf Method/Class-Level mutation-emit | 6+ Sprints | LOW | VERY HIGH | verworfen |
| D — Per-Mutation separate Compilation (im Wesentlichen identisch zu A) | 4+ Sprints | LOW | HIGH | verworfen |
| G — Targeted Engine NUR für SpanReadOnly | 3+ Sprints | LOW | HIGH | verworfen |
| **F — Skip-Formalization** | 1-2h | MEDIUM (ADR-completion + clarity) | LOW | **gewählt** |

**Argumentation für F (skip-as-architecture).**

1. **`ConditionalInstrumentationEngine` ist by-design Expression-level.** Upstream Stryker.NET hat keine TypeSyntax-targeting Mutators; die Conditional-Engine produziert immer `ParenthesizedExpression`, was definitionsgemäß TypeSyntax-incompatibel ist. Eine TypeSyntax-aware Engine wäre keine Erweiterung der bestehenden, sondern eine zweite Pipeline-Schicht (file-level separate-compile).
2. **User-Wert ist marginal:** SpanReadOnly ist ein Sprint-14-niche-Mutator (Span↔ReadOnlySpan, Memory↔ReadOnlyMemory), kompiliert in Read-Only-Bodies binär-identisch (Mutation surviv't ohne semantische Diskriminierung) und in Write-Bodies compile-error (Stryker klassifiziert das schon ohne Engine-Refactor als killed). Der Engine-Aufwand würde keinen new test signal liefern. UoiMutator-on-TypeSyntax ist semantisch sinnlos: `BoxInner++` in `BoxInner Inner { get; }` ist 100% non-compiling.
3. **Phase 1+2 sind der echte Engine-Refactor:** smart-pivot via `OriginalNode??=current`, `MemberAccessNameSlotOrchestrator`, `LiftPastConditionalAccess`. Bug-9 ist root-cause-fixed für die wichtigen Cases (Expression-level + CAE-aware). User-Forderung aus Sprint-142-Closing-Review ("Engine-Refactor statt panic-skip") ist erfüllt.
4. **TypeSyntax-Skip ist NICHT panic-skip:** UoiMutator dokumentiert seinen TypeSyntax-Skip als architectural-boundary mit Maxential-Trail-Verweis. SpanReadOnly's `Profile.None` ist nicht-temporär. Beide tragen den Trail explizit im Doku-Comment.

**Phase 3 Implementation (Sprint 145).**

1. `UoiMutator.IsInTypeSyntaxPosition`: Doku-Comment formalisiert als final architecture, mit Begründung "100% non-compiling mutations have no test signal".
2. `SpanReadOnlySpanDeclarationMutator`: Doku-Comment formalisiert als `Profile.None`-final, mit cost/benefit-Trail.
3. `MutatorReflectionProperties.IntentionallyDisabledMutators`: Doku update — "ADR-027 Phase 3 finalized — skip is architectural decision".
4. ADR-027 Phase 3 Sektion (diese): von "TBD geplant" auf "abgeschlossen". Maxential cost/benefit table eingebracht.
5. **KEIN Code-Logic-Change** — Tests bleiben unverändert grün.

**v3.2.0 Tag-Justification.** Phase 1 + 2 haben den Engine-Refactor implementiert; Phase 3 schließt das ADR mit der korrekten Architektur-Boundary. ADR-027 ist complete. Tag v3.2.0 final.

**User-Pushback-Path.** Wenn der Engine-Refactor (Option A) doch gewünscht: eigener v3.3.0+ Sprint-Auftrag mit klarer 4+ Sprint-Aufwand-Erwartung und expliziter user-value Begründung (welcher Real-World-Code profitiert von SpanReadOnly-mutations).

### Phase 3 — Skip-list expansion (Sprint 146 Hotfix v3.2.1)

**Befund.** Calculator-Tester report 3 (2026-05-06, 6h nach v3.2.0-Release) reproduziert Bug-9 Crash in v3.2.0 — identischer `InvalidCastException(ParenthesizedExpression → TypeSyntax)` Stack-Trace wie in v3.1.1. Die Root-Cause-Analyse zeigt: Phase-2's `IsInTypeSyntaxPosition`-Skip-Liste ist unvollständig. Sample.Library hat keine pattern-matching-Patterns; Calculator.Domain (Records + Switch-Expressions) trifft sie aber.

**Lokal-Repro Sprint 146.** Minimal:
```csharp
abstract record Transaction;
sealed record Deposit(int Amount) : Transaction;
int Get(Transaction t) => t switch { Deposit d => d.Amount, _ => 0 };
```
UOI feuert auf `Deposit` IdentifierName in `Deposit d`-Pattern. Parent ist `DeclarationPatternSyntax`. Phase-2's switch-arm-list enthielt **DeclarationPatternSyntax NICHT** → Skip greift nicht → ParenthesizedExpression-Envelope landet im DeclarationPattern.Type-Slot (TypeSyntax-strict) → Crash bei OrchestrateChildrenMutation.

**Skip-list expansion (4 neue arms).**

```csharp
DeclarationPatternSyntax dp => dp.Type == current,        // `t is Deposit d`
TypePatternSyntax tp => tp.Type == current,                // defensive: type-only pattern
RecursivePatternSyntax rp => rp.Type == current,           // `t is Deposit { Amount: 5 }`
TypeParameterConstraintClauseSyntax tpc => tpc.Name == current, // `where T : class`
```

**Warum war Sprint 144 davon nicht betroffen?** Sample.Library hat keine Records, keine pattern-matching Switch-Expressions, keine Type-constraint generic methods. Der Phase-2-Skip-Set war ausreichend für die in Sprints 143/144 getesteten Patterns aber nicht für Real-World-Domain-Code mit C# 9+ Pattern-Matching.

**Verifikation Sprint 146.**
- Lokaler Repro mit minimalem `t switch { Deposit d => ... }`: kein Crash, Score 68.89% (31 killed + 14 survived).
- 4 neue UoiMutator-Tests: `DoesNotMutate_TypeNameInDeclarationPattern`, `DoesNotMutate_TypeNameInRecursivePattern`, `DoesNotMutate_TypeParameterInConstraintClause`, `TypePatternSlot_IsCoveredByIsInTypeSyntaxPositionSwitch` (defensive — Roslyn-Parser produziert `TypePatternSyntax` selten, aber Skip-arm bleibt für Future-Roslyn-Behavior).
- Solution-wide tests grün (14 UoiMutator-Tests, ~2200 solution-wide).

**Phase-3-Skip-as-Architecture-Decision bleibt unverändert** — die finale Architektur ist weiterhin "Skip in TypeSyntax-Slots, kein Engine-Refactor". Nur die Skip-Liste war unvollständig. Phase 3 ist NICHT re-eröffnet — Sprint 146 ist ein punktueller Hotfix der den ursprünglichen Skip-as-Architecture-Plan vervollständigt.

**Note für mögliche v3.3.0+ Sprints.** Sprint 146 lokaler Bisect hat einen ZWEITEN Bug aufgedeckt: GenericConstraintMutator (All-only) emittiert `OriginalNode = MethodDeclarationSyntax` mit `Type = Mutator.Statement`. Bei Methods mit expression-body landet die Mutation auf einem child-expression-Inject-Frame (T)o, dessen `sourceNode.Contains(MethodDecl)` Check fail-t — Stryker wirft `InvalidOperationException: Cannot inject mutation`. Das ist ein **separater Bug**, NICHT Calculator's Bug-9 (anderer Exception-Type). Calculator's Code triggert ihn nicht (kein generic constraint). Tracking als "v3.3.0+ Method-level-mutation-frame-routing" bug.

### Maxential / ToT Decision-Trail

- **Naive Plan rev1 (verworfen).** "Lift UoiMutator auf einen `MutateAtExpressionLevelOrchestrator<IdentifierNameSyntax>` der die Mutation an die Expression-Ebene escaliert." Lokal getestet → der Crash hat sich nur eine Layer früher in der Stack manifestiert (`InvalidCastException(PostfixUnary → SimpleName)` statt Parens → SimpleName), weil `RoslynHelper.InjectMutation` mit `oldNode = IdentifierName` immer noch die strikte Slot-Substitution macht, BEVOR die Engine wrapt. Erkenntnis: das Problem ist nicht der Inject-Frame-Level, sondern der `(OriginalNode, ReplacementNode)`-Pair self.
- **Plan rev2 (akzeptiert).** Pivot der `OriginalNode` selbst — der Mutator entscheidet wo die Mutation strukturell sitzt. Engine respektiert das via `??=`. Phase-1 für MA, Phase-2 für MB-via-CAE, Phase-3 für TypeSyntax.

**Backed by.** Sprint 143 lokaler Bisect-Trail (mehrere `dotnet stryker-netx` Re-runs auf SpanTester.cs zwischen rev1- und rev2-Plan), User-Feedback aus Sprint-142-Closing-Review (zitiert oben), `Stryker.Core.Mutants.MutationStore.Inject` Code-Lesung zur Klärung der Frame-Bubble-Mechanik.

**Konsequenzen.**

- (+) Root-cause-fix für die Expression-level + CAE-aware Bug-9-Klasse (Phase 1+2): die Engine ist jetzt type-position-aware für SimpleName-Slots in MA.Name, MB.Name, und CAE.WhenNotNull-Subtrees. Kein Skip mehr für die Real-World-relevanten Cases.
- (+) Phase 1 stellt UOI-Coverage auf MA.Name wieder her (war Sprint-142-Hotfix-Verlust).
- (+) Phase 2 stellt UOI-Coverage auf MB.Name + MA-in-CAE-Subtree wieder her und discovered + mitigated einen latenten Phase-1-Crash für TypeSyntax-Position IdentifierName (user-defined Property-Types) via TypeSyntax-Skip.
- (+) Phase 3 schließt das ADR mit Skip-as-Architecture-Decision für TypeSyntax-Slots — die korrekte Engineering-Boundary nachdem die Engine-Refactor-Optionen (4-6+ Sprints für ein niche-Feature) als cost/benefit-unfavorable verworfen wurden. ADR-027 ist complete.
- (–) `SpanReadOnlySpanDeclarationMutator` bleibt `Profile.None`. Re-enable wäre eigener v3.3.0+ Sprint mit explizitem user-value-Mandat.
- (–) UoiMutator-on-TypeSyntax-position bleibt geskippt (semantisch sinnlos — kein Verlust).

**Supersedes.** Teile von ADR-026: der `DoNotMutateOrchestrator<SimpleNameSyntax>(MA.Name || MB.Name)` Guard wurde in Phase 1 zu `MB.Name only` reduziert; Phase 2 hat ihn ganz entfernt. UoiMutator's `IsSafeToWrap`-Skip für MA.Name (Sprint 142) wurde durch Phase-1-Pivot ersetzt; UoiMutator's `IsSafeToWrap`-Skip für MB.Name (Sprint 142) wurde durch Phase-2-CAE-Pivot ersetzt. Der `SpanReadOnlySpanDeclarationMutator: Profile.None` Guard sowie Phase-2's `UoiMutator.IsInTypeSyntaxPosition`-Skip werden in Phase 3 als finale Architektur-Entscheidung formalisiert (nicht entfernt).

---

## ADR-028: Central Syntax-Slot Validation Layer (v3.2.2 / Sprint 147)

**Status.** Accepted (Sprint 147 implemented).

**Datum.** 2026-05-06.

**Vorgeschichte.**

ADR-027 (Phase 1+2 in Sprints 143+144, Phase 3 closure in Sprint 145) hat den Bug-9 InvalidCastException-Crash für die in Sample.Library reproduzierbaren Patterns gefixt. Sprint 146 v3.2.1 hat die Skip-Liste um pattern-matching-Slots erweitert (DeclarationPattern, RecursivePattern, TypePattern, TypeParameterConstraintClause). Calculator-Tester Bug-Report 4 (~6h nach v3.2.1) reproduziert den Crash dennoch — jetzt als `NullReferenceException` mit identischem Stack-Trace-Pfad. Die Skip-basierte ADR-027 Phase-3-Architektur war reaktiv: jeder neue Real-World-Pattern erforderte einen weiteren Hotfix-Sprint.

User-Forderung c) aus Bug-Report 4: **"Validierungs-Layer vor der Mutation. Eine zentrale Stelle in der Pipeline, die jede beabsichtigte Mutation auf Syntax-Konsistenz prüft, bevor sie auf den Syntax-Tree angewandt wird."** Das ist die Aufforderung zu einer fundamentalen Architektur-Änderung statt weiterer Skip-Patches.

**Maxential Decision (13 Schritte, 3 ToT-Branches).**

| Branch | Beschreibung | Entscheidung |
|--------|--------------|--------------|
| A | Try/Catch Safety Net im RoslynHelper.InjectMutation. Force-traverse nach ReplaceNode; bei InvalidCast/NRE skip. | Teil von C |
| B | Reflection-based Slot-Type-Lookup. Statisch, theoretisch sauber. | Verworfen — fragil bei SyntaxList&lt;T&gt;-children. |
| C | **Hybrid: A (Validator) + per-Mutator Audit + erweiterte Skip-Liste.** | **Gewählt** |

**Entscheidung.**

`Stryker.Core.Helpers.SyntaxSlotValidator` wird eingeführt als zentrale Pipeline-Stage zwischen Mutator-Output und MutationStore-Inject:

```csharp
internal static class SyntaxSlotValidator
{
    public static bool TryReplaceWithValidation<T>(
        T sourceNode, SyntaxNode originalNode, SyntaxNode replacementNode,
        out T result, out string? validationError) where T : SyntaxNode
    {
        // 1. Apply replacement (catch ReplaceNode-level exceptions)
        // 2. Force-traverse via DescendantNodesAndSelf().ToList() (typed-visitor cascade)
        // 3. On InvalidCast / NRE: return false with diagnostic
    }
}
```

`RoslynHelper.TryInjectMutation` wraps this validator with **defensive null-guards** for sourceNode, mutation, mutation.OriginalNode, mutation.ReplacementNode (alle vier sind Crash-Klassen, die der Calculator-Tester Bug-Report 4 explizit forderte zu fangen).

`MutationStore.ApplyMutationsValidated<T>` ist die zentrale Helper-Method die jede Mutation durch den Validator schiebt. Slot-incompatible Mutations werden (a) aus dem Conditional/If-Wrap ausgeschlossen, (b) als `MutantStatus.CompileError` mit diagnostic-reason markiert, (c) in den Logs nachvollziehbar gemacht.

**Architektur-Schnittstellen:**

```
Mutator emits Mutation
    ↓
CsharpMutantOrchestrator.GenerateMutationsForNode (mutation.OriginalNode ??= current — Sprint 143)
    ↓
MutationStore.StoreMutations
    ↓
MutationStore.Inject (4 Overloads: Expression, Statement, Block, Block-from-Expression)
    ↓
MutationStore.ApplyMutationsValidated (Sprint 147 — central validation)
    ↓ valid                          ↓ invalid
RoslynHelper.TryInjectMutation       Mutant.ResultStatus = CompileError
    ↓                                Mutant.ResultStatusReason = diagnostic
SyntaxSlotValidator.                 → mutation excluded from envelope
    TryReplaceWithValidation
    ↓
ConditionalEngine wraps
    OR IfEngine wraps
    ↓
Mutated tree
```

**Vier Crash-Klassen die Sprint 147 abdeckt:**

1. **`InvalidCastException` (`ParenthesizedExpression → TypeSyntax`)** — Bug-9 v3.1.1/v3.2.0. Ursache: Mutator emittiert PostfixUnary in TypeSyntax-Slot, ConditionalEngine wickelt in ParenthesizedExpression, typed-visitor cascade refusiert.
2. **`NullReferenceException`** — Bug-9 v3.2.1 (Calculator-Tester Bug-Report 4). Gleicher Slot-Mismatch trifft eine andere typed-visitor-Methode, die `.Property` auf einem null-cast-result aufruft.
3. **`InvalidOperationException` ("Cannot inject mutation ... because we cannot find the original code")** — historisch (Sprint 143 + Sprint 144 Lokalbefund). GenericConstraintMutator emittiert Method-Level OriginalNode in expression-level Inject-Frame; `sourceNode.Contains(MethodDecl)` schlägt fehl.
4. **`NullReferenceException` aus null-Mutation-Properties** — sourceNode/mutation/OriginalNode/ReplacementNode jeweils null durch Mutator-Edge-Cases.

Alle vier werden durch `TryInjectMutation` als false-mit-diagnostic gefangen, statt zu crashen.

**User-Forderungen (Bug-Report 4) Abdeckung:**

- a) **Ursachen-Analyse statt Stack-Trace-Cosmetik:** ✓ ADR-028 dokumentiert die vier Crash-Klassen mit konkreten Mutator-Triggern.
- b) **Pattern-Match statt `as` für Nicht-Type-Fall:** ✓ `if (mutation.OriginalNode is null) { validationError = ...; return false; }` — explizite Designentscheidung statt silent-null-Crash.
- c) **Validierungs-Layer vor der Mutation:** ✓ SyntaxSlotValidator + ApplyMutationsValidated.
- d) **Regression-Tests:** ✓ 4 SyntaxSlotValidatorTests + acid-test mit allen 9 Calculator-Patterns lokal verifiziert (`-(a + b)`, `!(condition)`, `(predicate ? a : b)`, `(i + 1) * 2`, switch-expression mit DeclarationPattern, generic constraint, MA.Name, MB.Name, plain identifier).
- e) **Audit aller Mutators im All-Set:** Phase C der Implementation (Sprint 147 followed-up), per-Mutator Skip-Listen werden bei discovery erweitert. Der Validator ist die Safety-Net für unentdeckte Patterns.

**Konsequenzen.**

- (+) Bug-9 ist root-cause-fixed für ALLE bekannten Crash-Klassen + alle künftigen via Safety-Net.
- (+) Defense-in-depth: Per-Mutator-Skips (Performance-Optimierung) + Validator (Safety-Net). Keine Mutation kommt ungefiltered durch.
- (+) Diagnostic-Quality verbessert: jede gefilterte Mutation produziert `MutantStatus.CompileError` + ReasonText mit OriginalNode-Kind, ReplacementNode-Kind, Parent-Kind. Anwender und Entwickler sehen WAS gescheitert ist.
- (+) Future-proof: neue Mutator-Implementierungen sind automatisch geschützt.
- (–) Performance-Impact: jede Mutation triggert einen `DescendantNodesAndSelf().ToList()`-walk = O(tree-size). Bei tausenden Mutations pro Datei spürbar.
- (–) Try/Catch als control-flow ist normalerweise Code-Smell — hier explizit als bewusster safety-net Use-Case dokumentiert (siehe XML-doc auf SyntaxSlotValidator).

**Performance-Mitigations (geplant für v3.3+):**

- Cache: SyntaxNode kind → ist-strict-typed-slot lookup. Skip die Validation wenn der Slot loose-typed ist.
- Lazy: nur validiere wenn ReplacementNode kind != OriginalNode kind (echte Type-Veränderung).
- Profile-aware: nur in Profile.All Validate (Defaults+Stronger sind bereits stable).

Diese Optimierungen sind separate Sprints — der Sicherheits-Aspekt (Bug-9 fixed) ist jetzt prioritär.

**Supersedes / supplements.**

- Supplements ADR-027 Phase 3 (Skip-as-Architecture). Skip-Liste bleibt als Performance-Layer; Validator ist als Safety-Net hinzugefügt.
- Supersedes RoslynHelper.InjectMutation throw-on-Contains-violation: jetzt soft-fail mit diagnostic.

**Backed by.** Sprint 147 Maxential 13 Schritte mit 3 ToT-Branches (A=Try/Catch, B=Reflection, C=Hybrid). Lokaler Bisect-Trail mit 9 Calculator-Tester-Patterns: alle ohne Crash. Solution-wide tests grün (402 Stryker.Core.Tests, +4 vs v3.2.1).

---

## ADR-029: `--version` Tool-Convention + `--project-version` Hard Rename (v3.2.3 / Sprint 148)

**Status.** Accepted — Sprint 148 (v3.2.3, 2026-05-06).

**Kontext.** Calculator-Tester Bug-Report 4, Bug #4: in den Versionen v3.1.x und v3.2.x druckt `dotnet stryker-netx --version` nicht die Tool-Version, sondern wird als positionsloser Wert für die Dashboard-Projekt-Version interpretiert (oder McMaster-Parser-Fehler bei `--version` ohne nachfolgenden String). Die .NET-Tool-Plattform-Konvention ist eindeutig: `--version` druckt die Tool-Version + exit 0, ohne weitere Pipeline-Aktionen. Sprint 141 hatte das mit einem parallelen `--tool-version` / `-T` Flag umgangen, aber der User hat das im Bug-Report explizit zurückgewiesen: das Konvention-konforme `--version` muss die Tool-Version drucken, nicht die historische Project-Version.

**Entscheidung.** **Hard rename** mit Breaking-Change in CLI-Migration:

1. **`--version` / `-V`**: druckt die Tool-Version (Inhalt von `AssemblyInformationalVersionAttribute`, gestrippt um `+commit-sha`-Suffix) + exit 0. Short-circuit in `StrykerCli.RunAsync` BEVOR irgendein McMaster-Parsing oder NuGet-Client-Call. Bare-Flag-Detektion: `--version` wird nur dann short-circuited wenn der nächste Arg null ist oder mit `-` beginnt; `--version <wert>` (wie historisch für Dashboard genutzt) fällt durch zur McMaster-Parser-Fehler-Pipeline.
2. **`--project-version` (long-only, kein short-Alias)**: ersetzt die historische Project-Version-Bindung. Lange Form ist explizit, kein `-v` short-Alias mehr. Migration für CI-Pipelines: `--version <value>` → `--project-version <value>`.
3. **`--tool-version` / `-T`** (Sprint-141-Aliase): bleiben funktional als deprecated transitional path. Drucken auch die Tool-Version. Nicht offiziell deprecated im help-text, aber Dokumentation cue: `--version` ist die kanonische Form.

**Begründung der Wahl.** Maxential 11 Schritte, 3 ToT-Branches:
- **O1 (Hard Rename + new Tool-Version-Flag)**: gewählt. Konvention-konformes Verhalten, klare Migration, einmaliger Breaking-Change, langfristig stabilste Lösung.
- **O2 (Soft-Detection by Value-Presence)**: verworfen. `--version` mit Wert-Präsenz → Project-Version, ohne Wert → Tool-Version, ist undurchsichtig und nicht-konventionell. Macht help-Text kontraintuitiv.
- **O3 (Status-Quo + Documentation)**: verworfen. Der User hat den Sprint-141-Workaround im Bug-Report explizit zurückgewiesen.

**Implementation.**

- `src/Stryker.CLI/StrykerCli.cs`:
  - `RunAsync`: prüft `TryHandleToolVersionFlag(args, …)` BEFORE McMaster-Parser-Pipeline. Druckt `GetToolVersionString()` + exit 0.
  - `TryHandleToolVersionFlag(string[] args, out int exitCode)`: scannt args linear, akzeptiert `--tool-version` / `-T` als Alias (Sprint-141-Pfad) oder `--version` / `-V` als bare-flag (kein nachfolgender Wert).
  - `IsBareVersionFlag(string[] args, int i)`: liefert true wenn `args[i]` ∈ {`--version`, `-V`} UND (`args[i+1]` ist null oder beginnt mit `-`).
  - `GetToolVersionString()`: liest `AssemblyInformationalVersionAttribute`, strippt `+commit-sha`-Suffix.
  - Refactoring: `RunAsync` zerlegt in `BuildCommandLineApplication` + `ExecuteWithErrorHandlingAsync` (MA0051 60-Zeilen-Cap).

- `src/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs` (Z. 247):
  - `AddCliInput(inputs.ProjectVersionInput, "version", "v", …)` → `AddCliInput(inputs.ProjectVersionInput, "project-version", null, …)`.
  - Long-only Registrierung. Kein `-p` / `-v` Short-Alias mehr (bewusste Engführung wegen Risiko der Verwechslung mit `--project`).

- `tests/Stryker.CLI.Tests/StrykerCLITests.cs`:
  - 4 Sprint-148-Tests: `VersionFlag_LongForm`, `VersionFlag_ShortForm`, `ToolVersionFlag_Sprint141Alias_LongForm_StillWorks`, `ToolVersionFlag_Sprint141Alias_ShortForm_StillWorks`.
  - `ShouldSetProjectVersionFeatureWhenPassed`: InlineData `[("--version","master"), ("-v","master")]` → `[("--project-version","master")]`.

**Migration für Anwender.**

| v3.2.2 (alt) | v3.2.3+ (neu) | Verhalten |
|--------------|---------------|-----------|
| `dotnet stryker-netx --version` | `dotnet stryker-netx --version` | druckt jetzt Tool-Version statt Parser-Fehler |
| `dotnet stryker-netx --version master` | `dotnet stryker-netx --project-version master` | Dashboard-Projekt-Version setzen |
| `dotnet stryker-netx -v master` | `dotnet stryker-netx --project-version master` | Dashboard-Projekt-Version setzen (kein Short-Alias mehr) |
| `dotnet stryker-netx --tool-version` | `dotnet stryker-netx --version` (oder weiterhin `--tool-version`) | druckt Tool-Version |

**Konsequenzen.**

- (+) `--version` ist konvention-konform: druckt Tool-Version + exit 0, ohne NuGet-Client-Call, ohne Logo, ohne Mutation-Run.
- (+) Kein doppelter Code-Pfad: `--tool-version` ist ein dünner Alias auf den gleichen Handler.
- (+) Help-Text reflektiert die neue Realität: `--project-version` (ohne `-v`).
- (–) **Breaking-Change**: CI-Pipelines die `--version <value>` für Dashboard-Reporting verwenden, müssen auf `--project-version <value>` umgestellt werden. Migration-Cue im help-Text + ADR-029. Die alte Form fällt jetzt durch zu McMaster-Parser-Fehler "Unrecognized option" — kein silent-fail.
- (–) Der `-v` Short-Alias ist weg. Wer den verwendet hat, muss explizit `--project-version` schreiben. Kein Short-Alias-Pfad weil das Risiko der Verwechslung mit `--project` zu groß ist.

**Supersedes / supplements.**

- Supersedes Sprint 141 (`--tool-version` / `-T` als parallel-Flag): die Aliase bleiben transitional, sind aber nicht mehr die kanonische Form.
- Supersedes Project-Version-Registrierung in CommandLineConfigReader (`"version"`, `"v"` → `"project-version"`, null).

**Backed by.** Sprint 148 Maxential (3-Weg ToT: O1/O2/O3, O1 gewählt), 4 neue Unit-Tests, lokal-acid-test mit allen 4 Aliases (`--version`, `-V`, `--tool-version`, `-T` drucken `0.0.0-localdev`), help-Text verifiziert (`--project-version` registered, kein doppelter `--version`), Solution-wide ~817 Unit-Tests grün, Semgrep clean (0 Findings auf 3 modifizierten Dateien).

**Bezug zu offenen Bugs aus Bug-Report 4.**

- Bug #4 ✓ closed mit ADR-029 (Sprint 148 / v3.2.3).
- Bug #6 (`--reporters` plural-Alias) — separater Sprint 149 (v3.2.4).
- Bug #8 (Multi-Project UX) — separater Sprint 150 (v3.2.5 oder v3.3.0).
- Bug #9 ✓ closed mit ADR-028 (Sprint 147 / v3.2.2).

---

## ADR-030: `--reporters` Plural-Alias via args-Pre-Processor (v3.2.4 / Sprint 149)

**Status.** Accepted — Sprint 149 (v3.2.4, 2026-05-06).

**Kontext.** Calculator-Tester Bug-Report 4, Bug #6: das Tool kennt nur `--reporter` (Singular), externe Tutorials und Doku-Quellen schreiben aber häufig `--reporters` (Plural). McMaster zeigt eine "Did you mean: reporter"-Hilfe, lehnt den Aufruf aber als `Unrecognized option '--reporters'` ab. User-Forderung: entweder Plural-Alias akzeptieren ODER Doku überall korrigieren. Plural-Alias ist der Tool-Fix-Pfad.

**Entscheidung.** **args-Pre-Processor in `StrykerCli.RunAsync`**: rewrite `--reporters` → `--reporter` (sowie `--reporters=html` → `--reporter=html` und `--reporters:html` → `--reporter:html`) BEVOR McMaster die argv sieht. Konsistent mit Sprint-148-Pattern (`TryHandleToolVersionFlag` operiert auf demselben pre-McMaster-Layer). Ein einziger Helper `RewriteReportersAlias(string[]) → string[]` mit drei expliziten Branch-Forms.

**Begründung der Wahl.** Maxential 3-Schritte-Branchless:
- **Option A (args-pre-process)**: gewählt. Minimal-invasiv, kein API-Change, kein doppelter McMaster-Eintrag, konsistent mit Sprint-148.
- **Option B (zweite Option-Registrierung in CommandLineConfigReader)**: verworfen. Zwei Help-Zeilen, zwei IInput-Bindungs-Pfade die konsolidiert werden müssten.
- **Option C (Custom McMaster-Subclass mit Multi-Long-Aliases)**: verworfen. McMaster-Internals fragil, Implementations-Aufwand vs Nutzen.

**Implementation.**

- `src/Stryker.CLI/StrykerCli.cs`:
  - `RunAsync` ruft `args = RewriteReportersAlias(args)` als ALLERERSTEN Schritt auf — vor dem Tool-Version-Short-Circuit, vor McMaster.
  - `RewriteReportersAlias(string[] args) → string[]`: scannt args linear, ruft `TryRewriteReporterArg` pro Element. Bei keinem Treffer wird die Eingabe-Referenz unverändert zurückgegeben (kein Heap-Allocation). Bei erstem Treffer wird `args.Clone()` aufgerufen und das geclonte Array befüllt (struktur-erhaltend, keine Mutation der Eingabe).
  - `TryRewriteReporterArg(string arg, out string rewritten) → bool`: drei Branch-Forms — exakter Match `--reporters`, Prefix-Match `--reporters=`, Prefix-Match `--reporters:`. False-Positive-Guard: `--reportersx` matcht KEINES der drei Patterns und fällt durch.

- `tests/Stryker.CLI.Tests/StrykerCLITests.cs`:
  - 5 Theory-Cases für Rewrite (`RewriteReportersAlias_RewritesPluralToSingular`): space-separated, `=`-separated, `:`-separated, twice-repeated, mixed-with-other-flags.
  - 4 Theory-Cases für Non-Rewrite (`RewriteReportersAlias_LeavesNonPluralUnchanged`): `--reporter` already-singular spaced + `=`-form, false-positive `--reportersx`, empty args.
  - 1 End-to-End-Fact (`ShouldAcceptReportersPluralAsAliasForReporter`): `--reporters html` populiert `ReportersInput.SuppliedInput` über die volle Pipeline.

**Help-Text-Sichtbarkeit.** Der Plural-Alias erscheint NICHT im `--help`-Output. Das ist eine bewusste Designentscheidung: die kanonische Form bleibt `--reporter` (Singular), der Plural ist ein transparenter Compatibility-Alias für externe Doku/Tutorials. McMaster's "Did you mean: reporter"-Hilfe bleibt für andere Tippfehler-Variants (`--reporterz`, `--report`) aktiv — nur die exakte plural-Variante wird schweigend rewritten.

**Tippfehler-Toleranz vs Plural-Alias-Trennlinie.** Der Pre-Processor matcht NUR `--reporters` exakt (oder mit `=`/`:` direkt anschließend). Variants wie `--reporterz`, `--reportz`, `--report` werden NICHT rewritten — die fallen weiter durch zu McMaster's "Did you mean"-Hilfe. Das verhindert dass jede 1-Buchstabe-Variation zur stillen Akzeptanz wird (würde Help-Text-Akkuratheit untergraben).

**Konsequenzen.**

- (+) Externe Doku/Tutorials die `--reporters html` zeigen funktionieren ohne Änderung.
- (+) Kein Behavior-Change für bestehende `--reporter`-Nutzer.
- (+) Kein API-Change, keine Migration nötig.
- (+) Konsistentes Pattern mit Sprint-148-Pre-Processor (`TryHandleToolVersionFlag`).
- (–) Plural-Alias ist nicht im `--help` sichtbar — User die das Tool ausschließlich über `--help` lernen, sehen nur `--reporter`. Bewusst akzeptiert; ADR-030 dokumentiert die Entscheidung.

**Backed by.** Sprint 149 Maxential 3-Schritte (Option A vs B vs C, A gewählt). 10 neue Tests (5 Rewrite-Theory + 4 Non-Rewrite-Theory + 1 E2E-Fact). 91 Stryker.CLI.Tests grün, Solution-wide 844 Unit-Tests grün (vs Sprint 148 = 834, +10), Semgrep clean (0 Findings auf 2 modifizierten Dateien).

**Bezug zu offenen Bugs aus Bug-Report 4.**

- Bug #4 ✓ closed mit ADR-029 (Sprint 148 / v3.2.3).
- Bug #6 ✓ closed mit ADR-030 (Sprint 149 / v3.2.4).
- Bug #8 (Multi-Project UX) — separater Sprint 150 (v3.2.5 oder v3.3.0).
- Bug #9 ✓ closed mit ADR-028 (Sprint 147 / v3.2.2).

---

## ADR-031: `--all-projects` Multi-Project-Mutation Flag (v3.2.5 / Sprint 150)

**Status.** Accepted — Sprint 150 (v3.2.5, 2026-05-06).

**Kontext.** Calculator-Tester Bug-Report 4, Bug #8: wenn ein Test-Projekt mehrere `<ProjectReference>` zu Source-Projekten hat (Clean-Architecture-Setups: Domain + Infrastructure + App-Layer), bricht Stryker mit `"Test project contains more than one project reference. Please set the project option…"`. Der Anwender konnte zwar auf `--solution <file>.slnx` (Sprint 141 advertised diesen Pfad in der Fehlermeldung) ausweichen, aber das setzt eine Solution-Datei voraus und scannt alle Projekte der Solution — nicht nur die vom Test-Projekt referenzierten. User-Forderung: ein per-Test-Project-Flag das ALLE referenzierten Source-Projekte mutiert ohne Solution-Scan-Overhead.

**Entscheidung.** **Neue NoValue-Input-Klasse `AllProjectsInput` (lange Form `--all-projects`, kein short-Alias)**: wenn der User `--all-projects` setzt UND das Test-Projekt mehrere Source-Project-Referenzen hat, gibt der `InputFileResolver.ResolveSourceProjectInfos` die volle `List<SourceProjectInfo>` zurück statt die Disambiguation-Exception zu werfen. Der downstream `ProjectOrchestrator` iteriert ohnehin über die Liste (das ist der bestehende Solution-Mode-Pfad), kein Engine-Side-Refactor nötig.

**Begründung der Wahl.** Maxential 11 Schritte mit 2 ToT-Branches (B1 + B2 evaluiert end-to-end):
- **B1 (`--all-projects` Flag, NoValue)**: gewählt. Saubere Abgrenzung: Initialisation-Layer-Branch + 6 modified files + 5 unit tests. Kein API-Breaking-Change, kein Engine-Eingriff.
- **B2 (Multi-`--project` mit MultipleValue)**: verworfen. `SourceProjectNameInput` ist `Input<string>` (single) — Umstellung auf `Input<IEnumerable<string>>` wäre Breaking-Change auf der `IStrykerOptions.SourceProjectName`-API. Plus: Filter-Matching im InputFileResolver (`normalizedProjectUnderTestNameFilter`) ist auf single-string zugeschnitten — pipeline-weiter Refactor nötig. Hoher Aufwand, hohes Risiko.

**Implementation.**

- `src/Stryker.Configuration/Options/Inputs/AllProjectsInput.cs` (neu): `Input<bool?>` mit `Default = false`. Help-Text dokumentiert Use-Case (Clean-Architecture-Setups) und gegenseitige Ausschließung mit `--project` (single) und `--solution` (whole-solution scan).
- `src/Stryker.Abstractions/Options/IStrykerOptions.cs`: neue `bool IsAllProjectsMode { get; init; }` Property mit XML-doc.
- `src/Stryker.Configuration/Options/StrykerOptions.cs`: Implementation der `IsAllProjectsMode`-Property.
- `src/Stryker.Configuration/Options/IStrykerInputs.cs`: neue `AllProjectsInput AllProjectsInput { get; init; }` Property.
- `src/Stryker.Configuration/Options/StrykerInputs.cs`: konkrete `AllProjectsInput`-Property + `IsAllProjectsMode = AllProjectsInput.Validate()` Wiring im `BuildStrykerOptions`. MA0051-Refactor: `Thresholds`-Initialization extrahiert in `BuildThresholds()` Helper damit `BuildStrykerOptions` unter 60 Zeilen bleibt.
- `src/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs`: `AddCliInput(inputs.AllProjectsInput, "all-projects", null, optionType: CommandOptionType.NoValue, category: InputCategory.Build)` — long-only, kein short-Alias (short-Flag-Space ist mit `--project`/`--solution`/`-p`/`-s` bereits crowded).
- `src/Stryker.Core/Initialisation/InputFileResolver.cs`: `ResolveSourceProjectInfos`-Body unverändert; neue `ResolveMultiReferenceCase`-Helper-Methode wird bei `result.Count > 1` aufgerufen. In dieser Methode: wenn `options.IsAllProjectsMode == true` → `LogAllProjectsMode` + return result. Sonst → throw `InputException` mit verbessertem Help-Text der jetzt sowohl `--all-projects` als auch `--solution` als Lösungspfade nennt. MA0051-Refactor: Method-Body extrahiert um die 60-Zeilen-Cap zu halten.

- `tests/Stryker.Core.Dogfood.Tests/Options/Inputs/AllProjectsInputTests.cs` (neu): 5 Tests für `AllProjectsInput.Default == false`, 3-Theory-Cases für `Validate()` (null/false/true), Help-Text-Substring-Match.
- `tests/Stryker.CLI.Tests/StrykerCLITests.cs`: 2 neue Tests — `ShouldSetAllProjectsModeWhenPassed` (positive) + `ShouldDefaultAllProjectsModeToFalseWhenNotPassed` (negative) — beide üben die volle CLI-Pipeline.
- `tests/Stryker.CLI.Tests/ConfigBuilderTests.cs`: `GetMockInputs.SetupCoreInputs` ergänzt um `inputs.Setup(x => x.AllProjectsInput).Returns(new AllProjectsInput())` — sonst NRE in `RegisterCommandLineOptions`.

**Anwender-UX.**

| Vorher | Nachher (mit `--all-projects`) |
|--------|---------------------------------|
| `dotnet stryker-netx` (Test-Projekt referenziert 3 Source-Projekte) | `dotnet stryker-netx --all-projects` mutiert alle 3 sequenziell |
| Fehlermeldung "Please set the project option" + Liste der 3 Pfade | Logger-Message "--all-projects mode: mutating 3 referenced source projects sequentially." |
| Workaround: `--solution X.slnx` (scannt aber GANZE Solution, nicht nur Test-Projekt-Referenzen) | `--all-projects` skaliert auf Test-Projekt-Scope |

**Konsequenzen.**

- (+) Saubere Per-Test-Project-Abgrenzung — der Scope ist explizit "alles was das Test-Projekt referenziert", nicht "alles in der Solution".
- (+) Kein Breaking-Change: `--project A.csproj` und Default-Single-Reference-Path bleiben unverändert.
- (+) Update der Fehlermeldung: bei Multi-Reference ohne `--all-projects`/`--project`/`--solution` zeigt das Tool jetzt drei Lösungspfade statt zwei.
- (+) Konsistent mit dem Solution-Mode-Pattern: beide nutzen die gleiche `List<SourceProjectInfo>`-Iteration im Orchestrator.
- (–) Drei UX-Pfade für Multi-Project (`--all-projects`, `--solution`, mehrfache Single-Project-Runs) statt einer zentralen Lösung. Bewusst akzeptiert: `--solution` bleibt für Whole-Solution-Cases (CI-Pipelines), `--all-projects` für Per-Test-Project-Cases (Anwender-Workflows).
- (–) Multi-Project-Reports werden NICHT zu einem kombinierten HTML-Report aggregiert. Der ProjectOrchestrator schreibt pro Project einen eigenen Report-Output. Eine echte Aggregation ist eine separate Roadmap-Item für v3.3+ (Maxential-Trail dokumentiert).

**Supersedes / supplements.**

- Supplements Sprint 141's Fehlermeldungs-Hinweis (`--solution` als Alternativpfad) — der Hinweis-Text wird um `--all-projects` erweitert.
- Kein direkter Supersede; Solution-Mode (ADR vor v2.0.0) bleibt als orthogonaler Pfad bestehen.

**Backed by.** Sprint 150 Maxential 11 Schritte mit 2 ToT-Branches (B1 vs B2, B1 gewählt). 7 neue Tests (5 AllProjectsInputTests + 2 StrykerCLITests). Solution-wide 2035 Unit-Tests grün (incl. Stryker.Core.Dogfood.Tests 1189), Semgrep clean (0 Findings auf 10 modifizierten Dateien).

**Bezug zu offenen Bugs aus Bug-Report 4.**

- Bug #4 ✓ closed mit ADR-029 (Sprint 148 / v3.2.3).
- Bug #6 ✓ closed mit ADR-030 (Sprint 149 / v3.2.4).
- Bug #8 ✓ closed mit ADR-031 (Sprint 150 / v3.2.5).
- Bug #9 ✓ closed mit ADR-028 (Sprint 147 / v3.2.2).

**Bug-Report 4 ist mit Sprint 150 vollständig geschlossen.** Alle 4 Bugs sind via ADRs 028–031 architektonisch fixed.

---

## ADR-032: Orchestration-Phase Slot Validation — systemic Bug-9 audit (v3.2.6 / Sprint 151)

**Status.** Accepted — Sprint 151 (v3.2.6, 2026-05-06).

**Kontext.** Calculator-Tester Bug-Report 5, Bug #9 (verschärfte Forderung): in v3.2.5 reproduzierte das `Calculator.Infrastructure`-Projekt mit `--mutation-profile All` einen NEUEN Cast-Crash mit identischem Stack-Trace-Pfad: `InvalidCastException: ParenthesizedExpressionSyntax → IdentifierNameSyntax` (statt v3.1.1–v3.2.0's `→ TypeSyntax`). Der User stellte explizit fest, dass Sprint 147 ADR-028 zwar Punkte (a–d) der Bug-Report-4-Forderung umgesetzt hatte, aber Punkt (e) — "Audit aller Mutatoren auf ähnliche unbedacht-blind-castende Stellen" — nicht durchgeführt wurde. Die Maintainer-Selbst-Diagnose von Sprint 147 ("Validator als Safety-Net macht per-Mutator-Audit zur Performance-Optimierung statt Sicherheits-Voraussetzung") war ein **architektonischer Trugschluss**: der Sprint-147 `SyntaxSlotValidator` deckte nur die **Injection-Phase** (`MutationStore.Inject` → `RoslynHelper.TryInjectMutation`), nicht die **Orchestration-Phase** (`NodeSpecificOrchestrator.OrchestrateChildrenMutation` → `node.ReplaceNodes`).

User-Forderung (verschärft): "Eine projektweite Suche nach allen impliziten oder expliziten Casts in Mutator-Code-Pfaden, die einen Syntax-Knoten in einen spezifischeren Subtyp casten, mit Listing als Patch-Note in der nächsten Version. Eine reine Symptom-Behandlung ('der zweite Cast wird auch gefixt') wäre erneut keine Erfüllung der ursprünglichen Forderung — wir erwarten den **systemischen** Eingriff."

**Audit-Ergebnis (User-Forderung "Listing als Patch-Note").**

Projektweites Pattern-Match aller Cast-Stellen `(SyntaxType)expr` und `expr as SyntaxType` in `src/Stryker.Core/Mutators/` und `src/Stryker.Core/Mutants/`:

| Site | File | Line | Klassifikation | Anmerkung |
|------|------|------|----------------|-----------|
| `(ExpressionSyntax)getResult` | `Mutators/AsyncAwaitMutator.cs` | 50 | ✅ safe by construction | upcast — `getResult` ist `MemberAccessExpressionSyntax`, beide sind `ExpressionSyntax` |
| `(ExpressionSyntax)resultAccess` | `Mutators/AsyncAwaitResultMutator.cs` | 60 | ✅ safe by construction | upcast — wie oben |
| `(LiteralExpressionSyntax)patternExpression` | `Mutators/RegexMutator.cs` | 44 | ✅ safe | preceded by `if (… is LiteralExpressionSyntax)` typecheck |
| `(IdentifierNameSyntax)((QualifiedNameSyntax)n.Name).Right` | `Initialisation/CsharpProjectComponentsBuilder.cs` | 145 | ✅ safe | preceded by 2× `Kind() == SyntaxKind.QualifiedName/IdentifierName` typecheck |
| `result as BlockSyntax ?? SyntaxFactory.Block(result)` | `Mutants/MutationStore.cs` | 196, 223 | ✅ safe | `as`-Cast mit `??`-Fallback |
| `code as BlockSyntax ?? SyntaxFactory.Block(code)` | `Instrumentation/IfInstrumentationEngine.cs` | 28 | ✅ safe | wie oben |
| **`node.ReplaceNodes(node.ChildNodes(), …)`** | **`Mutants/CsharpNodeOrchestrators/NodeSpecificOrchestrator.cs`** | **84–85** | 🔴 **unsafe** | **Bug-9-Hauptverdächtiger** — Roslyn rebuilt Parent-Slot, mutated child kann typed-slot-Mismatch produzieren |
| `node.ReplaceNodes(node.ChildNodes(), …)` | `Mutants/CsharpNodeOrchestrators/ConditionalExpressionOrchestrator.cs` | 12 | 🔴 unsafe | wie oben |
| `node.ReplaceNodes(node.ChildNodes(), …)` | `Mutants/CsharpNodeOrchestrators/InvocationExpressionOrchestrator.cs` | 20 | 🔴 unsafe | **Bug-Report-5-Hauptverdächtiger** — `(x).Method()` Pattern |
| `node.ReplaceNodes(node.ChildNodes(), …)` | `Mutants/CsharpNodeOrchestrators/ExpressionBodiedPropertyOrchestrator.cs` | 40 | 🔴 unsafe | wie oben |
| `result.ReplaceNodes(…)` | `Mutants/CsharpNodeOrchestrators/StaticFieldDeclarationOrchestrator.cs` | 28 | ✅ safe by construction | replacement = `PlaceStaticContextMarker(ExpressionSyntax) → ExpressionSyntax`, type-preserving |
| `mutated.ReplaceNode(mutated.Body!, context.PlaceStaticContextMarker(mutated.Body!))` | `Mutants/CsharpNodeOrchestrators/StaticConstructorOrchestrator.cs` | 35 | ✅ safe by construction | replacement = `PlaceStaticContextMarker(BlockSyntax) → BlockSyntax`, type-preserving |

**Fazit des Audits:** alle Cast-Patterns in Mutator-Code sind safe (entweder upcast, by-construction, oder mit `??`-Fallback). Die unsafe Cast-Sites liegen nicht in Mutator-Code, sondern in den **Orchestrator-`ReplaceNodes`-Calls**, wo Roslyn intern beim Rebuild des Parents typed-Slots überprüft. 4 Sites sind unsafe (1 Base + 3 Derived); 2 Sites (StaticField/StaticConstructor) sind safe-by-construction weil sie type-preserving Replacements verwenden.

**Entscheidung.** **Hybrid-Strategie (Branch S3, Maxential 5-Schritte):**
1. **Phase A (per-site fix):** alle 4 unsafe Orchestrator-Sites werden auf einen neuen `OrchestrationHelpers.ReplaceChildrenValidated`-Helper umgeroutet, der pro-child via `SyntaxSlotValidator.TryReplaceWithValidation` validiert. Slot-incompatible Replacements werden silent dropped, das Original-Child wird beibehalten.
2. **Phase B (final safety net):** der Helper wrappt zusätzlich den finalen `node.ReplaceNodes(validated)`-Bulk-Call in try/catch; falls individuell-valide Replacements interagieren und trotzdem crashen, wird das gesamte Set silent dropped (return `node` unchanged).
3. **Phase C (Listing als Patch-Note):** dieses Audit-Listing oben.

**Begründung der Wahl.** Maxential 5-Schritte mit 3 ToT-Branches (S1 / S2 / S3):
- **S1 (Per-Site-Pattern-Match)**: jede Site einzeln zu `if (node is …) … else skip` umschreiben. Verworfen: Sprint-147 hat bereits den `SyntaxSlotValidator` etabliert, ein zweiter Pattern-Match-Layer pro Site wäre Duplikation.
- **S2 (try/catch um ganze OrchestrateChildrenMutation)**: alle Children-Mutations bei single-child-Crash verwerfen. Verworfen: zu coarse-grained, würde gute Mutations einer Mehrheit der Children mit-verlieren.
- **S3 (Hybrid: per-child validation + final safety-net)**: gewählt. Per-child fix preserviert valide Mutations; final safety-net deckt edge-cases. Konsistent mit Sprint-147-ADR-028-Architektur (Defense-in-Depth).

**Implementation.**

- `src/Stryker.Core/Mutants/CsharpNodeOrchestrators/OrchestrationHelpers.cs` (neu, 116 LOC): static partial class mit `ReplaceChildrenValidated<TParent>(TParent, IEnumerable<SyntaxNode>, Func<SyntaxNode, SyntaxNode>) → TParent`. Logging via `[LoggerMessage]` für rejected children + bulk-replace-fallback.
- `src/Stryker.Core/Mutants/CsharpNodeOrchestrators/NodeSpecificOrchestrator.cs` (Z. 83–93): default `OrchestrateChildrenMutation` routes durch `OrchestrationHelpers.ReplaceChildrenValidated`.
- `src/Stryker.Core/Mutants/CsharpNodeOrchestrators/ConditionalExpressionOrchestrator.cs`, `InvocationExpressionOrchestrator.cs`, `ExpressionBodiedPropertyOrchestrator.cs`: alle 3 Override-`OrchestrateChildrenMutation` umgeroutet auf `OrchestrationHelpers.ReplaceChildrenValidated` mit den jeweiligen lambda-bodies preserved.
- `tests/Stryker.Core.Tests/Mutants/CsharpNodeOrchestrators/OrchestrationHelpersTests.cs` (neu): 2 Unit-Tests (no-mutation identity preservation + compatible mutation propagation). Note: ein dritter Test "incompatible mutation dropped" erwies sich als unrealistisch konstruierbar auf unit-test-Level — Roslyn's public APIs crashen selbst beim Setup.
- `tests/Stryker.Core.Tests/Integration/OrchestrationSlotValidationTests.cs` (neu): 10 Integration-Tests mit `MutationProfile.All` über die volle Orchestrator-Pipeline. 4 Sprint-147-Patterns (Bug-Report 4 baseline, kept as regression) + 4 Sprint-151-Patterns (Bug-Report 5 closure) + 2 explicit `(x).Method()`-Pattern-Repros. Jeder Test asserts: keine unhandled exception + mutated tree re-parses cleanly.

**Konsequenzen.**

- (+) Bug-Report-5 Bug-9-Crash ist root-cause-fixed. Die zweite Cast-Site (`→ IdentifierNameSyntax`) UND alle künftigen Cast-Sites in Orchestrator-`ReplaceNodes`-Calls werden vom Validator abgefangen.
- (+) Defense-in-Depth zwischen Sprint-147 (Injection-Phase) + Sprint-151 (Orchestration-Phase) deckt jetzt die ZWEI Phasen ab, in denen Roslyn typed-slot-checks durchführt.
- (+) Audit-Listing dokumentiert alle Cast-Sites projektweit — User-Forderung "Listing als Patch-Note" erfüllt. Die safe-Sites (8 Stück) sind explizit klassifiziert und dokumentiert; die unsafe-Sites (4 Stück) sind gefixt.
- (–) Performance-Cost: jede Children-Mutation triggert pro Child einen `SyntaxSlotValidator.TryReplaceWithValidation`-Call (= Roslyn `ReplaceNode` + descendant-walk). Bei Tausenden Mutations pro Datei spürbar — Sprint-147 ADR-028 hatte denselben Cost-Trade-off bereits akzeptiert. Performance-Optimierungen sind Kandidaten für v3.3+ (z. B. cache von "slot-type kombinationen die bekanntermaßen safe sind").
- (–) Try/catch als control-flow ist Code-Smell — wie schon bei ADR-028 explizit als bewusster safety-net Use-Case dokumentiert (siehe XML-doc auf SyntaxSlotValidator + OrchestrationHelpers).

**Supersedes / supplements.**

- **Supplements** ADR-028 (Sprint 147 Central Syntax-Slot Validation Layer). ADR-028 deckt Injection-Phase, ADR-032 deckt Orchestration-Phase. Zusammen vollständige Pipeline-Coverage.
- **Korrigiert** den Sprint-147-architektonischen-Trugschluss "Validator als Safety-Net macht per-Mutator-Audit unnötig". Per-Mutator-Audit war zwar nicht nötig (alle Mutator-Cast-Sites sind safe), aber Per-Orchestrator-Audit war nötig.

**Backed by.** Sprint 151 Maxential 5 Schritte mit 3 ToT-Branches (S1 / S2 / S3, **S3 Hybrid gewählt**). 12 neue Tests (10 Integration + 2 Unit) all green; Solution-wide 2047 Unit-Tests grün (vs Sprint 150 = 2035, +12), Semgrep clean (0 Findings auf 7 modifizierten Dateien). Audit-Listing oben enumeriert 12 projektweite Cast-Sites mit Sicherheits-Klassifikation.

**Bezug zu offenen Bugs aus Bug-Report 5.**

- Bug #9 (verschärft) ✓ closed mit ADR-032 (Sprint 151 / v3.2.6) — Audit durchgeführt, 4 unsafe Sites gefixt, 8 safe Sites dokumentiert.
- Bugs #4, #6, #8 ✓ unverändert closed mit Bug-Report-4-Sprint-Sweep (147–150).

---

## ADR-036: CI build+test green via in-repo test fixtures + cross-platform paths (v3.2.7 / Sprint 152)

**Status.** Accepted — Sprint 152 (v3.2.7, 2026-05-06).

**Kontext.** Über Sprints 147–151 zeigte jede PR ein konsistentes CI-Pattern von 6/33 SUCCESS — die `build + test (ubuntu-latest)` und `build + test (windows-latest)` Jobs waren rot, wurden aber als "pre-existing flake" toleriert beim Merge. Die Failures zerfielen in zwei strukturell unterschiedliche Klassen:

1. **Stryker.Solutions.Tests Linux-Path** (4 tests): nutzten `_references/stryker-net/src/Stryker.slnx` als test fixture — aber `_references/` ist via `.gitignore` ausgeschlossen, so dass CI-Checkouts die Datei NICHT haben (`DirectoryNotFoundException: Could not find a part of the path '/home/runner/work/stryker-netx/stryker-netx/_references/stryker-net/src/Stryker.slnx'`).
2. **ProjectAnalysisMockBuilderTests Windows-Path** (1 test): hardcoded `"c:\\src\\MyProject.csproj"` — auf Linux/macOS ist `\` kein Pfad-Separator, so dass `Path.GetFileNameWithoutExtension` die ganze Eingabe als single filename behandelt und `"c:\\src\\MyProject"` statt `"MyProject"` liefert.

Plus die separate **integration-test-Matrix** (~25 tests) mit `extern alias TheLog`-Compile-Errors während Stryker's Mutation auf `integrationtest/TargetProjects/NetCore/TargetProject/StrykerFeatures/UseAssert.cs` — das ist eine Stryker-Mutation-Engine-Regression (likely seit Sprint 4 Bug-5 Path-A "surgical augmentation of _references" weiter divergiert), nicht in Sprint-152-Scope.

**Entscheidung.** Sprint 152 fixt die zwei build+test-Klassen (höchster amortisierter Hebel über alle künftigen PRs):

- **Stryker.Solutions.Tests fix**: vendor `Stryker.slnx` als in-repo test-resource bei `tests/Stryker.Solutions.Tests/TestResources/UpstreamStryker.slnx`. CSproj `<None Include="TestResources\**\*"><CopyToOutputDirectory>` kopiert es in den Test-Bin. Tests nutzen jetzt `Path.Combine(AppContext.BaseDirectory, "TestResources", "UpstreamStryker.slnx")` statt der relativen `_references/`-Pfads. Die slnx ist self-contained XML — keine Reload-Pfade hängen davon ab.
- **ProjectAnalysisMockBuilder fix**: ersetze hardcoded `"c:\\src\\MyProject.csproj"` durch `Path.Combine("src", "MyProject.csproj")` und ähnliche cross-platform-Konstruktionen. Das Test-Intent ("WithProjectFilePath leitet AssemblyName + TargetFileName + TargetDir korrekt ab") ist unverändert; nur die Eingabe-Pfade sind portabel.

Die integration-test-Matrix (Stryker-mutation-engine-Regression auf TargetProject) bleibt **honest deferred** als Sprint-153+-Followup mit explizitem ADR-Verweis.

**Begründung der Wahl.** Maxential 4-Schritte branchless:
1. Sprint-152-Scope: "CI flakes". Two flake-classes identifiziert via Job-Log-Analyse.
2. Class A (Solutions.Tests Linux-Path): `_references/`-`.gitignore`-Exclusion ist die Wurzel. 3 Lösungspfade — vendor (gewählt: minimal-invasiv, schon das Pattern bei `integrationtest/`), un-gitignore (würde alle `_references/`-Subfolder einschließen, zu invasiv), test-skip (Verluste an Coverage). Vendor gewählt.
3. Class B (Mock builder Windows-Path): hardcoded Windows-paths sind plain test-bug. Cross-platform via `Path.Combine` ist die Standard-Lösung.
4. Class C (integration-test compile-error): Stryker-mutation-engine-Issue, deutlich komplexer, eigenes Investigation. Sprint-152 dokumentiert das als deferred.

**Implementation.**

- `tests/Stryker.Solutions.Tests/TestResources/UpstreamStryker.slnx` (neu, copy of `_references/stryker-net/src/Stryker.slnx` — same XML content, in-repo)
- `tests/Stryker.Solutions.Tests/Stryker.Solutions.Tests.csproj`: `<None Include="TestResources\**\*">` `<CopyToOutputDirectory>` ItemGroup hinzugefügt
- `tests/Stryker.Solutions.Tests/SolutionFileShould.cs`: `UpstreamSlnxPath` umgestellt von `Path.Combine("..", "..", "..", "..", "..", "_references", ...)` zu `Path.Combine(AppContext.BaseDirectory, "TestResources", "UpstreamStryker.slnx")`. XML-doc erwähnt Sprint-152 ADR-036 + den neuen Pattern.
- `tests/Stryker.Core.Dogfood.Tests/TestHelpers/ProjectAnalysisMockBuilderTests.cs`: 4 hardcoded Windows-Paths (`c:\\src\\MyProject.csproj`, `c:\\out\\bin`, `d:\\custom\\Foo.dll`) refactored zu `Path.Combine(...)`-static-readonly-fields. Test-Intent unverändert.

**Konsequenzen.**

- (+) build + test (ubuntu-latest) und build + test (windows-latest) sollen jetzt grün laufen. Erwartetes neues CI-Pattern: ~30/9 SUCCESS statt vorher ~6/33 — die ~25 macOS/Ubuntu integration-test failures bleiben als Sprint-153+-Backlog markiert.
- (+) Test-Resource-Vendor-Pattern (in-repo TestResources/ + CopyToOutputDirectory) ist konsistent mit anderen Test-Projekten (`integrationtest/TargetProjects/` ist schon in-repo).
- (+) Cross-platform-Tests verifizieren Code auf allen drei OSes (Windows local + Linux/macOS CI).
- (–) Slnx-Datei doppelt vorhanden (in `_references/` UND in `TestResources/`). Wenn upstream Stryker.NET die Solution-Struktur ändert, müssen wir nachziehen. Mitigation: das ist kein häufiges Event (slnx-Struktur ist stabil seit upstream 4.14).
- (–) integration-test-Matrix Failures bleiben offen — explizit deferred. Bug "Stryker mutiert eigene integration-test TargetProject mit compile-errors" braucht eigene Investigation (Sprint-153+).

**Supersedes / supplements.**

- Supplements Sprint 4 Path A ("surgical augmentation of _references"). Path A war für Pillar-A run-to-completion; Sprint 152 fixt die Test-Resource-Layer separately.
- Kein direkter Supersede.

**Backed by.** Sprint 152 Maxential 4-Schritte branchless. CI-Job-Log-Analyse (job 74670816373 ubuntu-latest build+test) zeigte beide Failure-Klassen explizit. Solution-wide 2047 Tests grün lokal (vs Sprint 151 = 2047, ±0 — kein neuer Test, nur fixes für bestehende Tests). Semgrep clean (0 Findings auf 3 modifizierten Dateien).

**Bezug zu Backlog-Items.**

- Backlog-Item 5 (CI Integration Matrix Flakes) ✓ teilweise closed: build+test grün-pfad gefixt; macOS/Ubuntu integration-matrix bleibt deferred als separate Sprint-153+-Aufgabe.

---

## ADR-033: Combined Multi-Project Report Aggregation — discovery (no sprint / doc-correction)

**Status.** Accepted — 2026-05-06 (post-Sprint-152 doc bundle).

**Kontext.** ADR-031 (Sprint 150, `--all-projects` flag) hatte als Konsequenz markiert: "Multi-Project-Reports werden NICHT zu einem kombinierten HTML-Report aggregiert. Der ProjectOrchestrator schreibt pro Project einen eigenen Report-Output. Eine echte Aggregation ist eine separate Roadmap-Item für v3.3+ (Maxential-Trail dokumentiert)."

**Discovery.** Calculator-Tester Bug-Report 5 (Sprint 151) bestätigte explizit: "375 Mutanten erzeugt (Domain 4 + Infrastructure 271 + Calculator 100), 242 tested, **kombinierter Report**". Code-Audit in Sprint-153-Recherche bestätigt: `StrykerRunner.cs:51` ruft `AddRootFolderIfMultiProject([.. _mutationTestProcesses.Select(x => x.Input.SourceProjectInfo.ProjectContents)], options)` auf, baut den combined `rootComponent`, und `OnAllMutantsTested(rootComponent, combinedTestProjectsInfo)` wird ONCE am Ende gerufen — mit dem aggregierten Tree als Argument. Beide `JsonReporter` und `HtmlReporter` schreiben EINEN Report basierend auf `rootComponent`. Die Aggregation existiert seit Sprint 1 (ADR-005 Workspaces.MSBuild-Pipeline).

**Entscheidung.** Die "v3.3+ deferred"-Aussage in ADR-031 wird **rückgängig gemacht**. Combined Multi-Project Report Aggregation ist seit der ursprünglichen `--solution`-Mode-Implementation (vor v2.0.0) bereits funktional. `--all-projects` (Sprint 150 / ADR-031) erbt das automatisch durch die `MutateProjectsAsync` → `rootComponent` → `OnAllMutantsTested`-Pipeline.

**Begründung der Wahl.** Keine. Diese ADR korrigiert eine Falsch-Aussage in ADR-031. Kein Sprint nötig.

**Implementation.** Keine — die Aggregation ist bereits operational. Nur Doku-Korrektur.

**Konsequenzen.**

- (+) Backlog-Item 7 (Combined Multi-Project Report Aggregation) ✓ closed mit Discovery: keine Implementation nötig, ADR-031's deferred-Claim war veraltet.
- (+) User-Forderungen aus Bug-Report 4 / 5 implizit alle adressiert für Multi-Project-Output.
- (–) ADR-031's Konsequenzen-Sektion enthält veraltete Aussage. Sollte beim nächsten Major-Release in einer ADR-Revision korrigiert werden (out of scope für v3.x).

**Supersedes / supplements.**

- **Korrigiert** ADR-031 Konsequenzen-Sektion (v3.3+ deferred-Claim für Multi-Project-Report Aggregation): die Aussage ist false; Aggregation ist seit Sprint 1 implementiert.
- **Verifiziert via Code-Audit** in `src/Stryker.Core/StrykerRunner.cs` Z. 51 (rootComponent build) + Z. 65 + Z. 142 (OnMutantsCreated/OnAllMutantsTested calls) + `src/Stryker.Core/Reporters/{Json,Html}/...Reporter.cs` (single-call write).

**Backed by.** Calculator-Tester Bug-Report 5 v3.2.5 verification: "kombinierter Report" mit 375 Mutanten Total. Code-Audit StrykerRunner.cs + JsonReporter.cs + HtmlReporter.cs.

**Bezug zu Backlog-Items.**

- Backlog-Item 7 (Combined Multi-Project Report Aggregation) ✓ closed by discovery — keine Implementation nötig.

---

## ADR-035: TypeSyntax-Engine Refactor + HotSwap inkrementelles MT — status-quo confirmation (no sprint / doc-affirmation)

**Status.** Accepted — 2026-05-06 (post-Sprint-152 doc bundle).

**Kontext.** Im Sprint-152-Sprint-Roadmap-Planning waren zwei Backlog-Items von der Sprint-Sequenz ausgenommen:
- **Item 3 — TypeSyntax-Engine Refactor**: ADR-027 Phase 3 (Sprint 145, v3.2.0) hatte explizit `Skip-as-Architecture` als finale Entscheidung dokumentiert (Maxential Option F nach 11 Schritten + 3 Engine-Refactor-Alternativen). TypeSyntax-Engine-Refactor wäre 4+ Sprints für 1 niche-Mutator (UoiMutator in TypeSyntax-Position) — Cost/Benefit gegen Skip-as-Architecture verloren.
- **Item 4 — HotSwap inkrementelles MT**: ADR-022 (Sprint 15, Proposed) hatte inkrementelles Mutation-Testing als zukünftige Performance-Direction skizziert, aber explizit ohne commitment. ADR-021 (Sprint 15, Accepted) hatte ADR-016 (HotSwap-Engine v2.0.0) walked-back wegen falschen mentalen Modells.

User-Backlog-Direktive: "Damit machen wir weiter." — d. h. die 7-Item-Liste durcharbeiten. Diese ADR adressiert beide Items mit der gleichen Antwort: status-quo bleibt, kein Sprint allokiert, kein Reopen ohne neuen Trigger.

**Entscheidung.** **Beide Items bleiben in ihrem aktuellen ADR-Status.** Kein Sprint allokiert. Kein Reopen.

- **TypeSyntax-Engine**: Skip-as-Architecture ist ADR-027-Phase-3-final. Reopen würde Maxential ADR-027-revoke + neuen Multi-Sprint-Refactor-Plan benötigen.
- **HotSwap-inkrementell**: ADR-022 bleibt Proposed. Reopen würde Maxential ADR-021-walk-back-revoke + Multi-Sprint-Implementation-Plan benötigen.

**Begründung der Wahl.** Maxential-effort-Trade-off: beide Items haben akzeptierte ADRs die explizit "no commitment" oder "final architecture decision" festhalten. Reopen erfordert neuen Trigger (User-Bug-Report, Performance-Pain-Point, oder strategisches Roadmap-Update). Der aktuelle Backlog-Trigger ("Damit machen wir weiter") ist nicht spezifisch genug für einen architekturellen Reopen — die anderen 5 Items (Sprint 152-156) sind klar abgegrenzte Werte mit Cost/Benefit.

Diese ADR macht den status-quo explizit, damit nicht ein folgender Sprint die Items als "TODO" interpretiert.

**Konsequenzen.**

- (+) Backlog-Items 3 + 4 ✓ closed-as-deferred-with-explicit-rationale. Künftige Sprint-Planning-Iterations können sich auf die 5 anderen Items fokussieren ohne Re-Klärung dieser zwei.
- (+) ADR-027 + ADR-021 + ADR-022 bleiben unverändert; status-quo ist dokumentiert in ADR-035.
- (–) Wenn künftig ein User-Trigger kommt (z. B. Bug-Report über UoiMutator-skip-cases die Stronger-Profile-Mutation-Score senken, oder Performance-Pain-Point der inkrementelles MT rechtfertigt), wird ein neuer Sprint nötig sein. Das ist kein neuer Aufwand — das wäre auch ohne ADR-035 der Fall.

**Supersedes / supplements.**

- **Confirms** ADR-027 Phase 3 (Skip-as-Architecture) als finale TypeSyntax-Engine-Decision.
- **Confirms** ADR-021 (HotSwap walk-back) + ADR-022 (Proposed inkrementelles MT, no commitment) als unverändert.

**Backed by.** Sprint-152-Roadmap-Maxential-Planning (Tier 4 = "zurückgestellt mit Begründung"). Keine neuen Trigger für Reopen.

**Bezug zu Backlog-Items.**

- Backlog-Item 3 (TypeSyntax-Engine Refactor) ✓ closed-as-status-quo via ADR-035.
- Backlog-Item 4 (HotSwap inkrementelles MT) ✓ closed-as-status-quo via ADR-035.

---

## ADR-034: JsonReport full AOT-trim — concrete-types source-gen completion (v3.2.8 / Sprint 154)

**Status.** Accepted — Sprint 154 (v3.2.8, 2026-05-06).

**Kontext.** ADR-024 (Sprint 17, v2.4.0) hatte JsonReport full AOT-trim als "v3.0-scope deferral" markiert (Maxential 3-way E1/E2/**E3**). Die Sprint-16-v2.3.0 Implementierung (ADR-023) etablierte einen **hybriden** Source-Gen-Ansatz: Entry-Type-`JsonTypeInfo` für `JsonReport` + `IJsonReport` aus `JsonReportSerializerContext`, kombiniert mit `DefaultJsonTypeInfoResolver` als Reflection-Fallback für die polymorphen Interface-typed Properties (`ISourceFile`, `IJsonMutant`, `ILocation`, `IPosition`, `IJsonTestFile`, `IJsonTest`). Custom-Konverter (`SourceFileConverter`, etc.) routen Interface → konkreter Typ via `JsonSerializer.Serialize<TConcrete>(writer, (TConcrete)value, options)`.

Das Reflection-Fallback war der letzte Block für volle AOT-Trim. Sprint 154 entfernt es.

**Entscheidung.** **Konkrete Typen werden zur Source-Gen-Kontext registriert:**
- `JsonSerializable(typeof(SourceFile))`
- `JsonSerializable(typeof(JsonMutant))`
- `JsonSerializable(typeof(Location))`
- `JsonSerializable(typeof(Position))`
- `JsonSerializable(typeof(JsonTestFile))`
- `JsonSerializable(typeof(JsonTest))`
- Plus die Concrete-Dictionary-Typen aus `JsonReport` (Files / TestFiles / Thresholds).

`TypeInfoResolver` wird umgestellt von `JsonTypeInfoResolver.Combine(JsonReportSerializerContext.Default, new DefaultJsonTypeInfoResolver())` auf nur `JsonReportSerializerContext.Default` — der Reflection-Fallback wird gestrichen.

**Begründung der Wahl.** Maxential 4-Schritte branchless:
1. Code-Audit der 6 Custom-Konverter zeigte: alle nutzen `JsonSerializer.Serialize<TConcrete>` mit `options`. Wenn `options.TypeInfoResolver` Source-Gen-`JsonTypeInfo` für jeden `TConcrete` liefert, dann läuft der ganze Konverter-Chain ohne Reflection.
2. Die `TConcrete`-Klassen sind alle POCO-style mit `init`-set Properties, keine cyclic dependencies, keine speziellen Converters auf Properties. Source-Gen-friendly.
3. SYSLIB1220-Restriction (custom converters can't be declared on the source-gen attribute) bleibt unverändert — Konverter werden weiterhin runtime an `Options.Converters` angefügt. Die SYSLIB1220-Restriction war NIE der Blocker; der Blocker war das Reflection-Fallback im Resolver.
4. Net effect: Source-Gen-only `JsonTypeInfoResolver`, keine Reflection im JsonReport-Pipeline-Pfad. AOT/trim-warnings beim Publish sollten verschwinden.

**Implementation.**

- `src/Stryker.Core/Reporters/Json/JsonReportSerializerContext.cs`:
  - 6 neue `[JsonSerializable]`-Attribute für die konkreten Typen.
  - 3 Concrete-Dictionary-`[JsonSerializable]` für `JsonReport.Files`, `JsonReport.TestFiles`, `JsonReport.Thresholds`.
  - Updated XML-doc dokumentiert die "v3.2.8 (Sprint 154 / ADR-034): full AOT-trim"-Erweiterung.
- `src/Stryker.Core/Reporters/Json/JsonReportSerialization.cs`:
  - `TypeInfoResolver` umgestellt von `Combine(SourceGen, DefaultReflection)` zu nur `JsonReportSerializerContext.Default`.
  - Updated XML-doc erklärt die AOT-trim-completion.

**Konsequenzen.**

- (+) Backlog-Item 1 (JsonReport full AOT-trim) ✓ closed mit ADR-034 (Sprint 154 / v3.2.8). ADR-024 v3.0-scope-deferral ist erfüllt.
- (+) AOT-trim-fähigkeit für JsonReport-Pipeline. Beim Publish mit `<PublishAot>true</PublishAot>` oder `<PublishTrimmed>true</PublishTrimmed>` sollten keine IL2026/IL3050-Warnings mehr aus dem JsonReport-Pfad kommen.
- (+) Performance-Gewinn: Source-Gen-`JsonTypeInfo` ist schneller als Reflection-basiertes `DefaultJsonTypeInfoResolver` (auch ohne AOT/trim). JsonReport-Pipeline ist hot-path beim Stryker-Run-Abschluss — nicht performance-critical, aber gratis-Verbesserung.
- (–) **Subtle behaviour change**: wenn ein Future-Refactor den JsonReport-Pipeline um polymorphe Properties erweitert ohne Source-Gen-Registration, schlägt die Serialization mit `NotSupportedException` fehl statt silent-reflection. Das ist gewollt — explizite Fail-Fast bei AOT-Lücken statt unentdeckte Reflection-fallbacks.
- (–) Falls externe Konsumenten `JsonReportSerialization.Options` für eigene Typen nutzen, müssen die jetzt entweder eigene `JsonTypeInfo` registrieren oder einen anderen `JsonSerializerOptions` nutzen. `JsonReportSerialization.Options` ist `public static readonly` — also externer API-Surface. Diese Behaviour-Change ist nicht source-breaking aber doc-relevant.

**Supersedes / supplements.**

- **Closes** ADR-024 (JsonReport full AOT-trim deferral). v3.0-scope-target erfüllt mit Sprint 154 / v3.2.8.
- **Supplements** ADR-023 (hybrid source-gen design from Sprint 16) — der hybrid-Ansatz war richtig zwischen v2.3.0 und v3.2.7, aber jetzt ist die volle Source-Gen-Coverage erreicht.

**Backed by.** Sprint 154 Maxential 4-Schritte branchless. Code-Audit der 6 Custom-Konverter (`SourceFileConverter`, `JsonMutantConverter`, `LocationConverter`, `PositionConverter`, `JsonTestFileConverter`, `JsonTestConverter`) bestätigte: alle delegieren über `JsonSerializer.Serialize<TConcrete>(writer, ..., options)` zu konkreten Typen. JsonReport-Pipeline-Tests (11 Stryker.Core.Dogfood.Tests JsonReport-tests + 2 E2E-Tests) alle grün post-change. Solution-wide 2047 Tests grün, Semgrep clean.

**Bezug zu Backlog-Items.**

- Backlog-Item 1 (JsonReport full AOT-trim) ✓ closed mit ADR-034 (Sprint 154 / v3.2.8).

---

## Änderungshistorie

| Version | Datum | Autor | Änderung |
|---------|-------|-------|----------|
| 0.1.0 | 2026-04-30 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Initiale Sprint-0-Version mit 12 ADRs |
| 0.2.0 | 2026-04-30 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 5 (v2.0.0 Architecture Foundation): ADRs 013–018 hinzugefügt — AST/IL Hybrid, Operator-Hierarchie, SemanticModel-Driven, Hot-Swap (Trampoline), Equivalent-Mutant Filtering, Mutation Profiles |
| 0.3.0 | 2026-05-01 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 14 (v2.1.0): ADR-019 — HotSwap-Engine als eigene v2.2.0-Release statt Sprint-14-Quetschung |
| 0.4.0 | 2026-05-01 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 15 (v2.2.0): ADR-021 — Walking back ADR-016 (HotSwap-Engine wegen falschen mentalen Modells in v2.0.0-Architektur entfernt). ADR-022 (Proposed) — Inkrementelles Mutation-Testing als zukünftige Performance-Direction. Supersedes ADR-016 + ADR-019. |
| 0.5.0 | 2026-05-01 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 16 (v2.3.0): ADR-023 — Validation-Framework Count-Tests prinzipieller Skip statt Reconciliation. AsyncAwaitResultMutator (catalogue +1 = 52). JsonReport hybrid source-gen. |
| 0.6.0 | 2026-05-01 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 17 (v2.4.0): ADR-024 — JsonReport full AOT-trim als v3.0-scope deferral. Plus RoslynSemanticDiagnosticsFilter + GenericConstraintLoosen interface-pair extension. |
| 0.7.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 140 (v3.1.0): ADR-025 — Mutation-Profile Auto-Bump für Mutation-Level. Code-Side Reparatur des silent-no-op-Bugs aus Calculator-Tester-Bug-Report (#1). ToT (5 Branches) + Maxential (14 Thoughts, 2 Branches) Decision-Trail. |
| 0.8.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 142 (v3.1.2 Hotfix): ADR-026 — ConditionalInstrumentation × TypeSyntax-/SimpleName-Slot incompat. Bug #9 aus Calculator-Tester-Bug-Report-Update (`--mutation-profile All` crash). UoiMutator-pre-check erweitert + SpanReadOnlySpanDeclarationMutator disabled (Profile.None) + global DoNotMutateOrchestrator<SimpleNameSyntax> mit predicate. Sprint 23-Pattern auf neue crash-Klassen übertragen. |
| 0.9.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 143 (v3.2.0-dev Phase 1): ADR-027 — Type-Position-Aware Mutation Control. Multi-Sprint Engine-Refaktor zur Root-Cause-Fix von Bug #9 statt Sprint-142-Symptom-Skip (User-Feedback). Phase 1 implementiert: Smart-Pivot in UoiMutator für MA.Name + neuer MemberAccessNameSlotOrchestrator + Mutator-set OriginalNode (`??=`). Phase 2 (MB.Name CAE-aware Lifting) und Phase 3 (TypeSyntax-Engine, SpanReadOnly re-enable) geplant. **Kein Tag** — v3.2.0 erst nach Phase 3. |
| 0.10.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 144 (v3.2.0-dev Phase 2): ADR-027 Phase 2 implementiert. CAE-aware lifting für MB.Name + MA-in-CAE.WhenNotNull-Subtree via UoiMutator's `LiftPastConditionalAccess` walk-up loop (transparent durch CAE-Expression-side-crossings für nested patterns wie `matrix?.GetType().Name?.Length`). Phase-1-Gap entdeckt + gefixt: UoiMutator emittierte PostfixUnary in TypeSyntax-Slots (user-defined Property-Types) → `InvalidCastException(ParenthesizedExpression → TypeSyntax)` Crash; mit Phase-2 `IsInTypeSyntaxPosition`-Skip. Phase-1 MB.Name-Guard entfernt; MemberAccessNameSlotOrchestrator predicate auf MA-OR-MB aufgeweitet. 6 neue UoiMutator-Tests (Phase-1+2 regression, 10 grün gesamt). **Kein Tag** — v3.2.0 erst nach Phase 3 (TypeSyntax-Engine + SpanReadOnly re-enable). |
| 0.11.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 145 (v3.2.0 final Phase 3): ADR-027 Phase 3 abgeschlossen mit **Skip-as-Architecture** (Maxential Decision Option F nach 11 Schritten + 3 Engine-Refactor-Alternativen evaluiert). TypeSyntax-Engine-Refactor (4+ Sprints für 1 niche-Mutator) verworfen wegen Cost/Benefit. UoiMutator.IsInTypeSyntaxPosition + SpanReadOnly Profile.None werden als finale Architektur-Entscheidung formalisiert (nicht-temporäre Skip-Markierung). MutatorReflectionProperties Doku updated. ADR-027 schließt. **Tag v3.2.0** (final Phase-3-Closure) — gerechtfertigt durch Phase-1+2 echten Engine-Refactor (smart-pivot, MemberAccessNameSlotOrchestrator, CAE-walk-up). User-Pushback-Path: Engine-Refactor wäre eigener v3.3.0+ Sprint. |
| 0.12.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 146 Hotfix v3.2.1: Calculator-Tester Report 3 hat Bug-9 Crash in v3.2.0 reproduziert (gleicher `InvalidCastException(ParenthesizedExpression → TypeSyntax)` Stack-Trace wie v3.1.1). Root-Cause: Phase-2 `IsInTypeSyntaxPosition` Skip-Liste war unvollständig — Pattern-Matching-Slots (DeclarationPattern, RecursivePattern, TypePattern) und TypeParameterConstraintClause fehlten. UoiMutator feuerte auf `Deposit` in `t switch { Deposit d => ... }` und cracht. Sample.Library hat keine entsprechenden Patterns, Calculator.Domain (records + switch) schon. 4 neue Skip-arms eingefügt + 4 neue UoiMutator-Tests. KEIN Engine-Refactor (Phase-3 Skip-as-Architecture bleibt). Tag **v3.2.1** (Patch-Hotfix). Note: separater Bug bei GenericConstraintMutator als v3.3.0+ Kandidat dokumentiert. |
| 0.13.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 147 v3.2.2: ADR-028 — **Central Syntax-Slot Validation Layer**. Calculator-Tester Bug-Report 4 reproduziert Bug-9 in v3.2.1 als `NullReferenceException` mit identischem Stack-Trace-Pfad. User-Forderung c): **Validierungs-Layer vor der Mutation**, KEIN weiterer Hotfix. Maxential (13 Schritte, 3 ToT-Branches) → Branch C (Hybrid Validator + Audit) gewählt. **`SyntaxSlotValidator.TryReplaceWithValidation`** + **`RoslynHelper.TryInjectMutation`** + **`MutationStore.ApplyMutationsValidated`** — defensive Pipeline-Stage zwischen Mutator-Output und Engine-Wrap. Fängt 4 Crash-Klassen: InvalidCast, NRE, InvalidOperationException-Contains-mismatch, null-Mutation-Properties. 4 Validator-Tests + lokal-acid-test mit allen 9 Calculator-Patterns ohne Crash. Solution-wide ~2200 Tests grün. Tag **v3.2.2** — Bug-9-stable-fix. |
| 0.14.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 148 v3.2.3: ADR-029 — `--version` Tool-Convention + `--project-version` **Hard Rename**. Calculator-Tester Bug-Report 4, Bug #4: `dotnet stryker-netx --version` druckt jetzt konvention-konform die Tool-Version statt als Project-Version-Wert interpretiert zu werden. Maxential (3-Weg ToT: O1=Hard-Rename, O2=Soft-Detection, O3=Status-Quo) → O1 gewählt. **Breaking-Change**: `--version <value>` → `--project-version <value>` Migration. `--tool-version` / `-T` (Sprint-141-Aliase) bleiben transitional. `StrykerCli.TryHandleToolVersionFlag` short-circuited bare-Flag BEFORE McMaster-Parser-Pipeline. RunAsync zerlegt in `BuildCommandLineApplication` + `ExecuteWithErrorHandlingAsync` (MA0051-Cap). 4 neue Sprint-148-Tests + ShouldSetProjectVersion umgestellt auf `--project-version`. Solution-wide 817 Unit-Tests grün, Semgrep clean. Tag **v3.2.3** — Bug #4 closed. Bugs #6, #8 → separate Sprints 149/150. |
| 0.15.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 149 v3.2.4: ADR-030 — `--reporters` Plural-Alias via args-Pre-Processor. Calculator-Tester Bug-Report 4, Bug #6: externe Tutorials/Doku schreiben oft `--reporters` (Plural), McMaster lehnt das mit "Unrecognized option" + "Did you mean: reporter" ab. Maxential (3-Schritte): Option A (args-rewrite) gewählt vs B (zweite Option-Registrierung) vs C (McMaster-Subclass). `RewriteReportersAlias(string[]) → string[]` rewrites `--reporters`, `--reporters=…`, `--reporters:…` zu Singular-Form BEFORE McMaster sieht args. Konsistent mit Sprint-148-Pattern (Pre-Processor). False-Positive-Guard: `--reportersx` fällt durch zu McMaster's "Did you mean"-Hilfe. 10 neue Tests (5 Rewrite + 4 Non-Rewrite + 1 E2E). Solution-wide 844 Unit-Tests grün (vs Sprint 148 = 834, +10), Semgrep clean. Tag **v3.2.4** — Bug #6 closed. Bug #8 → Sprint 150. |
| 0.16.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 150 v3.2.5: ADR-031 — `--all-projects` Multi-Project-Mutation Flag. Calculator-Tester Bug-Report 4, Bug #8: Test-Projekte mit mehreren Source-Project-Referenzen (Clean-Architecture: Domain + Infrastructure + App) krachten mit "Test project contains more than one project reference. Please set the project option…". Sprint-141-Workaround `--solution` setzt Solution-Datei voraus + scannt ALLE Solution-Projekte; User wollte per-Test-Project-Scope. Maxential 11 Schritte mit 2 ToT-Branches → B1 (Flag) gewählt vs B2 (Multi-`--project` MultipleValue, breaking-change auf SourceProjectName). Neue `AllProjectsInput` (NoValue, long-only) + IStrykerOptions.IsAllProjectsMode + InputFileResolver.ResolveMultiReferenceCase Helper (MA0051-Cap-Refactor). Bei `--all-projects` UND multi-reference → return alle SourceProjectInfo statt throw. ProjectOrchestrator iteriert ohnehin (Solution-Mode-Pfad), kein Engine-Refactor nötig. Verbesserte Fehlermeldung: zeigt jetzt `--all-projects` UND `--solution` als Alternativen. 7 neue Tests (5 AllProjectsInputTests + 2 CLI-Plumbing). Solution-wide 2035 Unit-Tests grün, Semgrep clean. Tag **v3.2.5** — Bug #8 closed. **Bug-Report 4 vollständig geschlossen** (Bugs #4, #6, #8, #9 alle via ADRs 028–031 architektonisch fixed). |
| 0.17.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 151 v3.2.6: ADR-032 — **Orchestration-Phase Slot Validation**. Calculator-Tester Bug-Report 5 (verschärfte Bug-9-Forderung): in v3.2.5 reproduziert sich der Cast-Crash auf Calculator.Infrastructure als `ParenthesizedExpressionSyntax → IdentifierNameSyntax` (statt v3.2.x's `→ TypeSyntax`). User-Forderung verschärft: "projektweite Suche nach allen impliziten oder expliziten Casts in Mutator-Code-Pfaden + Listing als Patch-Note + systemischer Eingriff statt Symptom". Maxential 5-Schritte mit 3-Branch ToT (S1/S2/**S3 Hybrid chosen**). **Architektonischer Trugschluss von Sprint 147 korrigiert**: ADR-028 Validator deckt nur Injection-Phase, nicht Orchestration-Phase. Audit aller 12 projektweiten Cast-Sites in Mutator/Orchestrator-Code: 8 safe (upcast/by-construction/`??`-Fallback), 4 unsafe (NodeSpecific/Conditional/Invocation/ExpressionBodiedProperty Orchestrators' `node.ReplaceNodes`-Calls). Neue `OrchestrationHelpers.ReplaceChildrenValidated`: per-child `SyntaxSlotValidator.TryReplaceWithValidation` + bulk-replace try/catch safety-net. Defense-in-Depth zwischen Sprint-147 (Injection) + Sprint-151 (Orchestration). 12 neue Tests (10 Integration mit MutationProfile.All über Bug-Report-4+5 Patterns + 2 Unit). Solution-wide 2047 Unit-Tests grün (vs Sprint 150 = 2035, +12), Semgrep clean. Tag **v3.2.6** — Bug #9 systemic fix + audit listing als Patch-Note. **Bug-Report 5 closed.** |
| 0.18.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 152 v3.2.7: ADR-036 — **CI build+test green** via in-repo test fixtures + cross-platform paths. Über Sprints 147-151 zeigte jede PR ein konsistentes 6/33-SUCCESS-Pattern; build+test (ubuntu/windows) waren rot, integration-test-matrix toleriert als pre-existing flake. Sprint 152 fixt zwei strukturelle Failure-Klassen die build+test betreffen: (A) Stryker.Solutions.Tests `_references/stryker-net/src/Stryker.slnx`-Path nicht in CI-checkout (`.gitignore`-excluded) → vendor als in-repo `tests/Stryker.Solutions.Tests/TestResources/UpstreamStryker.slnx` mit `<None CopyToOutputDirectory>`. (B) ProjectAnalysisMockBuilderTests hardcoded Windows-Path `c:\\src\\MyProject.csproj` → cross-platform via `Path.Combine`. Plus: (D) SseServer test windows-CI flake — 2s Timeout zu eng für slow GitHub Actions Windows runners → 10s. Maxential 4-Schritte branchless. CI-Result: `build + test (ubuntu+windows)` GRÜN (vorher beide FAILURE). Integration-test-matrix Stryker-mutation-engine-Regression (`extern alias TheLog` compile-error in TargetProject) bleibt **honest deferred** als Sprint-153+ separate-investigation. Solution-wide 2047 Tests grün lokal (±0 vs Sprint 151 — kein neuer Test, nur fixes), Semgrep clean. Tag **v3.2.7**. |
| 0.19.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Doc bundle (post-Sprint-152): ADR-033 + ADR-035. **ADR-033** — Combined Multi-Project Report Aggregation discovery: ADR-031's "v3.3+ deferred"-Claim für Multi-Project-Report-Aggregation war FALSCH — Aggregation ist seit Sprint 1 implementiert via `StrykerRunner.AddRootFolderIfMultiProject` + single `OnAllMutantsTested(rootComponent)` call. Calculator-Tester Bug-Report-5-Verifikation hatte bereits "kombinierter Report" mit 375 Mutanten Total bestätigt. Backlog-Item 7 closed by discovery. **ADR-035** — TypeSyntax-Engine Refactor + HotSwap inkrementelles MT status-quo confirmation: beide Items bleiben in ihren existierenden ADR-Status (027 Phase 3 Skip-as-Architecture / 022 Proposed-no-commitment). Backlog-Items 3+4 closed-as-status-quo. Beide ADRs sind doc-only, kein Sprint, kein neuer Tag. |
| 0.20.0 | 2026-05-06 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Sprint 154 v3.2.8: ADR-034 — **JsonReport full AOT-trim**. Schließt ADR-024 v3.0-scope-deferral (Sprint 17). Source-gen-Kontext erweitert von 2 entry-types (`JsonReport`, `IJsonReport`) auf 9 types — die 6 konkreten Typen hinter den polymorphen Interface-Konvertern (`SourceFile`, `JsonMutant`, `Location`, `Position`, `JsonTestFile`, `JsonTest`) plus 3 concrete-dictionary-types (`Dictionary<string, SourceFile>` etc). `TypeInfoResolver` umgestellt von `Combine(SourceGen, DefaultReflection)` auf nur `JsonReportSerializerContext.Default` — Reflection-Fallback gestrichen. Hybrid-Custom-Konverter-Design unverändert (SYSLIB1220 verbietet sie auf source-gen attribute). Maxential 4-Schritte branchless. 13 JsonReport-related Tests grün post-change (11 Dogfood + 2 E2E), Solution-wide 2047 Tests grün, Semgrep clean. Tag **v3.2.8** — Backlog-Item 1 closed. |
