# stryker-netx

## Projekt

- **Stack**: C# 14 / .NET 10
- **Repository**: `https://github.com/pgm1980/stryker-netx.git`
- **Ziel**: `Ziel dieses Projketes ist die Portierung von Stryker.NET auf C#14 und .NET 10 für .NET 10 Projekte, da Stryker.NET inkompatibel mit .NET 10 Projekten ist und nur mit Projekten auf Basis von .NET Framework 4.8 sowie .NET Core 3.1 funktioniert. `

---

## Verbindliche Tool-Nutzung (OBERSTE DIREKTIVEN — NICHT VERHANDELBAR)

Die folgenden Tools MÜSSEN während der gesamten Entwicklung aktiv eingesetzt werden — sowohl in der Hauptsession als auch in Subagenten. Kein Fallback auf generische Alternativen ohne dokumentierte Begründung.

**Konfigurative Durchsetzung:** Zusätzlich zu diesen Direktiven sind Filesystem-Bash-Befehle und `pwsh` **hart gesperrt** via `.claude/settings.json`. Selbst bei Nichtbeachtung dieser Regeln werden `cat`, `ls`, `grep`, `find`, `cp`, `mv`, `rm`, `pwsh` etc. vom Harness mit `Permission denied` blockiert. Siehe Sektion [Konfigurative Durchsetzung via settings.json](#konfigurative-durchsetzung-via-settingsjson) für die vollständige Liste.

### Subagenten-Policy

Subagenten haben seit Claude Code v2.1.x vollen Zugriff auf alle **MCP-Server**, **Plugins** und **Skills** der Hauptsession. 
Die frühere Einschränkung (kein MCP-, Plugin und Skill-Zugriff für Subagenten) wurde durch Anthropic behoben.

#### Einsatz von Subagenten

Subagenten MÜSSEN für parallelisierbare Aufgaben eingesetzt werden. 
Sie erben automatisch alle MCP-Server, Plugins sowie Skills der Hauptsession und MÜSSEN die gleichen Quality-Standards einhalten wie die Hauptsession.

**ERLAUBT:**
- `subagent-driven-development` Skill für Task-basierte Implementierung mit Review-Zyklen
- `dispatching-parallel-agents` Skill für unabhängige, parallele Aufgaben
- `executing-plans` Skill für Plan-Ausführung in separater Session
- Code Reviews via Subagent (mit Serena + Semgrep Zugriff)

**PFLICHT für jeden Subagent-Prompt:**
Jeder Subagent-Prompt MUSS folgende Regeln enthalten, damit der Subagent die Projekt-Standards kennt:

```
PROJEKT-STANDARDS (NICHT VERHANDELBAR):
- Built-In Tools (Read/Edit/Write/Glob/Grep) für Filesystem-Operationen — Bash für cat/cp/mv/rm/find/grep ist via .claude/settings.json hart gesperrt
- GitHub CLI (`gh`) für mehrstufige Git-Workflows (Branch + Push + PR, Tag + Release)
- Serena für Code-Navigation (KEIN Grep für Klassen/Methoden/Properties)
- Context7 VOR Nutzung neuer APIs konsultieren
- Semgrep-Scan auf JEDE geänderte Datei
- FluentAssertions statt xUnit Assert — PFLICHT
- TreatWarningsAsErrors aktiv — 0 Warnings, 0 Errors
- Kein #pragma warning disable ohne Kommentar-Begründung direkt darüber
- Sealed classes für nicht-vererbbare Typen
- XML-Dokumentationskommentare für alle öffentlichen APIs
- ConfigureAwait(false) auf allen async Calls
- catch (Exception ex) when (ex is not OperationCanceledException) Pattern
- Alle neuen Dateien: Namespace muss der Verzeichnisstruktur entsprechen
```

#### Verifikation nach Subagent-Rückkehr

Auch wenn Subagenten MCP-Zugriff haben, MUSS die Hauptsession nach jeder Subagent-Rückkehr stichprobenartig verifizieren:

- [ ] Build: 0 Warnings, 0 Errors? (`dotnet build` selbst ausführen)
- [ ] Alle Tests grün? (`dotnet test` selbst ausführen)
- [ ] FluentAssertions verwendet (nicht `Assert.Equal` etc.)?
- [ ] Neue Namespaces/Schichten → ArchUnitNET-Test nötig?
- [ ] Roundtrip/Invariante testbar → FsCheck Property-Test nötig?
- [ ] Security-relevanter Code → Semgrep sofort ausführen?
- [ ] Neue API genutzt → Context7 vorher konsultiert?
- [ ] Serena `get_symbols_overview` auf neue Dateien — Strukturcheck
- [ ] Bei Security-relevantem Code: Semgrep-Scan selbst bestätigen

**Vertrauen, aber verifizieren.** Subagent-Aussagen "Build sauber, Tests grün" sind Hinweise, keine Beweise.


#### Subagent-Prompt-Standard

Jeder Subagent-Prompt MUSS die folgenden 5 Sektionen enthalten. Unvollständige Prompts führen zu schlechter Agent-Qualität.

```
## KONTEXT
[Wo stehen wir im Sprint? Was wurde bisher gemacht? Welche Dateien/Module sind betroffen?]

## ZIEL
[Exakt was der Agent tun soll — ein klar abgegrenztes Ergebnis, nicht vage]

## CONSTRAINTS
[Was der Agent NICHT tun darf — z.B. keine anderen Module ändern, keine Breaking Changes]

## MCP-ANWEISUNGEN
[Welche MCP-Server für diese Aufgabe relevant sind und wie sie eingesetzt werden sollen]
Beispiel:
- Serena: `find_symbol` vor jeder Code-Änderung, `get_symbols_overview` auf neue Dateien
- Semgrep: Scan auf alle geänderten Dateien vor Abschluss
- Context7: Bei Nutzung neuer APIs konsultieren

## OUTPUT
[Was der Agent zurückmelden soll — geänderte Dateien, Zusammenfassung, Build/Test-Status, offene Probleme]
```

**Beispiel eines vollständigen Subagent-Prompts:**
```
## KONTEXT
Sprint 3, Task 2: Wir implementieren den CacheService. Task 1 (Models) ist abgeschlossen.
Betroffene Dateien: src/<Projekt>.Infrastructure/Services/CacheService.cs (neu),
src/<Projekt>.Domain/Interfaces/ICacheService.cs (neu)

## ZIEL
Implementiere ICacheService und CacheService mit folgenden Methoden:
- GetAsync(string key) → CacheEntry?
- SetAsync(string key, object value, TimeSpan ttl) → Task
- InvalidateAsync(string key) → Task<bool>
Inklusive Unit Tests mit FluentAssertions + FsCheck für Roundtrip-Properties.

## CONSTRAINTS
- Keine Änderungen an bestehenden Service-Klassen
- Keine neuen NuGet-Pakete ohne Context7-Prüfung
- async/await-basiert, ConfigureAwait(false) auf allen Calls

## MCP-ANWEISUNGEN
- Serena: get_symbols_overview auf IBaseService.cs um bestehende Patterns zu verstehen
- Context7: IMemoryCache API prüfen (Expiration, Eviction)
- Semgrep: Scan auf neue Dateien nach Implementierung

## OUTPUT
- Liste geänderter/neuer Dateien
- Build-Status (0 Warnings, 0 Errors)
- Test-Status (alle grün)
- Offene Fragen oder Probleme
```

#### Worktree-Isolation (PFLICHT bei parallelen Edit-Agents)

Wenn mehrere Subagenten **parallel Code editieren**, MÜSSEN sie mit `isolation: "worktree"` gestartet werden. 
Ohne Worktree-Isolation überschreiben sich parallele Agents gegenseitig.

| Agent-Typ                                                | `isolation: "worktree"` | Begründung             |
|----------------------------------------------------------|-------------------------|------------------------|
| Parallele Implementierung (2+ Agents editieren Code)     | **PFLICHT**             | Verhindert Konflikte   |
| Sequentielle Implementierung (1 Agent nach dem anderen)  | Nicht nötig             | Kein Konfliktrisiko    |
| Code Review (read-only)                                  | Nicht nötig             | Keine Änderungen       |
| Exploration/Recherche (read-only)                        | Nicht nötig             | Keine Änderungen       |

**Nach Worktree-Agent-Rückkehr:**
1. Änderungen aus dem Worktree in den Hauptbranch mergen
2. Bei Konflikten: Hauptsession löst Konflikte manuell
3. Build + Test nach dem Merge ausführen

#### MaxTurns-Empfehlungen (PFLICHT)

Jeder Subagent-Aufruf MUSS einen `max_turns`-Parameter enthalten, um Endlosschleifen bei autonomem Betrieb zu verhindern.

| Agent-Aufgabe                         | `max_turns` | Begründung                                |
|---------------------------------------|-------------|-------------------------------------------|
| Feature-Implementierung (komplex)     | 40–50       | Braucht Platz für TDD-Zyklen, Refactoring |
| Feature-Implementierung (einfach)     | 20–30       | Weniger Zyklen nötig                      |
| Code Review                           | 15–20       | Lesen + Analysieren + Report              |
| Exploration/Recherche                 | 10–15       | Gezielte Suche, nicht open-ended          |
| Quick Fix / Bug Fix                   | 10–15       | Fokussierte Änderung                      |
| Security Audit (Semgrep)              | 10–15       | Scan + Analyse + Report                   |

**Bei Überschreitung:** Wenn ein Agent sein `max_turns`-Limit erreicht, MUSS die Hauptsession bewerten:
- War die Aufgabe zu groß? → In kleinere Tasks aufteilen
- Steckt der Agent in einer Schleife? → Anderen Ansatz wählen
- Braucht er mehr Kontext? → Neuen Agent mit besserem Prompt dispatchen

#### Error Recovery Pattern (PFLICHT)

Wenn ein Subagent fehlschlägt oder ein unvollständiges Ergebnis liefert:

```
     ┌─────────────────────┐
     │ Agent meldet Failure│
     └──────────┬──────────┘
                │
                ▼
┌─────────────────────────────────┐
│ Hauptsession analysiert Fehler  │
│ (Transcript lesen, Build prüfen)│
└───────────────┬─────────────────┘
                │
          ┌─────┴─────┐
          │           │
          ▼           ▼
┌──────────────┐  ┌──────────────┐
│ Trivial      │  │ Komplex/     │
│ (Typo,Import)│  │ Architektur  │
│              │  │              │
└──────┬───────┘  └──────┬───────┘
       │                 │
       ▼                 ▼
 ┌──────────┐  ┌────────────────────┐
 │ Fix-Agent│  │ Hauptsession löst  │
 │ mit Error│  │ selbst (kein Agent)│
 │ + Context│  │                    │
 └────┬─────┘  └────────────────────┘
      │
      ▼
┌──────────────────────┐
│ Max 2 Retry-Zyklen   │
│ Dann → Hauptsession  │
└──────────────────────┘
```

**Regeln:**
1. **Nie manuell fixen nach Agent-Failure** ohne den Fehler zu verstehen — Kontext-Pollution vermeiden
2. **Fix-Agent** bekommt: Original-Prompt + Fehlermeldung + relevante Teile des Agent-Transcripts
3. **Max 2 Retries** — nach 2 gescheiterten Fix-Agents eskaliert die Hauptsession und löst selbst
4. **Bei Build-Fehlern**: Erst `dotnet build` Output analysieren, dann gezielten Fix-Agent mit exakter Fehlermeldung dispatchen
5. **Bei Test-Fehlern**: Erst `dotnet test` Output analysieren, dann Fix-Agent mit Failed-Test-Namen + Stack Trace dispatchen


### Serena — Symbolbasierte Code-Analyse

Serena ist als MCP-Server verfügbar und bietet präzise, symbolbasierte Code-Navigation via Roslyn (OmniSharp/C# Language Server).
**Serena MUSS bevorzugt vor Grep/Glob/Read verwendet werden**, wenn es um Code-Analyse geht — während der gesamten Implementierung und bei Code-Navigation.

**Wann Serena verwenden (IMMER zuerst):**
- **Build-Fehler**: `find_symbol` um das fehlende/fehlerhafte Symbol zu lokalisieren, `find_referencing_symbols` um alle Aufrufer zu finden
- **Test-Failures**: `find_symbol` für die fehlschlagende Methode, `get_symbols_overview` für die Testklasse, `find_referencing_symbols` um die Aufrufkette zu verstehen
- **Refactoring**: `rename_symbol` statt manuelles Suchen/Ersetzen, `find_referencing_symbols` um Impact zu prüfen
- **Code verstehen**: `get_symbols_overview` für Datei-Überblick, `find_symbol` mit `include_body=true` für Implementierungsdetails
- **Neue Dateien erkunden**: `get_symbols_overview` IMMER zuerst, bevor eine Datei gelesen wird

**Serena-Tools in Reihenfolge der Präferenz:**
1. `get_symbols_overview` — Erster Überblick über eine Datei (Klassen, Methoden, Properties)
2. `find_symbol` — Symbol nach Name finden (mit `include_body=true` für Quelltext)
3. `find_referencing_symbols` — Wer ruft dieses Symbol auf? Wo wird es verwendet?
4. `rename_symbol` — Sicheres Umbenennen über die gesamte Codebase
5. `replace_symbol_body` — Gezielter Ersatz einer Methode/Klasse
6. `insert_after_symbol` / `insert_before_symbol` — Code an symbolischer Position einfügen
7. `search_for_pattern` — Regex-Suche (nur wenn symbolische Suche nicht passt)

**Wann Grep/Glob als Fallback erlaubt:**
- Suche in Nicht-Code-Dateien (XML, JSON, YAML, Markdown, .csproj)
- Suche nach Textmustern die keine Code-Symbole sind (z.B. Fehlermeldungen, Konfigurationswerte)
- Suche nach Dateinamen (`Glob`)

**VERBOTEN bei Code-Analyse:**
- **NICHT** `Grep` verwenden um Klassen, Methoden oder Properties zu finden — Serena nutzen
- **NICHT** `Read` auf eine ganze Datei anwenden um ein Symbol zu finden — `find_symbol` nutzen
- **NICHT** manuell Suchen/Ersetzen für Umbenennungen — `rename_symbol` nutzen

### Semgrep — Security-Scanning

Semgrep MUSS als Security-Scanner eingesetzt werden — in der Hauptsession UND in Subagenten.

**Wann Semgrep verwenden (PFLICHT):**
- **Vor jedem Sprint-Abschluss**: Vollständiger Scan der Codebase
- **Bei Code Reviews**: Scan der geänderten Dateien
- **Nach sicherheitsrelevantem Code**: Sofortiger Scan (Auth, Crypto, Input-Validierung, Deserialisierung)
- **Supply-Chain-Analyse**: Bei neuen NuGet-Paket-Abhängigkeiten

**VERBOTEN:**
- **NICHT** einen Sprint abschließen ohne bestandenen Semgrep-Scan
- **NICHT** Security-Findings ignorieren oder als False Positive markieren ohne dokumentierte Begründung

### Context7 — Aktuelle Dokumentation

Context7 MUSS vor der Nutzung von APIs und Libraries konsultiert werden — in der Hauptsession UND in Subagenten.

**Wann Context7 verwenden (PFLICHT):**
- **Vor Nutzung neuer APIs**: .NET APIs, NuGet-Pakete
- **Bei Unsicherheit über API-Verhalten**: Parameter, Rückgabewerte, Exceptions
- **Bei Versionswechseln**: Breaking Changes prüfen
- **Best Practices verifizieren**: Aktuelle Empfehlungen für Patterns und Anti-Patterns

**VERBOTEN:**
- **NICHT** APIs aus dem Gedächtnis verwenden ohne aktuelle Dokumentation zu prüfen
- **NICHT** veraltete Patterns anwenden wenn Context7 aktuellere Empfehlungen liefert

### Sequential Thinking (Maxential) — Komplexe Entscheidungen

Sequential Thinking Maxential ist ein **MCP Server** (`mcp__sequential-thinking-maxential__*`).
Er bietet Branching, Revisionen, Tags, Session-Persistenz und Visualisierung — nutze die volle Tiefe.

**Wann Maxential verwenden (PFLICHT):**
- Bei Architekturentscheidungen mit mehreren validen Alternativen
- Bei mehrstufigen Problemen die schrittweise Analyse erfordern
- Bei Abwägungen zwischen Performance, Wartbarkeit und Komplexität
- Bei Debugging-Szenarien mit mehreren möglichen Ursachen
- Bei Design-Reviews vor Implementierungsbeginn

**Workflow (PFLICHT — in dieser Reihenfolge):**
1. **`think`** — Gedanken nacheinander eintragen (Nummer wird auto-inkrementiert)
2. **`branch`** — Bei gleichwertigen Alternativen einen Branch erstellen (`branch_id` + `reason`)
3. **`think`** innerhalb des Branches — Alternative zu Ende denken
4. **`close_branch`** — Branch mit `conclusion` abschließen
5. **`switch_branch`** — Zurück zu `main` oder in einen anderen Branch wechseln
6. **`merge_branch`** — Abgeschlossenen Branch zurück in main mergen (`strategy: "full_integration"`)
7. **`revise`** — Wenn ein früherer Denkschritt sich als falsch herausstellt (`revises_thought: N`)
8. **`tag`** — Wichtige Gedanken taggen (z.B. `["decision"]`, `["risk"]`, `["tradeoff"]`)
9. **`complete`** — Denkprozess mit finaler `conclusion` abschließen

**Nutzungsregeln (PFLICHT):**
- **Mindestens 10 Denkschritte** bei Architektur- und Softwaredesign-Entscheidungen
- **Mindestens 8 Denkschritte** beim Entwurf von komplexeren Algorithmen
- **Mindestens 3 Denkschritte** bei einfacheren Abwägungen
- **Branches PFLICHT** wenn es zwei oder mehr gleichwertige Lösungsansätze gibt — jeden Branch zu Ende denken, dann vergleichen und mit `strategy: "full_integration"` mergen
- **`revise`** verwenden wenn ein früherer Denkschritt sich als falsch herausstellt — nicht einfach linear weitermachen
- **`tag`** verwenden um Schlüsselentscheidungen, Risiken und Trade-offs zu markieren
- **`merge_branch` mit `strategy: "full_integration"`** — immer die vollständige Integration wählen, nicht nur `conclusion_only`
- Ergebnis mit `complete` abschließen und im Chat zusammenfassen und dem User zur Entscheidung vorlegen
- **Sessions speichern** (`session_save`) bei umfangreichen Analysen, damit sie später wiederverwendet werden können

**Maximale Denktiefe — Konfigurationsreferenz:**
```
think          → Gedanken eintragen (unbegrenzt viele)
branch         → branch_id: string, reason: string
close_branch   → branch_id: string, conclusion: string
switch_branch  → branch_id: string | null (null = main)
merge_branch   → branch_id: string, strategy: "full_integration" | "summary" | "conclusion_only"
revise         → thought: string, revises_thought: number
tag            → thought_number: number, add: ["tag1", "tag2"]
search         → query: string, tags: ["tag"], branch_id: string
complete       → conclusion: string
session_save   → name: string, description: string
visualize      → format: "mermaid" | "ascii", show_content: true
```

**VERBOTEN:**
- **NICHT** Maxential mit nur 1-2 Schritten abkürzen
- **NICHT** `complete` aufrufen bevor eine fundierte Schlussfolgerung erreicht ist
- **NICHT** `merge_branch` mit `strategy: "conclusion_only"` verwenden wenn die Analyse komplex ist — immer `"full_integration"` bevorzugen
- **NICHT** Branches offen lassen — immer mit `close_branch` + `conclusion` abschließen vor dem Merge

### Code Analyzers — Directory.Build.props

Die folgenden Analyzers MÜSSEN in einer `Directory.Build.props` im Verzeichnis der `.slnx`-Datei konfiguriert sein. Code der Analyzer-Warnungen erzeugt, kompiliert nicht.

```xml
<Project>
  <!-- Analyzers -->
  <ItemGroup>
    <!-- Roslynator -->
    <PackageReference Include="Roslynator.Analyzers" Version="4.15.0" PrivateAssets="all" />
    <!-- Sonar -->
    <PackageReference Include="SonarAnalyzer.CSharp" Version="10.20.0.135146" PrivateAssets="all" />
    <!-- Meziantou -->
    <PackageReference Include="Meziantou.Analyzer" Version="3.0.22" PrivateAssets="all" />
  </ItemGroup>
  <!-- Alle Analyzer-Warnungen als Fehler behandeln (strenger Modus) -->
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**Analyzer-Verantwortung:**
- **Roslynator**: Code-Qualität, Vereinfachungen, Best Practices
- **SonarAnalyzer.CSharp**: Security, Reliability, Maintainability
- **Meziantou.Analyzer**: .NET Best Practices, Performance, Security

**VERBOTEN:**
- **NICHT** `#pragma warning disable` ohne dokumentierte Begründung im Code-Kommentar
- **NICHT** `TreatWarningsAsErrors` deaktivieren

### Test-Stack — Test-Projekte

Die folgenden Pakete MÜSSEN in jeder Test-`.csproj` referenziert werden:

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="8.0.0">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="FluentAssertions" Version="8.8.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  <PackageReference Include="TngTech.ArchUnitNET.xUnit" Version="0.11.0" />
  <PackageReference Include="FsCheck.Xunit" Version="3.1.0" />
</ItemGroup>
```

**VERBOTEN:**
- **NICHT** Tests ohne Coverage-Messung (Coverlet) ausführen
- **NICHT** Assert-Statements mit xUnit-Assert statt FluentAssertions schreiben

### ArchUnitNET — Architecture Testing

ArchUnitNET MUSS verwendet werden, um Architekturregeln als ausführbare Tests zu definieren und durchzusetzen.

**Paket** (in Test-`.csproj`):
```xml
<PackageReference Include="TngTech.ArchUnitNET.xUnit" Version="0.11.0" />
```

**Wann ArchUnitNET verwenden (PFLICHT):**
- **Bei Projektanlage**: Grundlegende Schichtenregeln als Tests definieren (z.B. Domain darf nicht auf Infrastructure zugreifen)
- **Bei neuen Namespaces/Schichten**: Sofort Architektur-Tests ergänzen
- **Bei Refactoring**: Architektur-Tests als Sicherheitsnetz gegen Schichtverletzungen

**Beispiel-Regeln:**
```csharp
[Fact]
public void Domain_Should_Not_Depend_On_Infrastructure()
{
    ArchRuleDefinition
        .Types().That().ResideInNamespace("Domain")
        .Should().NotDependOnAny("Infrastructure")
        .Check(Architecture);
}
```

**VERBOTEN:**
- **NICHT** Architekturregeln nur dokumentieren — sie MÜSSEN als ausführbare Tests existieren
- **NICHT** Schichtverletzungen durch `[ExcludeFromArchitectureCheck]` umgehen ohne Begründung

### FsCheck — Property-Based Testing

FsCheck MUSS ergänzend zu klassischen Unit Tests eingesetzt werden, um Edge Cases durch randomisierte Eingaben zu finden.

**Paket** (in Test-`.csproj`):
```xml
<PackageReference Include="FsCheck.Xunit" Version="3.1.0" />
```

**Wann FsCheck verwenden (PFLICHT):**
- **Serialisierung/Deserialisierung**: Roundtrip-Properties (Serialize→Deserialize = Original)
- **Parsing/Validation**: Für JEDEN gültigen Input muss die Invariante gelten
- **Mathematische/logische Operationen**: Kommutativität, Assoziativität, Idempotenz
- **Mappings/Konvertierungen**: Bijektivität prüfen

**Beispiel:**
```csharp
using FsCheck;
using FsCheck.Xunit;

public class SerializationProperties
{
    [Property]
    public Property Roundtrip_Serialization()
    {
        return Prop.ForAll<NonEmptyString>(input =>
        {
            var encoded = Encode(input.Get);
            var decoded = Decode(encoded);
            return decoded == input.Get;
        });
    }
}
```

**VERBOTEN:**
- **NICHT** nur Happy-Path-Tests schreiben wenn Property-Based Testing Edge Cases aufdecken kann
- **NICHT** FsCheck-Failures ignorieren — sie zeigen echte Grenzfälle auf

### BenchmarkDotNet — Performance Benchmarks

BenchmarkDotNet MUSS für Performance-kritische Komponenten eingesetzt werden.

**Paket** (in separatem Benchmark-Projekt):
```xml
<PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
```

**Wann BenchmarkDotNet verwenden (PFLICHT):**
- **Hot Paths**: Request-Handling, Serialisierung, häufig aufgerufene Methoden
- **Vor/Nach Optimierungen**: Messbare Vergleiche statt Bauchgefühl
- **Bei Architekturentscheidungen**: Performance-Vergleich zwischen Alternativen

**Beispiel:**
```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private readonly MyModel _model = CreateTestModel();

    [Benchmark(Baseline = true)]
    public string Serialize_SystemTextJson()
        => System.Text.Json.JsonSerializer.Serialize(_model);

    [Benchmark]
    public byte[] Serialize_Utf8()
        => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_model);
}
```

**Ausführung:**
```bash
dotnet run --project benchmarks/<Projektname>.Benchmarks -c Release
```

**VERBOTEN:**
- **NICHT** Performance-Behauptungen ohne Benchmark-Daten aufstellen
- **NICHT** Benchmarks im Debug-Modus ausführen (BenchmarkDotNet warnt selbst, aber trotzdem beachten)

### Filesystem-Operationen — Built-In Tools

Die Claude Code Built-In Tools (`Read`, `Edit`, `Write`, `Glob`, `Grep`) sind die primären Werkzeuge für Filesystem-Operationen in diesem Projekt. Bash-Filesystem-Befehle sind via `.claude/settings.json` hart gesperrt — siehe Sektion [Konfigurative Durchsetzung via settings.json](#konfigurative-durchsetzung-via-settingsjson).

**Tool-Verwendung:**

| Tool      | Anwendung                                                                                       |
|-----------|-------------------------------------------------------------------------------------------------|
| **Read**  | Datei-Inhalt lesen (mit Zeilennummern). Für Code-Symbole → Serena `find_symbol` bevorzugen      |
| **Edit**  | Gezielte Ersetzung in einer Datei (visueller Diff-Output, von Claude Code UI angezeigt)         |
| **Write** | Neue Datei erstellen oder vollständige Überschreibung (vorher `Read` auf existierende Datei)    |
| **Glob**  | Datei-Suche nach Namen-/Pfad-Pattern (`**/*.cs`, `src/**/*.csproj`)                             |
| **Grep**  | Inhalts-Suche in Dateien (regex-basiert, ripgrep). Für Code-Symbole → Serena bevorzugen         |

**Bash bleibt ERLAUBT für:** `dotnet build`, `dotnet test`, `dotnet run`, `dotnet publish`, `semgrep`, `gh`, atomare Git-Befehle (`git status`, `git log`, `git diff`, `git branch`, `git remote`, `git add`, `git commit`, `git push`, `git checkout`, `git merge`, `git rebase`, `git tag`).

**VERBOTEN UND HART GESPERRT (settings.json `deny`):**
- `cat`, `head`, `tail`, `cp`, `mv`, `rm`, `find`, `grep`, `rg`, `diff`, `tar`, `du`, `stat`, `ls`, `tree`, `sort`, `uniq`, `sed`, `awk`, `wc`, `base64`, `sha256sum`, `mkdir`, `touch` — **werden vom Harness blockiert**
- `pwsh` — **wird vom Harness blockiert**

**VERBOTEN (CLAUDE.md-Direktive):**
- **NICHT** `Grep` für Klassen/Methoden/Properties — Serena `find_symbol` nutzen
- **NICHT** `Read` auf eine ganze Datei anwenden um ein Symbol zu finden — Serena `find_symbol` nutzen
- **NICHT** manuell Suchen/Ersetzen für Symbol-Umbenennungen — Serena `rename_symbol` nutzen
- **NICHT** Bash-Filesystem-Befehle nutzen — sind hart gesperrt

### GitHub CLI (`gh`) — Mehrstufige Git-Workflows

Mehrstufige Git-Workflows (Branch + Push + PR, Tag + Release, Issue-Erstellung) MÜSSEN über die **GitHub CLI** (`gh`) abgewickelt werden — atomare Git-Befehle bleiben via Bash erlaubt.

**Installation:**
```bash
gh --version
```

**Authentifizierung:** Siehe [_misc/git-setup-for-claude-code.md](_misc/git-setup-for-claude-code.md).

**Git-Operationen — Entscheidungsmatrix:**

| Git-Operation | Tool | Begründung |
|---------------|------|------------|
| Mehrstufige Workflows (Branch + Push + PR, Tag + Release, Issue-Erstellung) | **`gh`** (PFLICHT) | strukturierte Kapselung der GitHub-API |
| Atomare Einzelbefehle (`git status`, `git log`, `git diff`, `git branch`, `git remote`) | **Bash** (allow via settings.json) | reine Informationsabfrage |
| `git add`, `git commit`, `git push`, `git checkout`, `git merge`, `git rebase`, `git tag` | **Bash** (allow nach Konfiguration in settings.json) | atomare Schreib-Operationen |

**VERBOTEN:**
- **NICHT** mehrstufige GitHub-Workflows als Bash-Sequenzen manuell zusammenbauen — `gh pr create`, `gh issue create`, `gh release create` nutzen
- **NICHT** GitHub-API-Calls via `curl` ausführen — `gh api` nutzen

---

## Commands

| Command                                                             | Beschreibung                                    |
|---------------------------------------------------------------------|-------------------------------------------------|
| `dotnet build`                                                      | Solution bauen (alle Projekte)                  |
| `dotnet test --collect:"XPlat Code Coverage"`                       | Tests mit Coverage ausführen                    |
| `dotnet test --filter "FullyQualifiedName~UnitTests"`               | Nur Unit Tests                                  |
| `dotnet test --filter "FullyQualifiedName~IntegrationTests"`        | Nur Integration Tests                           |
| `dotnet run --project src/<Projekt>`                                | Projekt starten                                 |
| `dotnet publish -c Release`                                         | Release-Build erstellen                         |
| `semgrep scan --config auto .`                                      | Security-Scan (vollständig)                     |
| `semgrep scan --config auto --changed-files`                        | Security-Scan (nur geänderte Dateien)           |
| `dotnet run --project benchmarks/<Projekt>.Benchmarks -c Release`   | Performance Benchmarks ausführen                |

---

## Architecture

> Wird bei Projektanlage befüllt. Erwartete Struktur:

```
<Projektname>/
  src/
    <Projekt>/                         # Application Layer (Entry Point)
      Program.cs                       # Entry Point
      <Projekt>.csproj
    <Projekt>.Domain/                  # Domain Layer (Interfaces + Models)
      Interfaces/
      Models/
      Exceptions/
      <Projekt>.Domain.csproj
    <Projekt>.Infrastructure/          # Infrastructure Layer (Implementierungen)
      Services/
      Configuration/
      <Projekt>.Infrastructure.csproj
  tests/
    <Projekt>.Tests/                   # xUnit + FluentAssertions + FsCheck + ArchUnitNET
      <Projekt>.Tests.csproj
  benchmarks/
    <Projekt>.Benchmarks/              # BenchmarkDotNet
      <Projekt>.Benchmarks.csproj
  Directory.Build.props                # Analyzers (Roslynator, SonarAnalyzer, Meziantou)
  global.json                          # SDK 10.0
  .editorconfig                        # Code-Style-Enforcement
  <Projektname>.slnx                   # Solution-Datei (XML-Format)
