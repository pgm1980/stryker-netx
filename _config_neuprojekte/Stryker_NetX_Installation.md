# stryker-netx — Installation & Konfiguration

> **Ziel-Audience:** Neue Projekte auf C# 14 / .NET 10, die diese CLAUDE.md-Blueprint übernehmen.

`stryker-netx` ist der **C# 14 / .NET 10 kompatible Fork** von [Stryker.NET 4.14.1](https://github.com/stryker-mutator/stryker-net). Er wird als globales `dotnet`-Tool installiert und über `dotnet stryker-netx` aufgerufen.

---

## Warum stryker-netx (und nicht das originale Stryker.NET)?

Upstream Stryker.NET 4.14.1 (Stand: April 2026) ist mit .NET-9- / .NET-10-Projekten **nicht kompatibel**. Die Inkompatibilitäten lassen sich nicht via Konfiguration umgehen:

| Problem upstream | Auswirkung | Lösung in stryker-netx |
|------------------|------------|------------------------|
| **Buildalyzer 8.0** als transitive Dependency | kann .NET 10 MSBuild-Strukturen nicht parsen → Stryker bricht beim Project-Load ab | Buildalyzer komplett entfernt; Ersatz: `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace` |
| **`MsBuildHelper`** mit Fallback auf `vswhere` / `MsBuild.exe`-Pfade | greift auf Windows-only-Tools zu, die auf .NET-10-SDK-only-Maschinen nicht existieren | direkter MSBuild-via-SDK-Lookup |
| **`.slnx`-Format wird nicht unterstützt** | moderne Solutions können nicht geladen werden | paralleler `Microsoft.VisualStudio.SolutionPersistence`-basierter `.slnx`-Reader |
| **Buildalyzer 9.0 würde es fixen** | landete *acht Tage nach* Stryker.NET 4.14.1 — keine offizielle Stryker-Version, die das nutzt | stryker-netx ist die einzige produktiv nutzbare Variante für .NET 10 |

`stryker-netx` ist eine **unabhängige Community-Fork** und nicht offiziell mit dem Stryker-Mutator-Team affiliiert. Alle CLI-Flags und das `stryker-config.json`-Schema sind 1:1 backwards-kompatibel mit upstream Stryker.NET 4.14.1 — Migration ist eine reine Tool-Umstellung, kein Config-Edit.

Über die reine Portierung hinaus bringt stryker-netx (seit v2.0.0) eine **erweiterte Operator-Palette** mit (52 Mutatoren statt 26 in upstream, plus 5 Equivalent-Mutant-Filter) und zwei orthogonale Konfig-Achsen: `--mutation-level` (Basic/Standard/Advanced/Complete) und `--mutation-profile` (Defaults/Stronger/All).

---

## Voraussetzungen

| Voraussetzung | Version | Prüfung |
|---|---|---|
| **.NET SDK** | **10.0.107+** | `dotnet --version` |
| **Test-Project Target Framework** | net8.0, net9.0 oder **net10.0** | `<TargetFramework>` in der Test-`.csproj` |
| **Test-Runner** | VsTest oder Microsoft Testing Platform | bereits via Test-Stack vorhanden (xUnit etc.) |
| **OS** | Windows ✓, Linux ✓ (CI), macOS (best-effort) | — |

**Solution-Format:** `.sln` und `.slnx` werden beide unterstützt. In neuen Projekten ist `.slnx` Pflicht (siehe CLAUDE_CS.md).

---

## Installation

### Standard-Installation (globales Tool, empfohlen)

```bash
dotnet tool install -g dotnet-stryker-netx
```

Nach erfolgreicher Installation steht `dotnet stryker-netx` als Command zur Verfügung.

### Pin auf eine spezifische Version (empfohlen für CI)

```bash
dotnet tool install -g dotnet-stryker-netx --version 3.0.24
```

Versionspin verhindert "Mutation-Score-Drift" durch unbeabsichtigte Operator-Set-Änderungen zwischen Tool-Updates. **Pflicht in CI-Pipelines.**

