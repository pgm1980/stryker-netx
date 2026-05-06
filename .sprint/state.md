---
current_sprint: "152"
sprint_goal: "CI Integration Matrix Flakes — fix build+test (ubuntu/windows) Linux-Path-Issues. ADR-036 in-repo test-fixtures + cross-platform paths. v3.2.7."
branch: "feature/152-ci-integration-matrix-flakes"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 152 in progress (CI build+test fix)

## User-Direktive: 7-Item-Backlog autonom

User-Anweisung: "Ab jetzt alles, was auf der Agenda steht, autonom und strikt nach CLAUDE.md".

7-Item Sprint-Roadmap (Maxential-priorisiert):
- Sprint 152 (jetzt): CI build+test fix — höchster Hebel weil amortisiert über alle künftigen PRs
- Sprint 153: Combined Multi-Project Report Aggregation (ADR-033)
- Sprint 154: JsonReport full AOT-trim (ADR-034)
- Sprint 155: RoslynDiagnostics v2
- Sprint 156: Issue #191 closure (MutationTestProcessTests port)
- ADR-035: TypeSyntax-Engine + HotSwap-incremental status-quo-confirmation (no sprint)

## Sprint 152 — CI build+test fix (Maxential 4-Schritte branchless)

**Decision:** ADR-036 — vendor Stryker.slnx as in-repo test-resource + cross-platform Path.Combine in builder tests.

Two structural failure classes in `build + test (ubuntu/windows)` jobs:
- Class A (Stryker.Solutions.Tests, 4 tests): `_references/stryker-net/src/Stryker.slnx` not in CI checkout (`.gitignore`-excluded) → DirectoryNotFoundException
- Class B (ProjectAnalysisMockBuilderTests, 1 test): hardcoded Windows-Path `c:\\src\\MyProject.csproj` → backslash not path-separator on Linux/macOS → wrong AssemblyName derivation

Class C (~25 macOS/Ubuntu integration-test failures with `extern alias TheLog` compile-error) is a Stryker-mutation-engine-regression on integration TargetProject — **honest deferred** to Sprint-153+ separate investigation.

## Sprint-152-Phasen

- **Phase A** ✅ Maxential 4-Schritte: 2 fixable classes A+B in scope, Class C deferred
- **Phase B** ✅ Class A: `tests/Stryker.Solutions.Tests/TestResources/UpstreamStryker.slnx` (in-repo) + csproj `<None CopyToOutputDirectory>`. Tests use `Path.Combine(AppContext.BaseDirectory, "TestResources", "UpstreamStryker.slnx")`.
- **Phase C** ✅ Class B: 4 hardcoded Windows-paths in ProjectAnalysisMockBuilderTests refactored to `Path.Combine(...)` static-readonly fields.
- **Phase D** ✅ Solution-wide build (0 W / 0 E), 2047 Tests grün lokal (±0 vs Sprint 151 — only fixes for existing tests), Semgrep clean
- **Phase E** ✅ ADR-036 + 0.18.0 history-row written
- **Phase F** PR + CI verification (expect build+test (ubuntu+windows) GREEN) + merge + tag v3.2.7