```

---

## Key Files

> Wird bei Projektanlage befüllt. Erwartete Einträge:

- `<Projektname>.slnx` — Solution-Datei (XML-Format)
- `Directory.Build.props` — Analyzer-Konfiguration (Roslynator, SonarAnalyzer, Meziantou)
- `global.json` — SDK-Version pinnen (`{ "sdk": { "version": "10.0.100" } }`)
- `.editorconfig` — Code-Style-Regeln (Indent, Naming Conventions, Severity)
- `src/<Projekt>/Program.cs` — Entry Point
- `_docs/architecture spec/architecture_specification.md` — Architektur-Spezifikation mit ADRs
- `_docs/design spec/software_design_specification.md` — FRs + NFRs
- `_config/development_process.md` — Vollständiger Scrum-basierter Entwicklungsprozess

---

## Environment / Prerequisites

| Voraussetzung          | Version | Zweck                                                       |
|------------------------|---------|-------------------------------------------------------------|
| .NET SDK               | 10.0    | Build, Test, Run                                            |
| Semgrep CLI            | aktuell | Security-Scanning                                           |
| Serena MCP-Server      | aktuell | Symbolbasierte Code-Analyse (via Roslyn)                    |
| Context7 MCP-Server    | aktuell | Aktuelle API-Dokumentation                                  |
| Git                    | aktuell | Versionskontrolle                                           |
| GitHub CLI (`gh`)      | aktuell | Mehrstufige Git-Workflows, GitHub-API-Zugriff               |

---

## Git-Konventionen

Beim Repository-Setup den `git-workflow-guide` Skill verwenden.

- **Branching**: GitHub Flow
- **Commits**: Conventional Commits (`type(scope): description`)
- **Tags**: SemVer (`vMAJOR.MINOR.PATCH`), annotated Tags nach Erreichen eines Milestones
- **Branch-Naming**: `feature/[ISSUE-NR]-kurzbeschreibung`, `fix/[ISSUE-NR]-kurzbeschreibung`

---

## Sprint State Management (`.sprint/state.md`)

Die Datei `.sprint/state.md` ist das zentrale Steuerungsdokument für alle Hooks in `.claude/hooks/`. Sie MUSS ein **YAML-Frontmatter** mit exakt den unten definierten Feldern enthalten. Fehlt die Datei oder das Frontmatter, feuern die Hooks ohne Wirkung.

### Schema (PFLICHT — alle Felder erforderlich)

```yaml
---
current_sprint: "1"                    # Sprint-Nummer (String)
sprint_goal: "Kurzbeschreibung"        # 1-Satz Sprint-Ziel
branch: "feature/1-kurzbeschreibung"   # Erwarteter Git-Branch für diesen Sprint
started_at: "2026-03-30"               # ISO-Datum des Sprint-Starts
housekeeping_done: false               # true = alle HK-Items erledigt, false = Sprint-Gate aktiv
memory_updated: false                  # true = MEMORY.md in diesem Sprint aktualisiert
github_issues_closed: false            # true = alle Sprint-Issues geschlossen
sprint_backlog_written: false          # true = Sprint-Backlog-Dokument existiert
semgrep_passed: false                  # true = Semgrep-Scan ohne Findings bestanden
tests_passed: false                    # true = alle Tests grün (pytest + mypy + ruff)
documentation_updated: false           # true = Docs/Docstrings aktualisiert
---
```

### Feld-Referenz

| Feld | Typ | Default | Gelesen von Hook(s) |
|------|-----|---------|---------------------|
| `current_sprint` | String | — | sprint-health, sprint-gate, statusline, post-compact-reminder, sprint-housekeeping-reminder |
| `sprint_goal` | String | — | sprint-health, post-compact-reminder |
| `branch` | String | — | sprint-health, post-compact-reminder |
| `started_at` | ISO-Datum | — | sprint-health, sprint-gate |
| `housekeeping_done` | Boolean | `false` | sprint-health, sprint-gate, statusline, post-compact-reminder, sprint-housekeeping-reminder |
| `memory_updated` | Boolean | `false` | sprint-health, sprint-housekeeping-reminder |
| `github_issues_closed` | Boolean | `false` | sprint-health, sprint-housekeeping-reminder |
| `sprint_backlog_written` | Boolean | `false` | sprint-health, sprint-housekeeping-reminder |
| `semgrep_passed` | Boolean | `false` | sprint-health |
| `tests_passed` | Boolean | `false` | sprint-health |
| `documentation_updated` | Boolean | `false` | sprint-health |

### Lifecycle

1. **Sprint-Start**: Claude erstellt/aktualisiert `.sprint/state.md` mit neuem Sprint, `housekeeping_done: false` und allen Items auf `false`
2. **Während Sprint**: Items werden auf `true` gesetzt sobald sie erledigt sind
3. **Sprint-Ende**: Alle Items `true`, dann `housekeeping_done: true` setzen → Sprint-Gate deaktiviert
4. **Nächster Sprint**: Frontmatter mit neuer Sprint-Nummer überschreiben, alle Items zurück auf `false`

### Hook-Zuordnung

| Hook | Trigger | Liest | Wirkung |
|------|---------|-------|---------|
| `sprint-health.sh` | SessionStart | Alle Felder | Zeigt Sprint-Status + offene HK-Items + Warnungen |
| `sprint-gate.sh` | PostToolUse (git commit) | `housekeeping_done`, `current_sprint`, `started_at` | Warnt wenn HK nicht erledigt |
| `statusline.sh` | Permanent | `current_sprint`, `housekeeping_done` | `S1 [HK!]` oder `S1` |
| `post-compact-reminder.sh` | PostCompact | `current_sprint`, `sprint_goal`, `branch`, `housekeeping_done` | CLAUDE.md-Reminders + Sprint-State |
| `sprint-housekeeping-reminder.sh` | Stop | `current_sprint`, `housekeeping_done`, `memory_updated`, `github_issues_closed`, `sprint_backlog_written` | Session-End-Warnung |
| `sprint-state-save.sh` | PreCompact | Gesamte Datei | Hängt Git-Context an state.md an |
| `verify-after-agent.sh` | SubagentStop | — (prüft Code direkt) | Ruff + mypy + pytest + Semgrep |

### Validierung

Der `sprint-health.sh` Hook validiert beim SessionStart, ob alle Pflichtfelder vorhanden sind. Fehlende Felder werden als Warnung ausgegeben.

**VERBOTEN:**
- **NICHT** `.sprint/state.md` ohne YAML-Frontmatter schreiben — die Hooks ignorieren die Datei dann komplett
- **NICHT** Felder weglassen — fehlende Felder führen zu stillen Hook-Fehlfunktionen
- **NICHT** `housekeeping_done: true` setzen bevor alle Items tatsächlich erledigt sind

---

## Entwicklungsprozess — Scrum-basiert

Vollständiger Prozess: Siehe [_config/development_process.md](_config/development_process.md)

**Kurzübersicht:**

| Phase           | Inhalt                                                                               | Ergebnis                                                            |
|-----------------|--------------------------------------------------------------------------------------|---------------------------------------------------------------------|
| Sprint 0        | Brainstorming → Architektur (ADRs) → Softwaredesign (FRs, NFRs)                      | `architecture_specification.md`, `software_design_specification.md` |
| Product Backlog | DoD, Epics, Features, User Stories, Acceptance Criteria → GitHub Issues + Milestones | `product_backlog.md`, Vollständiges Backlog in GitHub               |
| Sprint 1–N      | Sprint Planning → Implementation (TDD) → Tests → Increment                           | `sprint_backlog.md`, Lauffähiges und getestetes Feature             |
| Review          | Code Review → Feedback → Branch Integration                                          |  Merged Feature, GitHub Issues schließen, ggf. GitHub Tag           |

---

## Overlap-Resolution

Wenn zwei Skills in Frage kommen, gilt diese Entscheidungstabelle:

| Situation                                    | Verwende                                                         | Nicht                        |
|----------------------------------------------|------------------------------------------------------------------|------------------------------|
| Neues Konzept, kein Code vorhanden           | `brainstorming`                                                  | `feature-development`        |
| Feature in bestehender Codebase              | `feature-development`                                            | `brainstorming`              |
| Vage Idee → strukturiertes Spec-Dokument     | `write-spec`                                                     | `brainstorming`              |
| Einzelne Tech-Entscheidung (ADR)             | `architecture`                                                   | `architecture-designer`      |
| Vollständiges System-Design (neues Projekt)  | `architecture-designer`                                          | `architecture`               |
| Architektur steht, nur Task-Breakdown        | `writing-plans`                                                  | `feature-development`        |
| Architektur offen, Exploration nötig         | `feature-development`                                            | `writing-plans`              |
| Tasks sind sequentiell/abhängig              | `executing-plans`                                                | `subagent-driven-development`|
| Tasks sind unabhängig/parallelisierbar       | `subagent-driven-development` oder `dispatching-parallel-agents` | Sequentielle Einzelarbeit    |
| Schneller Check nach einem Task              | `requesting-code-review`                                         | `pr-review`                  |
| Umfassendes Review vor Merge/PR              | `pr-review`                                                      | `requesting-code-review`     |
| Quality Review innerhalb feature-development | `feature-development` (Phase 6)                                  | `pr-review`                  |
| Standalone Review außerhalb Feature-Workflow | `pr-review`                                                      | `feature-development`        |
| Bug mit klarem Stack Trace / Error           | `debug`                                                          | `systematic-debugging`       |
| Bug unklar, mehrere mögliche Ursachen        | `systematic-debugging`                                           | `debug`                      |
| Code-Qualität bewerten, Refactoring-Backlog  | `tech-debt`                                                      | `requesting-code-review`     |
| Iterative Prozessverbesserung (PDCA)         | `plan-do-check-act`                                              | `tech-debt`                  |
| MCP Server bauen/erweitern                   | `mcp-builder`                                                    | `feature-development`        |
| Feature-Branch braucht Isolation             | `using-git-worktrees`                                            | Manuelles `git worktree`     |
| Einfaches Reasoning (step-by-step)           | `thought-based-reasoning`                                        | `tree-of-thoughts`           |
| Komplexes Reasoning (Exploration + Pruning)  | `tree-of-thoughts`                                               | `thought-based-reasoning`    |
| Multi-Agent-Architektur entwerfen            | `multi-agent-patterns`                                           | `dispatching-parallel-agents`|
| Tiefgehende Multi-Perspektiven-Analyse       | `critique`                                                       | `pr-review`                  |
| Schnelles Code-Review vor Merge              | `pr-review`                                                      | `critique`                   |

---

## Cross-Cutting Skills

Diese Skills sind an keine Phase gebunden — sie werden **situativ** aktiviert:

| Skill                            | Trigger                                                                                |
|----------------------------------|----------------------------------------------------------------------------------------|
| `systematic-debugging`           | Bug, Testfehler, unerwartetes Verhalten — Ursache unklar, mehrere Hypothesen           |
| `debug`                          | Bug mit klarem Stack Trace oder Error Message — schnelle, fokussierte Session          |
| `verification-before-completion` | Vor jeder Behauptung "fertig", "funktioniert", "Tests grün"                            |
| `finishing-a-development-branch` | Wenn alle Tests grün und Sprint abgeschlossen                                          |
| `dispatching-parallel-agents`    | Wenn 2+ unabhängige Aufgaben gleichzeitig bearbeitet werden können                     |
| `receiving-code-review`          | Wenn Review-Feedback vorliegt, vor Umsetzung der Vorschläge                            |
| `subagent-driven-development`    | Wenn Plan mit unabhängigen Tasks in der aktuellen Session ausgeführt wird              |
| `write-spec`                     | Vage Feature-Idee → strukturiertes Spec/PRD mit Goals, Non-Goals, Akzeptanzkriterien   |
| `architecture`                   | Einzelne Architekturentscheidung (ADR) treffen, Technologie-Wahl bewerten              |
| `architecture-designer`          | Vollständiges System-Design: Requirements, Patterns, Diagramme, NFRs, DB-Auswahl       |
| `tech-debt`                      | Nach Release: Code-Qualität bewerten, Refactoring priorisieren, Wartungsbacklog        |
| `plan-do-check-act`              | Iterative Verbesserung: Hypothese → Experiment → Messung → Standardisierung            |
| `mcp-builder`                    | MCP Server bauen oder erweitern (Python FastMCP oder TypeScript SDK)                   |
| `using-git-worktrees`            | Feature-Branch-Isolation vor Implementierung oder paralleler Arbeit                    |
| `thought-based-reasoning`        | Komplexes Reasoning: CoT, Self-Consistency, Least-to-Most, ReAct, PAL — Technik-Auswahl|
| `tree-of-thoughts`               | Hardest Problems: Systematische Exploration mit Pruning, Multi-Agent-Judges, Synthesis |
| `multi-agent-patterns`           | Multi-Agent-Architektur entwerfen: Supervisor, Peer-to-Peer, Hierarchisch              |
| `critique`                       | Tiefgehende Multi-Perspektiven-Analyse: 3 Judges + Debate + Consensus (report-only)    |
| `skill-creator`                  | Nur beim Erstellen, Bearbeiten oder Testen von Skills selbst                           |

---
## Konfigurative Durchsetzung via settings.json

Zusätzlich zu den CLAUDE.md-Direktiven erzwingt `.claude/settings.json` harte Sperren auf Harness-Ebene. Diese Sperren können **nicht** umgangen werden — das Harness blockiert den Aufruf mit `Permission denied` bevor er ausgeführt wird.

**Hart gesperrt (`deny`):**

| Kategorie                      | Gesperrte Befehle                  | Stattdessen verwenden                                                                   |
|--------------------------------|------------------------------------|-----------------------------------------------------------------------------------------|
| **Datei lesen**                | `cat`, `head`, `tail`              | Built-In `Read`                                                                         |
| **Datei kopieren/verschieben** | `cp`, `mv`                         | Built-In `Read` + `Write` (lesen + neu schreiben)                                       |
| **Datei löschen**              | `rm`                               | Manuell durch User oder explizite Confirmation einholen                                 |
| **Verzeichnis**                | `ls`, `tree`, `mkdir`, `touch`     | Built-In `Glob`; Verzeichnisse werden implizit durch `Write` erstellt                   |
| **Suche**                      | `find`, `grep`, `rg`               | Serena `find_symbol` (Code-Symbole), Built-In `Grep`/`Glob` (Text/Dateinamen)           |
| **Text-Verarbeitung**          | `sort`, `uniq`, `sed`, `awk`, `wc` | Built-In `Edit` für gezielte Änderungen, `Read`+`Write` für komplexere Transformationen |
| **Archiv/Hash**                | `tar`, `base64`, `sha256sum`       | Out-of-Scope — falls nötig: User-Anfrage stellen                                        |
| **Datei-Info**                 | `diff`, `du`, `stat`               | Built-In `Read` zum Vergleichen; Git für Diffs (`git diff`)                             |
| **PowerShell**                 | `pwsh`                             | — (PowerShell wird in diesem Projekt nicht verwendet)                                   |

**Erlaubt (`allow`):**

| Befehl                                 | Begründung                          |
|----------------------------------------|-------------------------------------|
| `dotnet *`                             | Build, Test, Run, Publish           |
| `git status*`, `git log*`, `git diff*` | Atomare Git-Informationsabfragen    |
| `git branch*`, `git remote*`           | Branch/Remote-Informationsabfragen  |
| `git add*`, `git commit*`, `git push*`, `git checkout*`, `git merge*`, `git rebase*`, `git tag*` | Atomare Git-Schreib-Operationen |
| `gh *`                                 | GitHub CLI für mehrstufige Workflows |
| `semgrep *`                            | Security-Scanning                   |

**Nicht konfigurierbar (Built-In Tools):** `Read`, `Write`, `Edit`, `Glob`, `Grep` sind Kern-Tools des Claude Code Harness und können NICHT via settings.json gesperrt werden. Ihre Nutzung wird ausschließlich durch die CLAUDE.md-Direktiven geregelt — siehe Sektion [Filesystem-Operationen — Built-In Tools](#filesystem-operationen--built-in-tools) für die exakten Bedingungen.

**Hinweis zu Subagenten:** Subagenten erben die `deny`-Regeln der settings.json. Ein Subagent kann also ebenfalls kein `cat`, `cp`, `pwsh` etc. ausführen — die gleichen Sperren gelten.

---

## Gotchas

- **TreatWarningsAsErrors ist aktiv**: Jede Analyzer-Warnung ist ein Build-Fehler. Nicht umgehen — beheben.
- **Serena-Onboarding nicht vergessen**: Nach jedem Projektstart `get_symbols_overview` auf die Hauptdateien ausführen, damit Serena den Projekt-Index aufbaut.
- **Coverlet braucht `--collect`-Flag**: `dotnet test` allein erzeugt keinen Coverage-Report. Immer `--collect:"XPlat Code Coverage"` verwenden.
- **`.slnx` statt `.sln`**: Dieses Projekt verwendet das neue XML-basierte Solution-Format. Ältere `dotnet`-Tooling-Versionen unterstützen `.slnx` möglicherweise nicht.
- **`global.json` nicht vergessen**: Ohne `global.json` nutzt `dotnet` die neueste installierte SDK-Version — das kann auf anderen Maschinen zu Build-Fehlern führen.
- **`.editorconfig` wird von Roslyn-Analyzern ausgewertet**: Naming-Conventions und Severity-Overrides wirken direkt auf die Build-Analyse. Nicht nur IDE-Kosmetik.
- **Semgrep bei C#**: Nicht alle Regeln greifen bei C#. `--config auto` ist der beste Startpunkt; projektspezifische Rules bei Bedarf ergänzen.
- **ArchUnitNET braucht statische Architecture-Instanz**: `Architecture` einmal pro Testklasse laden (teuer), nicht pro Test. Shared Fixture nutzen.
- **FsCheck-Seed dokumentieren**: Bei fehlschlagenden Property-Tests den Seed aus dem Output notieren — damit lässt sich der Fehler reproduzieren.
- **BenchmarkDotNet NUR im Release-Modus**: Debug-Benchmarks sind wertlos. `dotnet run -c Release` ist Pflicht. BenchmarkDotNet bricht bei Debug ab.
- **`#pragma warning disable` ist verboten** ohne dokumentierte Begründung im Code-Kommentar direkt darüber.
- **Subagenten erben MCP-Zugriff**: Seit Claude Code v2.1.x haben Subagenten vollen MCP-Zugriff. Die CLAUDE.md-Regeln gelten für sie genauso. Trotzdem nach Rückkehr stichprobenartig verifizieren.

---

## Projektspezifische Regeln

- **Sprache**: C# 14 mit .NET 10
- **Solution-Format**: `.slnx` (neues XML-Format)
- **Build**: Self-contained, Single-File, Trimmed/AOT wo möglich
- **Plattformen**: Windows
- **Testframework**: xUnit + FluentAssertions + Coverlet + ArchUnitNET (Architecture) + FsCheck (Property-Based) + BenchmarkDotNet (Performance)
- **Logging**: Strukturiertes Logging (Serilog)
- **Konfiguration**: YAML/JSON basiert
- **Coding-Standards**: Durchgesetzt via Roslynator + SonarAnalyzer + Meziantou
- **Code-Dokumentation**: XML-Dokumentationskommentare für öffentliche APIs

---

## Referenzen

| Pfad                                 | Inhalt                                                           |
|--------------------------------------|------------------------------------------------------------------|
| `_config/development_process.md`     | Vollständiger Scrum-basierter Entwicklungsprozess                |
| `_misc/git-setup-for-claude-code.md` | GitHub CLI / Git Konfiguration für Claude Code (Windows)         |
| `MEMORY.md`                          | Projektgedächtnis mit aktuellem Stand und offenen Entscheidungen |
