---
current_sprint: "179"
sprint_goal: "Fix-Sprint 1/6 (Fahrplan sprint_178_synthesis.md): Quick-Wins + P1. TDD je Fix (Red-Test aus Register-Befund): #282 P1 (out-Lambda-NRE → default-Literal), #295 (&=~ → |=), #294 (Contains is false → is true), #283 (DoNotMutate CaseSwitchLabel + goto case), G-01/#279-Teil (AddEndingReturn in Mutations-Pfad), #292 (Enrichment-Guards), #274 (Config-Key raus), H-05/J-11 (#302-Einzeiler), #273 (Test-Toleranz). Verifikation: Build 0/0, Tests grün, Semgrep, Probe P-4 grün + Probe-1-CE-Rate sinkt (lokale CLI). Ship: PR → Squash → Tag v3.3.4 auf Merge-Commit → Release → Nightly-Dispatch (Ziel 11/11) → Closing-PR."
branch: "feature/179-quick-wins-p1"
started_at: "2026-06-11"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 179 (Quick-Wins + P1)

## Fix-Liste (aus sprint_178_synthesis.md §3/§4)

| Fix | Issue | Ort | Status |
|-----|-------|-----|--------|
| 1 | #294 | TestRunner/Tests/TestIdentifierList.cs:44 `is false`→`is true` | ☐ |
| 2 | #295 | TestRunner.VsTest/VsTestContextInformation.cs:279 `&= ~`→`\|=` | ☐ |
| 3 | #282 P1 | Instrumentation/DefaultInitializationEngine.cs:57 default-Literal bei Type==null | ☐ |
| 4 | G-01 | CsharpNodeOrchestrators/BaseFunctionOrchestrator.cs:140 AddEndingReturn | ☐ |
| 5 | #283 | CsharpMutantOrchestrator-Liste: DoNotMutate CaseSwitchLabel + goto-case | ☐ |
| 6 | #292 | Initialisation/ProjectMutator.cs:46–48 Guards | ☐ |
| 7 | H-05 | InputFileResolver.FindProjectFile:847 Extension-Check | ☐ |
| 8 | J-11 | CLI/ConfigBuilder.cs:53 fullPath | ☐ |
| 9 | #273 | Core.Tests InitialTestProcess-Toleranz | ☐ |
| 10 | #274 | MTP-Modul stryker-config.json Key raus | ☐ |

## Erfolgsmaße

- Build 0/0, alle Tests grün, Semgrep 0
- P-4-Probe (out-Lambda) gegen LOKALE CLI: Exit 0 statt 127
- Probe-1-Re-Run: case-Label-Mutanten nicht mehr CE; Block-CS0161-CEs weg (G-01)
- Nach Merge: Nightly-Dispatch → MTP-Modul grün (11/11)