### Update auf die neueste Version

```bash
dotnet tool update -g dotnet-stryker-netx
```

### Lokales Tool (Projekt-spezifisch via `dotnet-tools.json`)

Wenn das Tool in der Repo-`dotnet-tools.json` gepinnt werden soll (CI-stabilität, Onboarding-fest):

```bash
# Einmalig pro Repo
dotnet new tool-manifest

# stryker-netx als lokales Tool registrieren
dotnet tool install dotnet-stryker-netx --version 3.0.24

# Aufruf via Manifest
dotnet stryker-netx
```

`.config/dotnet-tools.json` wird ins Repo eingecheckt; Onboarding ist dann nur `dotnet tool restore`.

### Verifikation

```bash
dotnet stryker-netx --version
```

Erwartete Ausgabe: aktuelle Versionsnummer (z.B. `3.0.24`).

---

## Erstausführung

### Variante A — gegen ein Test-Projekt

Im Test-Projektverzeichnis:

```bash
cd tests/<Projektname>.Tests
dotnet stryker-netx
```

stryker-netx erkennt automatisch:
- die Test-`.csproj` (im aktuellen Verzeichnis)
- das zugehörige Source-Projekt (via `<ProjectReference>`)
- die Solution

### Variante B — gegen eine .slnx-Solution

Vom Solution-Root:

```bash
dotnet stryker-netx --solution <Projektname>.slnx
```

### Variante C — mit `stryker-config.json`

Empfohlen für nicht-triviale Konfiguration:

```bash
dotnet stryker-netx --config-file stryker-config.json
```

---

## Konfiguration (`stryker-config.json`)

Im Test-Projektverzeichnis ablegen. Vollständiges Schema 1:1 kompatibel mit [upstream Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration).

### Minimal-Konfiguration

```json
{
  "stryker-config": {
    "project": "../../src/<Projektname>/<Projektname>.csproj",
    "test-projects": [
      "../<Projektname>.Tests/<Projektname>.Tests.csproj"
    ],
    "reporters": ["html", "progress", "json"],
    "thresholds": {
      "high": 80,
      "low": 60,
      "break": 50
    }
  }
}
```

### Empfohlene Default-Konfiguration für neue Projekte

```json
{
  "stryker-config": {
    "project": "../../src/<Projektname>/<Projektname>.csproj",
    "test-projects": [
      "../<Projektname>.Tests/<Projektname>.Tests.csproj"
    ],
    "reporters": ["html", "progress", "json", "cleartext"],
    "mutation-level": "Standard",
    "mutation-profile": "Defaults",
    "coverage-analysis": "perTest",
    "thresholds": {
      "high": 80,
      "low": 60,
      "break": 50
    },
    "mutate": [
      "**/*.cs",
      "!**/Migrations/**/*.cs",
      "!**/*.Generated.cs",
      "!**/*.Designer.cs"
    ]
  }
}
```

### stryker-netx-spezifische Erweiterungen

```json
{
  "stryker-config": {
    "mutation-profile": "Stronger"
  }
}
```

`mutation-profile` ist orthogonal zu `mutation-level`. Erlaubte Werte: `Defaults` (default), `Stronger`, `All`. Siehe Sektion "Mutation Profile" unten.

---

## Mutation Level (1:1 von upstream)

`--mutation-level` steuert *welche Mutationen* ein gegebener Mutator emittiert.

| Level | Verhalten |
|---|---|
| `Basic` (0) | Nur die fundamentalsten Mutationen pro Mutator |
| `Standard` (25) | **Default.** Standard-Mutationen, gute Balance Aussagekraft/Noise |
| `Advanced` (50) | Erweiterte Mutationen inkl. typaware Operatoren (TypeDrivenReturn etc.) |
| `Complete` (100) | Alle Mutationen inkl. der aggressivsten (UoiMutator, ConstructorNull, NakedReceiver, ROR-Vollmatrix etc.) |

```bash
dotnet stryker-netx --mutation-level Advanced
```

---

## Mutation Profile (stryker-netx v2.0.0+)

