# User-Feedback: Serena-First für Code-Analyse

User-Hinweis (2026-04-30, bekräftigt 2026-06-12 durch Neueinrichtung des Servers):
> „Bitte alles strikt nach CLAUDE.md, was auch Aktivierung und Onboarding sowie insb. Code-Analyse auf Serena einschließt."
> „Claude-Code-Sessions vergessen das immer mal wieder und fallen auf Grep/Read zurück, was zu Fehlern und extrem langen Bearbeitungszeiten führt. Serena mit den Symbols ist einfach maximal überlegen."

## Pflicht-Praxis (höchste Priorität)
Bei JEDER Berührung von .cs-Files:
1. **Erst** `get_symbols_overview` (neue Files) oder `find_symbol` (bekannte Symbole)
2. Refactor: `rename_symbol`, `replace_symbol_body`, `insert_before/after_symbol`
3. Cross-Refs: `find_referencing_symbols`
4. Read auf ganze Datei NUR bei nötiger vollständiger Sequenz-Verarbeitung
5. Grep NIE für Klassen/Methoden/Properties — nur für Nicht-Symbol-Text
6. Session-Start: `activate_project` („stryker-netx") + `check_onboarding_performed` + passende Memories

Historische Ausnahme: Sprints 170–179 liefen mit dokumentiertem Fallback (Server kannte das
Projekt nicht; für die 360°-Volltext-Analyse war Read ohnehin die Methode). Seit 2026-06-12
ist der Server neu eingerichtet — **Fallback-Ära beendet, Serena-first gilt wieder voll.**

## Betriebs-Eigenheiten dieses Setups (2026-06-12 verifiziert)
- Standalone-SSE-Server `localhost:9121` hinter `SerenaMcpProxy.exe`; Projekt server-seitig
  als `/workspace/stryker-netx` gemappt → in Tool-Calls IMMER relative Pfade
- **Calls strikt sequenziell** — parallele Serena-Calls erzeugen Timeout (-32001) /
  „Server unavailable"; nach `activate_project` lädt der C#-LSP die 25-Projekt-Solution,
  erster Folge-Call kann timeouten → einmal wiederholen
- Nach Server-NEUSTART ist die MCP-Session dieser Claude-Instanz stale → User muss
  reconnecten (/mcp) — Symptom: ALLE Serena-Calls werfen sofort generische Errors
- `find_symbol` inkl. Hover-Info funktioniert sofort; `find_referencing_symbols` ebenfalls
  (verifiziert via MutantPlacer/AddEndingReturn → 2 Treffer mit Snippets)
- **Verifizierte Limitation:** Aufrufstellen von EXTENSION-Methoden in Extension-Form
  (`node.ContainsDeclarations()`) werden NICHT als Referenzen aufgelöst (leeres Ergebnis).
  Für Extension-Symbole (RoslynHelper, IProjectAnalysis*Extensions!) als Cross-Ref-Fallback
  `search_for_pattern` mit dem Methodennamen nutzen — relevant ab Sprint 180 (#284)
