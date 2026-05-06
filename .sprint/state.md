---
current_sprint: "143"
sprint_goal: "v3.2.0-dev Phase 1 — type-position-aware mutation control: smart-pivot for MA.Name slot (UoiMutator + MemberAccessNameSlotOrchestrator + Mutator-set OriginalNode). KEIN Tag — multi-sprint refactor."
branch: "feature/143-engine-refactor-part1-uoi-statement-lift"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 143 in progress (v3.2.0-dev Phase 1)

## Sprint 143 — ADR-027 Phase 1 implemented

User-Pushback aus Sprint 142 closing-review: "Hotfix-Skip ist symptomatisch, der Fehler hätte durch Engine-Rewrite (type-position-aware) entfernt werden müssen." → Multi-Sprint Engine-Refaktor; **KEIN v3.2.0 Tag** bis alle 3 Phasen fertig sind.

### Phase 1 Implementiert (Sprint 143)
- `CsharpMutantOrchestrator.GenerateMutationsForNode`: `mutation.OriginalNode = current` → `mutation.OriginalNode ??= current`. Mutatoren dürfen explizit eine Eltern-Node setzen.
- `UoiMutator`: smart-pivot für `MA.Name` (data.Length). `OriginalNode = parent MA`, `ReplacementNode = PostfixUnary(MA)`. Sprint-142-Skip für MA.Name aus IsSafeToWrap entfernt; MB.Name-Skip bleibt (Phase 2).
- `MemberAccessNameSlotOrchestrator` (neu): defers injection für SimpleName in MA.Name-Slot über `MutationControl.MemberAccess`. Mutationen blubbern in den umschließenden MA-Frame, dessen Inject-Call dann `sourceNode.InjectMutation(mutation)` mit valid Contains-check macht.
- `CsharpMutantOrchestrator` BuildOrchestratorList: globalen DoNotMutate<SimpleName> für (MA.Name || MB.Name) auf nur MB.Name reduziert.
- ADR-027 (Phase 1 detailed + Phase 2/3 skizziert) + 0.9.0 history entry.
- Lokaler Bisect-Trail mit temporärer SpanTester.cs (vor commit entfernt — würde E2E-Baseline `Defaults_ProducesExpectedTotalAndScore = 5` brechen).
- 3 UoiMutator-Tests adapted: `MutatesAtParentLevel_RightHandOfMemberAccess`, `StillMutates_LocalIdentifierInExpression`, `DoesNotMutate_RightHandOfMemberBinding` (Phase-2-deferred).

### Verifikation
- Lokal repro `--mutation-profile All --mutation-level Complete` auf Sample.Tests: läuft sauber durch, kein Crash. Calculator-Baseline 30/14, SpanTester 28 testbare Mutations (1 killed + 27 survived/compile-error klassifiziert).
- Solution-wide build: 0 Warnings, 0 Errors.
- Solution-wide tests grün (RedirectDebugAssert ist pre-existing nicht-deterministischer Flake aus Sprint 27).
- Semgrep: 0 Findings auf 6 modifizierten Dateien.

### Phase 2 + Phase 3 (geplant, kein Sprint-Commitment)
- **Phase 2 (Sprint TBD)**: `MB.Name` (`data?.Length`) → CAE-aware Lifting. Pivot lift bis zur umschließenden ConditionalAccessExpression.
- **Phase 3 (Sprint TBD)**: TypeSyntax-Engine für `SpanReadOnlySpanDeclarationMutator` re-enable.

## Open follow-ups
- Bug #4 (`--version` Tester-Sicht) + Bug #6 (`--reporters` plural Alias) bleiben aus Sprint 142 offen.
- ADR-027 Phase 2 + 3 Sprints.

## Cumulative Session-Stats (Sprints 95-143)
- 49 Sprints, 47 Releases (kein neuer Tag in Sprint 143)
- ADR-027 ist erste explizit Multi-Sprint-ADR mit Phasenplan