`--mutation-profile` steuert *welche Mutatoren überhaupt aktiv sind*. Orthogonal zu `--mutation-level`.

| Profile | Aktive Mutatoren | Use-Case |
|---|---|---|
| `Defaults` (default) | 26 v1.x-Mutatoren (BinaryExpression, Boolean, String, Linq, Math, Regex, etc.) | Drop-in v1.x-Parität. Identisches Verhalten wie upstream Stryker.NET. |
| `Stronger` | Defaults + 18 type-aware/cargo-mutants/PIT-Operatoren = **44 Mutatoren** (Aod, RorMatrix, ConstantReplacement, MatchGuard, WithExpression, MemberVariable, AsyncAwait, DateTime, SpanMemory, GenericConstraintLoosen, ConstructorNull, SwitchArmDeletion, ConfigureAwait, TaskWhenAllToWhenAny, DateTimeAddSign, AsyncAwaitResult, TypeDrivenReturn, InlineConstants) | Mehr Bugs fangen, Noise managen. |
| `All` | Stronger + 8 aggressivste Operatoren = **52 Mutatoren** (UoiMutator, NakedReceiver, MethodBodyReplacement, ArgumentPropagation, AsSpanAsMemory, SpanReadOnlySpanDeclaration, ExceptionSwap, GenericConstraint) | Maximaler Operator-Katalog. Mutation-Volume und Runtime wachsen ~3-5×. |

```bash
# CLI
dotnet stryker-netx --mutation-profile Stronger

# Config
{ "stryker-config": { "mutation-profile": "Stronger" } }
```

**Empfehlung für Projekte:** Start mit `Defaults`. Nach Erreichen einer hohen Mutation Score (>80%) → Umstieg auf `Stronger` für weitere Test-Verschärfung. `All` nur in spezifischen Code-Kern-Modulen.

---

## Coverage-Optimization

`--coverage-analysis` reduziert die Mutation-Test-Laufzeit dramatisch durch Skip uncovered mutants und Test-Selection.

| Modus | Verhalten | Wann nutzen |
|---|---|---|
| `off` | Alle Mutanten gegen alle Tests | nur Debug |
| `all` | Mutanten skipped wenn Zeile keine Test-Coverage hat | wenn Per-Test-Coverage zu langsam |
| `perTest` | **Default.** Mutanten skipped + nur die Tests laufen, die die mutierte Zeile abdecken | empfohlen für CI |

```bash
dotnet stryker-netx --coverage-analysis perTest
```

---

## CI-Integration (GitHub Actions)

Empfohlene Workflow-Datei: `.github/workflows/mutation-testing.yml`

```yaml
name: Mutation Testing

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  mutation:
    runs-on: ubuntu-latest
    timeout-minutes: 60

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore lokale Tools (inkl. stryker-netx)
        run: dotnet tool restore

      # Alternative wenn KEIN dotnet-tools.json verwendet wird:
      # - name: Install stryker-netx (gepinnt)
      #   run: dotnet tool install -g dotnet-stryker-netx --version 3.0.24

      - name: Build
        run: dotnet build <Projektname>.slnx -c Release

      - name: Mutation Testing
        working-directory: tests/<Projektname>.Tests
        run: dotnet stryker-netx --reporter "html" --reporter "json" --reporter "cleartext" --coverage-analysis perTest

      - name: Upload Mutation Report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: stryker-report
          path: tests/<Projektname>.Tests/StrykerOutput/**/reports/
```

**Wichtig:**
- **Tool-Version pinnen** (entweder via `dotnet-tools.json` oder `--version`) — verhindert Score-Drift zwischen Tool-Updates
- **Threshold-Break setzen** (`thresholds.break: 50` in stryker-config.json) — CI failed bei Mutation Score < 50%
- **`coverage-analysis: perTest`** ist Pflicht für akzeptable CI-Laufzeit

---

## Migration von upstream Stryker.NET

Wenn das Projekt zuvor das originale `dotnet-stryker` benutzt hat:

