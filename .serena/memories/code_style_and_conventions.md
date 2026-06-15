# Code Style and Conventions

## Pflicht-Patterns (CLAUDE.md)
- `sealed` für nicht-vererbbare Klassen; XML-Doc auf allen öffentlichen APIs
- `ConfigureAwait(false)` auf jedem await in Library-Code
- Boundary-Catch: `catch (Exception ex) when (ex is not OperationCanceledException)`
- Namespace = Verzeichnisstruktur; file-scoped namespaces; usings sortiert, System zuerst
- Logging via `[LoggerMessage]`-Source-Gen-Partials (Projektstandard, keine Inline-LogX-Strings)
- FluentAssertions in Tests (nie Assert.Equal/Shouldly)

## Verboten ohne dokumentierte Begründung
- `#pragma warning disable` (Begründungskommentar direkt darüber Pflicht)
- `<NoWarn>` in csproj; `[ExcludeFromArchitectureCheck]`

## Analyzer-Praxisfallen (TWAE — gelernt in Sprints 168–179)
- **S125**: code-artige Kommentare (Snippets mit Klammern/Operatoren) gelten als
  auskommentierter Code → Begründungen als PROSA formulieren
- **MA0051**: Methoden-Cap 60 Zeilen → früh extrahieren (Beispiel: `NonMutableSyntaxFences()`
  aus `BuildOrchestratorList` in Sprint 179)
- **MA0002/MA0006/MA0099**: Dictionary/HashSet IMMER mit StringComparer; `string.Equals`
  statt `==` in Lambdas; Flags-Enums ohne 0-Member brauchen pragma bei `= 0`
- **CA1873**: teure Log-Argumente hinter `IsEnabled`-Guard + pragma (Source-Gen-Blindheit)
- **S2699**: jeder [Fact] braucht eine Assertion (auch Diagnose-Tests)

## Naming
Interfaces `I*`; private Felder `_camelCase`; Konstanten/Typen PascalCase.

## EditorConfig-Tunings (ADR-004, begründet)
IL2026/IL3050/… = none (kein NativeAOT-Zwang); CA1031 = suggestion (Boundary-Catch gewollt);
CA1812 = suggestion (InjectedHelpers laufen im User-Prozess); CS1591/CA1852 = warning.

## Filesystem & Symbols (CLAUDE.md-Konvention)
- Built-In Tools (Read/Edit/Write/Glob/Grep) für Dateien; Bash-FS-Kommandos per Konvention tabu
- **Serena IMMER zuerst** für Code-Symbole: `find_symbol` / `get_symbols_overview` /
  `find_referencing_symbols`; Refactors via `rename_symbol`/`replace_symbol_body`
- Grep nur für Nicht-Symbol-Text (Kommentare, Konfig, Markdown)

## Spezial-Constraints
- `src/Stryker.Core/InjectedHelpers/*` läuft im USER-Testprozess: nur C#-2-Sprachmittel,
  voll-qualifizierte System-Typen, kein moderner Syntax-Zucker
- Test-Erwartungen der Dogfood-Suite: STRUKTURELLE Assertions (CountMutations/
  MutateSourceInClass) statt Literal-Strings mit IsActive-IDs (Sprint-119-Konvention —
  50 Mutatoren ≠ Upstream-40, IDs driften)
