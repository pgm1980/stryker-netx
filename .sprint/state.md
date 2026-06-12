---
current_sprint: "182"
sprint_goal: "Fix-Sprint 4/6 (Robustheit, Fahrplan sprint_178_synthesis.md): #277a (RegexMutator-Cast-Crash auf interpolierten Patterns), #277b (try/catch-Robustheitsschicht um mutator.Mutate — ein Mutator-Bug darf den Lauf nie töten), #288 (Loop-Bound im Roslyn-NRE-Retry des CsharpCompilingProcess — Hang-Klasse), #297a/b/c (Hang-Epic Runner-Kette: VsTest-Pool-Init fire-and-forget, Discovery-Wait ohne Timeout, MTP-Disconnect komplettiert Listener nicht), Parse-Guards H-19 (bool/int.Parse, Referenz-Existenz, SolutionFile-Catch-Set) + G-37b (Baseline-Enum.TryParse). TDD je Fix; Serena-first für Analyse UND Implementierung; Serena-Memory vor/nach Sprint. Erfolgsmaß: Crash-Probe interpolierter Regex (Level Advanced) → Exit 0 + WARN statt Exit 127. Ship: PR → Squash → Tag v3.3.7 → Release → Closing."
branch: "feature/182-robustness"
started_at: "2026-06-12"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 182 (Robustheit)

## Fix-Liste

| Fix | Befund | Ort | Status |
|-----|--------|-----|--------|
| 1 | #277a | RegexMutator — is-Pattern statt hartem Cast; interpolierte Patterns unmutiert | ☑ |
| 2 | #277b | SafelyMutate-Guard (Materialisierung IM try; OCE propagiert; WARN mit Mutator+NodeKind) | ☑ |
| 3 | #288 | HasScanProgress (Tree-Referenzvergleich) + MaxNreScanRounds=5 → CompilationException | ☑ |
| 4 | #297a | Pool beobachtet Runner-Bau-Fehler; RunThis nach MTP-Muster (1s-Poll, 5-Min-Cap, Fail-fast) | ☑ |
| 5 | #297b | WaitEnd(TimeSpan) — Timeout in den Aborted-Pfad; Default 5 Min | ☑ |
| 6 | #297c | ResponseListener.Fail (TrySet-Semantik) + FailAllListeners bei Disconnect (IOException) | ☑ |
| 7 | H-19 | TryParse-Guards (bool/int) + File.Exists vor CreateFromFile + InputException für „no serializer" | ☑ |
| 8 | G-37b | BaselineMutantFilter Enum.TryParse — unbekannter Status → Mutant bleibt Pending | ☑ |

## Erfolgsmaße
- Crash-Probe (interpolierter Regex, `--mutation-level Advanced` auf Probe-Projekt): Exit 0 + WARN statt Exit 127
- Je Fix Red→Green; Build 0/0; Vollsuite grün; Semgrep 0

## Notizen
- #297a-Vorbild: MicrosoftTestPlatformRunnerPool (synchrones Parallel.For im Ctor + 5-Min-Timeout mit Wartelogs)
- #288: äußere MaxAttempt=50 zählt Rollback-Runden, NICHT NRE-Retries — eigener Bound nötig
- G-29/G-30 (Generator-Trees, „get get"-DisplayName) bleiben außen vor (P3, #302-Liste)