```bash
# 1. Upstream deinstallieren
dotnet tool uninstall -g dotnet-stryker

# 2. stryker-netx installieren
dotnet tool install -g dotnet-stryker-netx --version 3.0.24

# 3. In Scripts / CI-Workflows:
#    `dotnet stryker`        →  `dotnet stryker-netx`

# 4. stryker-config.json bleibt unverändert (1:1 schema-kompatibel)
```

**Keine Änderungen an `stryker-config.json` notwendig.** Alle Reporter-Outputs (HTML, JSON, Dashboard) sind formatkompatibel.

---

## Troubleshooting

### `stryker-netx: command not found`

Globale dotnet-tools liegen in `~/.dotnet/tools` (Linux/macOS) bzw. `%USERPROFILE%\.dotnet\tools` (Windows). Sicherstellen, dass dieser Pfad in `$PATH` ist:

```bash
# Linux / macOS
export PATH="$PATH:$HOME/.dotnet/tools"

# Windows PowerShell
$env:Path += ";$env:USERPROFILE\.dotnet\tools"
```

### `error : Could not find solution file`

Vom Test-Projektverzeichnis aufrufen ODER `--solution <pfad>.slnx` explizit angeben.

### `error : MSBuildLocator could not find any installed instances of MSBuild`

.NET SDK 10.0.107+ ist Pflicht. Mit älteren SDKs (z.B. nur 9.0.x installiert) bricht stryker-netx ab.

```bash
dotnet --list-sdks
```

Falls 10.0.x fehlt: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installieren.

### `error : NuGet restore failed for net48 project`

NetFramework-Projekte mit `packages.config`-Style benötigen `nuget.exe restore` *vor* der stryker-netx-Invocation, weil `dotnet msbuild -restore` nur `<PackageReference>` handhabt.

```bash
nuget restore <Projektname>.sln
dotnet stryker-netx
```

