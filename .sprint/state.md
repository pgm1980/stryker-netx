---
current_sprint: "146"
sprint_goal: "Hotfix v3.2.1 — Calculator-Tester Report 3: Bug-9 v3.2.0 Skip-list-Gap. UoiMutator IsInTypeSyntaxPosition erweitert um DeclarationPattern + TypePattern + RecursivePattern + TypeParameterConstraintClause."
branch: "fix/146-bug9-typesyntax-pattern-slots"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 146 in progress (v3.2.1 hotfix)

## Sprint 146 — Calculator-Tester Bug Report 3

**Befund:** Calculator-Tester Report 3 (2026-05-06, ~6h nach v3.2.0-Release)
zeigt Bug-9 reproduziert in v3.2.0 mit gleichem `InvalidCastException(
ParenthesizedExpression → TypeSyntax)` Stack-Trace wie v3.1.1. Phase 1+2
hatten den Crash für SimpleName + CAE-Klasse gefixt aber TypeSyntax-Skip-Liste
war unvollständig.

## Root-Cause

Phase-2 `UoiMutator.IsInTypeSyntaxPosition` switch hatte 4 fehlende arms:
1. `DeclarationPatternSyntax` — `t is Deposit d` (Calculator's repro)
2. `TypePatternSyntax` — `t is Deposit or Withdrawal` (defensive)
3. `RecursivePatternSyntax` — `t is Deposit { Amount: 5 }`
4. `TypeParameterConstraintClauseSyntax` — `where T : class` (lokal entdeckt)

Sample.Library hat keine dieser Patterns. Calculator.Domain (records +
switch-expressions) trifft sie. Real-World-Code mit C# 9+ pattern-matching
generell.

## Fix

`IsInTypeSyntaxPosition` switch um 4 arms erweitert. KEINE anderen Code-
Logic-Änderungen. Tests: 14 UoiMutator-Tests grün (4 neue Pattern-Slot-
Regressions).

## Verifikation

- Lokaler Repro `t switch { Deposit d => ... }`: kein Crash, Score 68.89%.
- Solution-wide build: 0 warnings, 0 errors.
- Solution-wide tests: ~2200 grün.
- Semgrep: clean.

## Phase-3-Skip-as-Architecture bleibt

Sprint 146 ist ein punktueller Hotfix der die Phase-3-Skip-Liste vervollständigt.
ADR-027 Phase 3 Decision (Skip-as-Architecture) bleibt unverändert; nur die
Skip-Patterns waren nicht complete genug.

## Note für mögliche v3.3.0+ Sprints

Lokal-Bisect hat ZWEITEN Bug aufgedeckt: GenericConstraintMutator (All-only)
emittiert `OriginalNode = MethodDeclarationSyntax` mit Type=Statement; bei
expression-body Methods landet die Mutation auf child-Expression-Inject-Frame
das `sourceNode.Contains(MethodDecl)` Check fail-t. Anderes Exception-Class
(`InvalidOperationException: Cannot inject`) als Calculator's Bug-9
(`InvalidCastException`). NICHT in dieser Hotfix-Scope.
