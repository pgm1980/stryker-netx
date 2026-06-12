---
current_sprint: "180"
sprint_goal: "Fix-Sprint 2/6 (CE-Noise I, Fahrplan sprint_178_synthesis.md): #278 (IsPatternExpressionMutator skippt Designation-Patterns — CS0165-Klasse), #284 (Pattern-interne Mutationen designation-aware auf Block-Level + ContainsDeclarations um VarPattern/Recursive-/ListPattern-Designation erweitert — CS0128-Klasse), #285 (StaticInitializerMarkerEngine skippt unmutierte Initializer + ExpressionBodiedProperty-Pfad auf MustInjectCoverageLogic gegated — TrackValue-CE/Heal-Klasse). Manifest-Pin 3.3.2→3.3.4. TDD je Fix; Serena-first für alle Code-Analysen; Serena-Memory vor/nach Sprint aktualisiert. Erfolgsmaß: Probe-1-CE-Rate 62,5 %→<30 %, keine „mutant −1"-Rollback-Runde im Debug-Log. Ship: PR → Squash → Tag v3.3.5 → Release → Closing."
branch: "feature/180-ce-noise-1"
started_at: "2026-06-12"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 180 (CE-Noise I)

## Fix-Liste

| Fix | Issue | Ort | Status |
|-----|-------|-----|--------|
| 0 | Pin | .config/dotnet-tools.json 3.3.2→3.3.4 | ☑ |
| 1 | #278 | Mutators/IsPatternExpressionMutator: Skip bei SingleVariableDesignation im Pattern | ☐ |
| 2 | #284a | Neuer PatternOrchestrator: Block-Store, wenn IsPattern-Wurzel Designation trägt | ☐ |
| 3 | #284b | RoslynHelper.ContainsDeclarations + VarPattern/Recursive-/ListPattern-Designation | ☐ |
| 4 | #285a | StaticInitializerMarkerEngine: Skip ohne Mutations-Annotationen im Initializer | ☐ |
| 5 | #285b | ExpressionBodiedPropertyOrchestrator: Marker nur bei MustInjectCoverageLogic | ☐ |

## Erfolgsmaße
- Probe-1 (lokale CLI): CE-Rate < 30 % (Baseline 62,5 % nach Sprint 179)
- Probe-Debug-Log: keine „Found mutant -1 … StaticInitializerMarkerEngine"-Zeile
- Build 0/0, Vollsuite grün, Semgrep 0