`nuget.exe` muss auf dem PATH sein (Windows CI: `windows-latest`-Runner liefert es mit). Lokal: über Chocolatey (`choco install nuget.commandline`) oder manuell von [nuget.org](https://www.nuget.org/downloads).

### `error : The project does not target a supported framework`

stryker-netx unterstützt `net8.0`, `net9.0`, `net10.0`. Multi-targeted Projekte: stryker-netx baut gegen das *erste* `<TargetFramework>` in der Liste oder gegen `<TargetFrameworks>` mit `--target-framework`-Flag explizit gewählt.

### Mutation-Score plötzlich abgesackt nach Tool-Update

stryker-netx-Versionssprünge (z.B. v2.4 → v3.0) können neue Mutatoren einführen, die Score senken. Mitigation:

1. Tool-Version in `dotnet-tools.json` pinnen
2. `--mutation-profile Defaults` setzen, um nur die v1.x-26-Operator-Basis zu verwenden (drift-frei)

### `dotnet stryker-netx --engine ...` zeigt Deprecation-Warning

Korrekt — die `--engine`-Flag wurde in v2.2.0 walked back (siehe [ADR-021](https://github.com/pgm1980/stryker-netx/blob/main/_docs/architecture%20spec/architecture_specification.md)). Der Flag wird als No-op-Shim akzeptiert, hat aber keine Wirkung. **Aus CI-Scripts entfernen.**

---

## Workflow-Empfehlungen

### Pro Sprint (PFLICHT laut CLAUDE.md)

1. **Sprint Start:** Mutation Score Baseline notieren (in Sprint-Backlog)
2. **Während Sprint:** Bei Feature-Completion lokal `dotnet stryker-netx` auf das geänderte Test-Projekt — schnelle Feedback-Schleife
3. **Sprint Review:** Mutation Score in CI gegen Baseline vergleichen — Score-Drop ist ein Quality-Gate-Fail

### Pro Pull Request

CI läuft `dotnet stryker-netx` mit `thresholds.break: 50` (oder höher) — PR wird blockiert wenn Mutation Score zu stark abfällt.

### Bei Mutation-Score < 80%

1. HTML-Report öffnen (`StrykerOutput/<timestamp>/reports/mutation-report.html`)
2. Surviving Mutants identifizieren
3. Pro surviving Mutant entscheiden: **Test schreiben** (Default) oder **dokumentiert ignorieren** (z.B. Infrastruktur-Code)
4. Niemals einfach `mutation-profile` runterstellen, um Score zu erhöhen — das verstellt nur den Maßstab

---

## Operator-Katalog v3.0.24 (Übersicht)

| Familie | v1.x (Defaults) | Stronger-Erweiterung | All-only |
|---|---|---|---|
| Arithmetic | BinaryExpression, Math | Aod, InlineConstants, ConstantReplacement (CRCR) | — |
| Relational | BinaryExpression, RelationalPattern | RorMatrix (Vollmatrix) | — |
| Logical/Boolean | Boolean, NegateCondition, ConditionalExpression, BinaryPattern | — | — |
| Unary | PrefixUnary, PostfixUnary | — | UoiMutator |
| Strings | String, StringEmpty, StringMethod, StringMethodToConstant, InterpolatedString | — | — |
| Collections/LINQ | Linq, Collection, Initializer, ArrayCreation | TypeDrivenReturn | — |
| Object construction | ObjectCreation | ConstructorNull | — |
| Method calls | (indirect) | — | NakedReceiver, ArgumentPropagation |
| Pattern matching | IsPatternExpression, RelationalPattern, BinaryPattern | MatchGuard, SwitchArmDeletion | — |
| Records | (via ObjectCreation) | WithExpression | — |
| Member variables | (via Assignment) | MemberVariable | — |
| Method bodies | (via Block/Statement) | — | MethodBodyReplacement |
| Async/await | — | AsyncAwait, AsyncAwaitResult, ConfigureAwait, TaskWhenAllToWhenAny | — |
| DateTime | — | DateTime, DateTimeAddSign | — |
| Span/Memory | — | SpanMemory | AsSpanAsMemory, SpanReadOnlySpanDeclaration |
| Exceptions | — | — | ExceptionSwap |
| Generics | — | GenericConstraintLoosen | GenericConstraint |
| Other | Block, Statement, Assignment, Checked, Regex, NullCoalescing | — | — |

**Total:** 26 (Defaults) + 18 (Stronger) + 8 (All-only) = **52 Mutatoren**.

**Equivalent-Mutant Filter:** 5 Filter immer aktiv — `IdentityArithmetic`, `IdempotentBoolean`, `ConservativeDefaultsEquality`, `RoslynDiagnostics` (Parser-Level), `RoslynSemanticDiagnostics` (Speculative-Binding für O(1) per-Mutation Type-Check).

---

## Referenzen

| Quelle | Inhalt |
|---|---|
| [stryker-netx GitHub](https://github.com/pgm1980/stryker-netx) | Source, Issues, Releases |
| [upstream Stryker.NET docs](https://stryker-mutator.io/docs/stryker-net/) | CLI-Flags, `stryker-config.json`-Schema (1:1 kompatibel mit stryker-netx) |
| [upstream Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration) | Detaillierte Config-Schema-Doku |
| [stryker-netx README](https://github.com/pgm1980/stryker-netx/blob/main/README.md) | Aktueller Operator-Katalog, Versionshistorie, Sprint-Status |
| [stryker-netx ADRs](https://github.com/pgm1980/stryker-netx/blob/main/_docs/architecture%20spec/architecture_specification.md) | Architekturentscheidungen ADR-001 bis ADR-024 |
| [Mutation Testing Konzept](https://en.wikipedia.org/wiki/Mutation_testing) | Wikipedia-Einstieg |

---

## License & Disclaimer

`stryker-netx` ist eine **unabhängige Community-Fork** unter Apache License 2.0.
**Nicht affiliiert mit, endorsed durch oder gesponsort von** dem offiziellen Stryker.NET-Projekt, dem Stryker-Mutator-Team oder Info Support BV.
Der "Stryker"-Name wird hier nur deskriptiv verwendet (Stryker-Mutator-kompatibles Tooling), nicht als Trademark-Anspruch.
