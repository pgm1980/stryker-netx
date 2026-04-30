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

## Änderungshistorie

| Version | Datum | Autor | Änderung |
|---------|-------|-------|----------|
| 0.1.0 | 2026-04-30 | Claude Opus 4.7 (Co-Authored mit pgm1980) | Initiale Sprint-0-Version mit 12 ADRs |
