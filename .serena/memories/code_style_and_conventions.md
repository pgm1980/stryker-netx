# Code Style and Conventions

## Mandatory C# Patterns (CLAUDE.md PFLICHT)
- `sealed` default for non-inheritable classes
- XML-doc comments on ALL public APIs (CS1591 enforced as warning)
- `ConfigureAwait(false)` on EVERY `await` in library code (Roslynator-enforced)
- Exception pattern at system boundaries:
  ```csharp
  catch (Exception ex) when (ex is not OperationCanceledException)
  {
      _logger.LogError(ex, "...");
      // graceful handling
  }
  ```
- Namespace MUST match directory structure
- File-scoped namespaces (`namespace Foo;` not `namespace Foo { ... }`)
- `using` directives outside namespace, sorted, system first

## Forbidden Without Documented Justification
- `#pragma warning disable` (must have inline comment with reason directly above)
- `<NoWarn>` in csproj (use `.editorconfig` severity instead — also requires comment)
- `[ExcludeFromArchitectureCheck]` attribute

## Naming Conventions
- Interfaces: `I` prefix (`IMutator`, `IReporter`)
- Private fields: `_camelCase` underscore prefix
- Constants: `PascalCase`
- Types (classes, structs, enums): `PascalCase`

## EditorConfig Severity Tunings (with justification, ADR-004)
The following severities are intentionally lowered/disabled in `.editorconfig` for Stryker-specific patterns:
- `IL2026, IL3050, IL2070, IL2090, IL3053` = `none` — NativeAOT not enforced (ADR-006); reflection-heavy paths in Buildalyzer/Roslyn intentional
- `CA1031` = `suggestion` — defensive catch at system boundaries is intentional (matches CLAUDE.md catch-pattern)
- `CA1812` = `suggestion` — embedded resources (MutantControl.cs, MutantContext.cs) loaded at runtime in user-test-projects, not in stryker-netx itself
- `CS1591` = `warning` — enforce XML doc on public APIs
- `CA1852` = `warning` — sealed-by-default policy

## Filesystem Operations Convention (CLAUDE.md)
**Built-In Tools first** (`Read`, `Edit`, `Write`, `Glob`, `Grep`).
Bash filesystem commands (`cat`, `cp`, `mv`, `rm`, `find`, `grep`, `ls`, etc.) are technically available (settings.json `bypassPermissions`) but **per-convention forbidden**.

**Phase 1 documented exception**: Bulk-copy from read-only `_reference/` to `src/` was done with `cp -r` (one-shot, 56 files; alternative would be 112 Read+Write tool-calls). All edits and analyses follow strict convention.

## Code-Symbol Analysis (CLAUDE.md)
- **Serena ALWAYS first** for class/method/property navigation: `find_symbol`, `get_symbols_overview`, `find_referencing_symbols`
- `rename_symbol` for refactoring (NOT manual search/replace)
- Grep/Glob only for non-code (XML, JSON, YAML, MD)
- Read on a whole file ONLY when symbolic search is insufficient
