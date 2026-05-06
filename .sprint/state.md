---
current_sprint: "155"
sprint_goal: "RoslynSemanticDiagnostics v2 — StatementSyntax-Coverage via TryGetSpeculativeSemanticModel + descendant-walk. Schließt Sprint-16-deferred-Item. ADR-037. v3.2.9."
branch: "feature/155-roslyn-semantic-statements"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 155 in progress (RoslynDiagnostics v2)

## Sprint 155 — ADR-037 (Maxential 4-Schritte, 3 ToT-Branches)

**Decision:** S2 (TryGetSpeculativeSemanticModel + descendant-walk via GetSymbolInfo) gewählt.

**Verworfen:**
- S1 (TryGetSpeculativeSemanticModel + GetDiagnostics): NotSupportedException aus SpeculativeSemanticModelWithMemberModel.GetDiagnostics
- S3 (Compilation.AddSyntaxTrees per-mutation): O(parse + bind) statt O(1)

## Sprint-155-Phasen

- **Phase A** ✅ Maxential 4-Schritte (S1 test-failure → S2 chosen)
- **Phase B** ✅ Code-Audit Sprint-17 RoslynSemanticDiagnosticsEquivalenceFilter — Statement+Declaration explicit out-of-scope per Sprint-16-deferral
- **Phase C** ✅ IsEquivalent switch-pattern: ExpressionSyntax → IsEquivalentExpression (Sprint 17 unchanged), StatementSyntax → IsEquivalentStatement (NEU), Declaration → false
- **Phase D** ✅ TryGetSpeculativeSemanticModel + descendant-walk + Sprint-137 MemberBindingExpression-skip
- **Phase E** ✅ 6 RoslynSemanticDiagnostics-Tests grün (1 renamed + 1 neu)
- **Phase F** ✅ Solution-wide build (0 W / 0 E), Semgrep clean
- **Phase G** ✅ ADR-037 + 0.21.0 history
- **Phase H** PR + merge + tag v3.2.9

## Backlog-Status nach Sprint 155

- ✓ Item 1 (JsonReport AOT-trim) — Sprint 154 / ADR-034
- ✓ Item 2 (RoslynDiagnostics v2) — Sprint 155 / ADR-037
- ✓ Item 3 (TypeSyntax-Engine) — ADR-035 status-quo
- ✓ Item 4 (HotSwap-incremental) — ADR-035 status-quo
- ✓ Item 5 (CI flakes Class A+B+D) — Sprint 152 / ADR-036; Class C deferred
- ✓ Item 7 (Combined Report) — ADR-033 discovery

Remaining: Item 6 (Issue #191 MutationTestProcessTests port) — Sprint 156
