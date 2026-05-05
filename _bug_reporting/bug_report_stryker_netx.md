# Bug-Report: `dotnet-stryker-netx` 3.0.24 — nicht produktionsreif

**An:** stryker-netx Maintainer-Team
**Von:** Calculator-Testbed-Projekt (.NET 10 / C# 14)
**Datum:** 2026-05-06
**Status:** Empfehlung — **nicht produktionsreif** in dieser Version

---

## TL;DR

Wir haben `dotnet-stryker-netx` 3.0.24 als Mutation-Testing-Tool auf einer realistischen .NET-10-Codebase (1.700 LOC src, 357 xUnit-Tests, 92,34 % Coverage, `.slnx`-Solution) eingesetzt. Das Tool **läuft grundsätzlich**, **liefert aber an mehreren Stellen falsches oder widersprüchliches Verhalten**. Der gravierendste Befund ist, dass das beworbene Feature `--mutation-profile` (Defaults / Stronger / All) auf unserer Codebase keinerlei Differenzierung zeigt. Daneben gibt es eine Reihe kleinerer, kosmetischer und UX-Schwächen, die in Summe ein unrundes Tool-Erlebnis ergeben.

**Kern-Empfehlung:** Vor Produktiv-Einsatz mindestens Bug #1 (Profile-Flag) und Bug #2 (Version-Banner) fixen; Bugs #3–#7 sind nice-to-have.

---

## 1. Umgebung

| Komponente | Version / Konfiguration |
|------------|-------------------------|
| OS | Windows 11 Pro (10.0.26200) |
| .NET SDK | 10.0.107 |
| `dotnet-stryker-netx` (NuGet) | 3.0.24 |
| `dotnet-stryker-netx` (interner Banner) | `1.0.0-preview.1` ⚠️ siehe Bug #2 |
| Test-Framework | xUnit 2.9.3 + FluentAssertions 8.8.0 + FsCheck.Xunit 3.1.0 |
| Solution-Format | `.slnx` (XML-basiert) |
| Test-Projekt | net10.0, xUnit 2.9.3, 357 Tests grün |
| Source-Projekte | `Calculator.Domain` (classlib), `Calculator.Infrastructure` (classlib), `Calculator` (Console-Exe) |
| Coverage (Coverlet) | 92,34 % Lines, 90,87 % Branches |
| Bash | MSYS Bash auf Windows |

---

## 2. Bug-Liste (sortiert nach Schweregrad)

### 🔴 Bug #1 — `--mutation-profile` zeigt keinen messbaren Effekt

**Schweregrad:** HOCH (beworbenes Feature funktioniert nicht)

**Erwartung** (laut Doku): `Defaults` aktiviert 26 Mutatoren, `Stronger` 44, `All` 52. Auf einer Codebase mit Type-Aware-Targets, Method-Bodies und Receiver-Aufrufen sollte sich daraus eine signifikant unterschiedliche Mutanten-Anzahl ergeben.

**Beobachtung:** Drei Runs auf identischer Codebase, einzige Variation `--mutation-profile`:

```bash
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Defaults  -O StrykerOutput/Inf-Defaults
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Stronger  -O StrykerOutput/Inf-Stronger
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile All       -O StrykerOutput/Inf-All
```

| Profile | Created | CompileError | Ignored | Tested | Killed | Survived | Timeout | Score |
|---------|---------|--------------|---------|--------|--------|----------|---------|-------|
| `Defaults` | **271** | 20 | 48 | 203 | 144 | 55 | 4 | 72,91 % |
| `Stronger` | **271** | 20 | 48 | 203 | 144 | 55 | 4 | 72,91 % |
| `All` | **271** | 20 | 48 | 203 | 143 | 55 | 5 | 72,91 % |

→ **Bitidentische** Mutanten-Anzahl in allen drei Spalten Created/CompileError/Ignored/Tested. Die einzige Differenz zwischen `Defaults`/`Stronger` und `All` (ein Mutant, der von Killed nach Timeout kippt) ist Rauschen aus parallelem Testlauf.

**Hinweis:** Das Tool validiert das Profile-Argument korrekt — `--mutation-profile XYZ` wird abgewiesen mit:
```
The given mutation profile (XYZ) is invalid. Valid options are: [None, Defaults, Stronger, All]
```
Das Argument wird also durchgelassen, aber an der Mutanten-Generierung augenscheinlich nicht angewandt.

**Vermutete Ursache:** Profile-Argument wird validiert und gespeichert, aber der Code-Pfad, der die Mutator-Liste anhand des Profils zusammenstellt, fällt auf einen Default-Set zurück (vermutlich `Defaults`).

**Vorschlag zur Fehlersuche:**
1. Logging der tatsächlich aktivierten Mutator-Liste pro Run hinzufügen (`-V debug`).
2. Unit-Test im stryker-netx-Repo, der nachweist, dass `MutatorLevelOptions.GetActiveMutators(profile)` für die drei Profile unterschiedliche Mengen zurückliefert.

---

### 🟡 Bug #2 — Versions-Banner inkonsistent zur Paket-Version

**Schweregrad:** MITTEL (Trust- und Diagnose-Issue)

**Erwartung:** Tool meldet seine eigene Paket-Version (3.0.24).

**Beobachtung:** Bei jedem Run, im Banner-Output:
```
Version: 1.0.0-preview.1
```

Gleichzeitig:
```bash
$ dotnet tool list -g | grep stryker-netx
dotnet-stryker-netx                    3.0.24          dotnet-stryker-netx
```

Die NuGet-Paket-Version ist 3.0.24, das Tool meldet sich aber als `1.0.0-preview.1`. Das ist nicht nur kosmetisch:

1. **Diagnose** wird schwer — wenn Bug-Reports mit unterschiedlichen Versions-Strings im Banner kommen, weiß man nicht, ob die Reporter unterschiedliche Tool-Versionen haben oder nur unterschiedlich viele Updates verpasst.
2. **Vertrauen** leidet — wenn ein Tool sich nicht über seine eigene Version im Klaren ist, ziehen Anwender berechtigterweise weitere Annahmen über Reife in Zweifel.

**Vermutete Ursache:** Hardcoded `AssemblyInfo.Version = "1.0.0-preview.1"` aus einem frühen Fork-Stand, das beim Build der NuGet-Pakete nicht mit der Paket-Version synchronisiert wird.

**Vorschlag:** `<Version>` und `<AssemblyVersion>` im `.csproj` an gemeinsame Quelle binden (z.B. `Directory.Build.props`).

---

### 🟡 Bug #3 — Update-Hinweis zeigt die bereits installierte Version als "neue verfügbare Version"

**Schweregrad:** MITTEL (UX-Verwirrung)

**Erwartung:** Wenn 3.0.24 installiert ist und 3.0.24 die neueste auf NuGet, sollte kein Update-Hinweis erscheinen.

**Beobachtung:** Bei jedem Run mit installierter 3.0.24:
```
A new version of stryker-netx (3.0.24) is available. Please consider upgrading
using `dotnet tool update -g dotnet-stryker-netx`
```

Das Tool weist sieben Mal in fünf Runs darauf hin, dass eine neue Version verfügbar sei — und zwar genau die, die schon installiert ist.

**Vermutete Ursache:** Der Update-Check vergleicht die NuGet-Latest (3.0.24) mit der internen Banner-Version (`1.0.0-preview.1` aus Bug #2). Da `3.0.24 > 1.0.0-preview.1`, gibt es immer ein "Update verfügbar".

**Vorschlag:** Behebt sich automatisch mit Bug #2 (Banner-Version-Sync), oder der Update-Check vergleicht stattdessen `Assembly.GetEntryAssembly().GetName().Version` gegen NuGet-Latest.

---

### 🟡 Bug #4 — `--version`-Flag liefert nicht die Tool-Version

**Schweregrad:** NIEDRIG (UX-Inkonsistenz)

**Erwartung:** `dotnet stryker-netx --version` zeigt die Tool-Version (Konvention bei .NET-Tools).

**Beobachtung:**
```bash
$ dotnet stryker-netx --version
Specify --help for a list of available options and commands.
Missing value for option 'version'
```

`--version` ist ein bestehender Flag, aber **erwartet ein Argument**. Aus `--help`:
```
-v|--version    Project version used in dashboard reporter
                and baseline feature. | default: ''
```

Der Flag ist also für **Project-Version** (Dashboard-Feature) reserviert, nicht für Tool-Version. Das ist eine konzeptionelle Kollision mit der gängigen .NET-Tool-Konvention.

**Test:** `--version 99.99.99` wird wortlos akzeptiert und der ganze Mutation-Run startet — also keine Validierung des übergebenen Wertes als SemVer.

**Vorschlag:**
- Den Project-Version-Flag in `--project-version` oder `--baseline-version` umbenennen.
- `--version` (oder `-V`) für Tool-Version reservieren.
- Plattform-Konvention: `dotnet stryker-netx --version` sollte einen Einzeiler wie `3.0.24` ausgeben und 0 returnen.

---

### 🟡 Bug #5 — Wiederkehrende Warning `Could not find a valid analysis for target`

**Schweregrad:** NIEDRIG (Log-Rauschen, kosmetisch)

**Beobachtung:** Bei **jedem** Run erscheint im Info-Log:
```
[INF] Could not find a valid analysis for target  for project
'C:\claude_code\stryker-test\tests\Calculator.Tests\Calculator.Tests.csproj'.
Selected version is net10.0.
```

Die zwei Leerzeichen vor "for project" deuten auf ein leeres Format-String-Argument hin (`$"... for target {emptyString} for project ..."`). Tritt auf, obwohl das Test-Projekt klar als `net10.0` aufgelöst wird und Stryker direkt danach verkündet `Found project ... to mutate.` und auch korrekt 357 Tests entdeckt.

**Vermutete Ursache:** MSBuild-Workspace-Loader liefert ein leeres `TargetFramework`-Feld in einer Übergangsphase, und der Code loggt das als Warning, obwohl er anschließend einen Default einsetzt.

**Vorschlag:** Entweder Warning entfernen (wenn der Default-Pfad zuverlässig funktioniert), oder den fehlenden Wert in der Meldung explizit nennen (`for target '' (empty)`).

---

### 🟡 Bug #6 — Inkonsistenz `--reporter` (singular) im Help vs. CLAUDE-Beispiele mit `--reporters` (plural)

**Schweregrad:** NIEDRIG (UX, ggf. Dokumentationsbug)

**Beobachtung:** Mehrere externe Anleitungen (auch unsere [_config/Stryker_NetX_Installation.md](../../_config/Stryker_NetX_Installation.md)) zeigen `--reporters "html"`. Das Tool kennt diesen Flag nicht:

```bash
$ dotnet stryker-netx --reporters html
Specify --help for a list of available options and commands.
Unrecognized option '--reporters'

Did you mean this?
    reporter
    open-report
```

Im `--help` ist nur `-r|--reporter` (singular) dokumentiert, mit `default: ['Progress', 'Html']`. Mehrfach-Werte mit `-r html -r json`.

**Vorschlag:** Entweder
- `--reporters` (plural) als Alias akzeptieren — nahe an Tippfehler-Toleranz, was die "Did you mean"-Hilfe ohnehin schon andeutet.
- Oder die Doku überall an `--reporter` (singular) anpassen.

(Dies ist möglicherweise eher ein Doku-Bug der weiterverbreiteten Tutorials als ein Tool-Bug.)

---

### 🟢 Hinweis #7 — NuGet-Publishing-Verzögerung

**Schweregrad:** INFORMATIONELL

**Beobachtung:** Am 2026-05-05 war das Paket `dotnet-stryker-netx 3.0.24` auf nuget.org **nicht** auffindbar:

```bash
$ dotnet tool install -g dotnet-stryker-netx --version 3.0.24
"Version "3.0.24" des Pakets "dotnet-stryker-netx"" wurde in NuGet-Feeds
"https://api.nuget.org/v3/index.json" nicht gefunden.

$ dotnet tool search stryker
dotnet-stryker          4.14.1   ...
dotnet-stryker-unofficial   3.7.2  ...
# (kein dotnet-stryker-netx)
```

Am 2026-05-06 (~24 h später) war dieselbe Version (3.0.24) ohne weitere Änderungen plötzlich verfügbar und installierbar. Möglicherweise NuGet-Indexing-Latency oder verzögerte Veröffentlichung.

**Wirkung:** Anwender, die der Doku folgen, erhalten zunächst die Fehlermeldung "Paket nicht gefunden" und vermuten, dass das Tool nicht (mehr) existiert. CI-Pipelines fallen scheinbar unmotiviert um.

**Vorschlag:**
- Release-Prozess: nach `dotnet nuget push` mindestens auf Index-Sichtbarkeit warten (`dotnet package search` polling) bevor die Doku/Release-Notes verteilt werden.
- Doku: Hinweis auf typische NuGet-Indexing-Latenz (15–60 min) hinzufügen.

---

### 🟢 Hinweis #8 — Multi-Source-Project-Test-Setup verlangt manuelles `--project`

**Schweregrad:** NIEDRIG (UX)

**Beobachtung:** Wenn das Test-Projekt mehrere `<ProjectReference>` zu Source-Projekten hat (in unserem Fall drei: Domain, Infrastructure, App), bricht Stryker mit:

```
Test project contains more than one project reference. Please set the project
option to specify which project to mutate.
Choose one of the following references:
  C:/.../src/Calculator.Domain/Calculator.Domain.csproj
  C:/.../src/Calculator.Infrastructure/Calculator.Infrastructure.csproj
  C:/.../src/Calculator/Calculator.csproj
```

Der Anwender muss pro Layer einen separaten Run starten — das ist umständlich, wenn ein Projekt mehrere Layer hat (Clean-Architecture-Setups sind so üblich).

**Vorschlag:**
- Zusätzliche Option `--all-projects` o.ä., die alle gefundenen References sequenziell mutiert und einen kombinierten Report generiert.
- Oder eine Mehrfach-`--project`-Angabe (`--project A.csproj --project B.csproj`).

---

## 3. Beobachtungen, die KEIN Bug sind

Zur Fairness:

- **Tool-Crash**: Keiner. Alle Runs liefen sauber durch.
- **HTML-Report**: Self-contained, gut lesbar, klickbare Source-Annotation pro Mutant.
- **JSON-Report**: Validates JSON, vollständige Mutant-Liste, gut maschinell parsebar.
- **Coverage-basierter Test-Lauf**: `--coverage-analysis perTest` ist Default, funktioniert, ~10× schneller als ohne.
- **`.slnx`-Support**: Fehlerlos. Erkennt Test-Projekt, baut es, mutiert die referenzierten Source-Projekte.
- **Survivor-Detection**: 55 Survivors auf Infrastructure-Layer sind realistische Test-Gaps (siehe `comparison.md` Sektion 4 für Klassifikation).
- **Profile-Validierung**: Werte `[None, Defaults, Stronger, All]` werden geprüft, ungültige Werte wie `XYZ` sauber abgelehnt mit Liste der validen Optionen.

---

## 4. Reproduzier-Setup

**Codebase:** Calculator-Demo in C# 14 / .NET 10. Falls gewünscht: Branch `feature/2-mutation-testing` enthält den Stand bei Bug-Discovery; Mutation-Reports unter `tests/Calculator.Tests/StrykerOutput/`.

**Minimaler Repro für Bug #1 (Profile-Flag):**

```bash
# 1. Tool installieren
dotnet tool install -g dotnet-stryker-netx --version 3.0.24

# 2. In Test-Project-Verzeichnis wechseln
cd tests/Calculator.Tests

# 3. Drei Runs mit identischer Source-Project-Auswahl, einzige Variation: Profile
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Defaults -O StrykerOutput/A
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Stronger -O StrykerOutput/B
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile All       -O StrykerOutput/C

# 4. Vergleichen — alle drei Runs zeigen "271 mutants created" und Score 72,91 %
diff <(jq '.files[] | .mutants | length' StrykerOutput/A/reports/mutation-report.json) \
     <(jq '.files[] | .mutants | length' StrykerOutput/B/reports/mutation-report.json)
# (Kein Output → identisch.)
```

**Erwartet:** Unterschiedliche Mutanten-Zahlen pro Profile (analog zur Mutator-Tabelle in der Tool-Doku, 26 / 44 / 52 Operatoren).

**Tatsächlich:** Identische Zahlen, identische Survivor-Listen.

---

## 5. Empfehlung

| Entscheidung | Begründung |
|--------------|------------|
| **Tool nicht im Produktiv-CI einsetzen, bis Bug #1 gefixt ist.** | Wer `--mutation-profile Stronger` setzt, erwartet stärkere Mutanten und höhere Aussagekraft des Mutation-Scores. Die Identität der drei Profile-Outputs macht jede solche Begründung haltlos. |
| **Vor Bug-Fix: nur `Defaults` (= effektives Verhalten) dokumentieren.** | Wer trotzdem das Tool nutzt, sollte wissen, dass `Stronger` und `All` aktuell keine zusätzliche Wirkung entfalten — sonst werden falsche Schlüsse aus Score-Vergleichen gezogen. |
| **Bug #2 (Versions-Banner) ist Trust-relevant.** | Ein Tool, das seine eigene Version nicht kennt, lässt Anwender an der Code-Disziplin zweifeln. Auch wenn rein kosmetisch — der Eindruck zählt. |
| **Bug #3 (Update-Hinweis) selbsterklärend zu fixen, sobald Bug #2 behoben ist.** | Beide haben dieselbe Wurzel. |
| **Bugs #4–#8 nice-to-have.** | Niedrige Priorität, eher Polish und Doku-Sync. |

**Gesamtbewertung:** **Tool ist alpha-/beta-tauglich, aber nicht produktionsreif.** Es liefert wertvolle Survivor-Detektion und integriert sich technisch gut in die .NET-10-Toolchain — was ein echter Fortschritt gegenüber dem inkompatiblen upstream Stryker.NET 4.14.x auf .NET 10 ist. Die Anomalien (insbesondere Bug #1) verhindern aber den Status "verlässliches CI-Tool".

---

## 6. Anhang — vollständige Log-Ausgaben

### Banner & Versionscheck (jeder Run)
```
   _____ _              _               _   _ ______ _______
  / ____| |            | |             | \ | |  ____|__   __|
 | (___ | |_ _ __ _   _| | _____ _ __  |  \| | |__     | |
  \___ \| __| '__| | | | |/ / _ \ '__| | . ` |  __|    | |
  ____) | |_| |  | |_| |   <  __/ |    | |\  | |____   | |
 |_____/ \__|_|   \__, |_|\_\___|_| (_)|_| \_|______|  |_|
                   __/ |
                  |___/

Version: 1.0.0-preview.1                          ← Bug #2
A new version of stryker-netx (3.0.24) is available.  ← Bug #3
Please consider upgrading using `dotnet tool update -g dotnet-stryker-netx`
```

### Standard-Run-Log (gekürzt)
```
[INF] Analysis starting.
[INF] Analyzing 1 test project(s).
[INF] Could not find a valid analysis for target  for project          ← Bug #5
      'C:\...\tests\Calculator.Tests\Calculator.Tests.csproj'.
      Selected version is net10.0.
[INF] Found project C:\...\Calculator.Infrastructure.csproj to mutate.
[INF] Analysis complete.
[INF] Number of tests found: 357 ... Initial test run started.
[INF] 271 mutants created
[INF] Capture mutant coverage using 'CoverageBasedTest' mode.
[INF] 20    mutants got status CompileError.
[INF] 48    mutants got status Ignored.        Reason: Removed by block already covered filter
[INF] 203   total mutants will be tested

Killed:   144
Survived: 55
Timeout:  4

Your html report has been generated at: ...\reports\mutation-report.html
[INF] Time Elapsed 00:01:04
[INF] The final mutation score is 72.91 %
```

### `--version`-Versuch (Bug #4)
```bash
$ dotnet stryker-netx --version
Specify --help for a list of available options and commands.
Missing value for option 'version'

$ dotnet stryker-netx --version 99.99.99
[Banner ...] [Banner says it's running, accepts 99.99.99 silently]
```

### `--reporters`-Versuch (Bug #6)
```bash
$ dotnet stryker-netx --reporters html
Specify --help for a list of available options and commands.
Unrecognized option '--reporters'

Did you mean this?
    reporter
    open-report
```

### `--mutation-profile XYZ` — Validierung funktioniert
```bash
$ dotnet stryker-netx --mutation-profile XYZ --project Calculator.Domain.csproj
The given mutation profile (XYZ) is invalid.
Valid options are: [None, Defaults, Stronger, All]
```

---

## 7. Kontakt / Repo-Verweis

Falls Reproduktion an Originalcode gewünscht: dieses Repository (`stryker-test`) auf `feature/2-mutation-testing`-Branch enthält die exakte Codebase + die fünf Stryker-Output-Verzeichnisse mit HTML- und JSON-Reports (`tests/Calculator.Tests/StrykerOutput/`).

Bei Rückfragen: gerne per Issue im stryker-netx-Repo oder direkt per E-Mail an das Calculator-Testbed-Projekt.
