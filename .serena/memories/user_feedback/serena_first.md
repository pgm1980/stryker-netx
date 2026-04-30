# User-Feedback: Serena-First für Code-Analyse (Sprint 1 Phase 1)

User-Hinweis vom 2026-04-30:
> „Bitte alles strikt nach CLAUDE.md, was auch Aktivierung und Onboarding sowie insb. Code-Analyse auf Serena einschließt."

> „Ich werde dich im weiteren Verlauf immer wieder auf Serena Memory und für Code-Analyse hinweisen. Leider scheint es so zu sein, dass Claude Code Sessions das immer mal wieder vergessen und auf Grep sowie Read zurückfallen, was zu Fehlern und extrem langen Bearbeitungszeiten führt. Serena mit den Symbols ist einfach maximal überlegen."

## Pflicht-Praxis (höchste Priorität)

Bei JEDER Berührung von .cs-Files:
1. **Erst** `get_symbols_overview` (neue Files) oder `find_symbol` (bekannte Symbole)
2. Refactor: `rename_symbol`, `replace_symbol_body`, `insert_before_symbol`, `insert_after_symbol`
3. Cross-Refs: `find_referencing_symbols`
4. Read auf ganze Datei NUR wenn vollständige sequentielle Verarbeitung nötig
5. Grep NUR für Nicht-Symbol-Text in .cs (Kommentar-Patterns, Compiler-Error-Suche), NIE für Klassen/Methoden/Properties
6. Bei Session-Start: `check_onboarding_performed` + ggf. `read_memory` auf passende Memories

Diese Regel hat höhere Priorität als Convenience oder vermeintliche Geschwindigkeit.
