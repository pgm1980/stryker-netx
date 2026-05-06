---
current_sprint: "149"
sprint_goal: "Bug #6 P1 (--reporters Plural-Alias): args-pre-processor in StrykerCli rewriten --reporters → --reporter. Calculator-Tester Bug-Report 4. ADR-030. v3.2.4."
branch: "feature/149-bug6-reporters-plural-alias"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 149 in progress (Bug #6 --reporters plural alias)

## Bug-Report 4 Bug #6 — der Auftrag

Calculator-Tester Bug-Report 4: externe Tutorials und unsere historische Doku zeigen `--reporters html` (Plural). Tool kennt nur `--reporter` (Singular) und lehnt mit `Unrecognized option` ab. User-Forderung: Plural-Alias akzeptieren ODER Doku überall korrigieren.

## Sprint 149 — Option A (args-Pre-Processor, 3-Schritte Maxential)

**Decision:** args-Pre-Processor in `StrykerCli.RunAsync` rewriten `--reporters` → `--reporter` BEFORE McMaster.
- Konsistent mit Sprint-148-Pattern (`TryHandleToolVersionFlag` Pre-Processor).
- Drei Argv-Forms: spaced (`--reporters html`), `=`-separated (`--reporters=html`), `:`-separated (`--reporters:html`).
- False-Positive-Guard: `--reportersx` matcht nicht.

## Sprint-149-Phasen

- **Phase A** ✅ Maxential 3-Schritte (Option A vs B vs C, A gewählt)
- **Phase D1** ✅ `RewriteReportersAlias(string[]) → string[]` + `TryRewriteReporterArg` Helper in StrykerCli.cs
- **Phase D2** ✅ 10 neue Tests (5 Rewrite-Theory + 4 Non-Rewrite-Theory + 1 E2E-Fact)
- **Phase F** ✅ Solution-wide build (0 W / 0 E), 844 Unit-Tests grün, Semgrep clean
- **Phase G** ✅ ADR-030 + 0.15.0 history-row geschrieben
- **Phase H** PR + merge + tag v3.2.4

## Sprint-Plan für Bug-Report-4

- **Sprint 147** ✅ closed: Bug #9 P0 (v3.2.2 ADR-028)
- **Sprint 148** ✅ closed: Bug #4 P1 (v3.2.3 ADR-029)
- **Sprint 149 (jetzt)**: Bug #6 P1 (`--reporters` plural)
- **Sprint 150**: Bug #8 P1 (Multi-Project UX)
