# Sprint 45 — Investigation: 18 Cross-Sprint Behaviour-Delta Skips

**Tag:** v2.32.0 | **Branch:** `feature/45-investigation-behaviour-delta-skips`

## Outcome
- **Documentation-only sprint** (no code changes).
- Produced full analysis report: `_docs/sprint_45_investigation.md`.
- Solution-wide unchanged: 816 green + 18 skip excl E2E.

## Decision Matrix Summary
| Decision | Count |
|----------|-------|
| WON'T-FIX-DOC | 5 |
| WON'T-FIX-PERMANENT | 1 |
| CANDIDATE-FIX | 5 |
| DEFER | 7 |

## Lessons (NEW)
- **Sprint 25 prediction validated**: 18/18 skips fall into 4 distinct categories — uniform-skip-with-reason was the right strategy. Per-test investigation in original sprints would have cost 5-11h × 18 = 90-200h vs ~30 min decision-matrix in dedicated sprint.
- **Investigation Sprint pattern established**: documentation-only deliverable produces actionable sub-sprint plans without polluting current sprint scope. Repeatable for future skip-pile-up situations.
- **Deferred infrastructure investments unblock multiple skips**: MTP-mock-server harness alone unblocks 5 Sprint-34 tests; IProjectAnalysis-mock-builder unblocks 1 Sprint-29 test + sets foundation for future Stryker.Core.UnitTest tranches.

## Roadmap (post-Investigation)
- **Sprint 46+**: Stryker.Core.UnitTest tranches (largest module: ~25k LOC, 161 files; needs 10+ sub-sprints with decomposition by directory)
- **Optional sub-sprints (from Investigation recommendations)**:
  - VsTest-Refactor (~1 day, 5 CANDIDATE-FIX tests)
  - Skip-message update (~2h, 5 WON'T-FIX-DOC)
  - MTP-mock-server harness (~3 days, unblocks 5 DEFER)
  - IProjectAnalysis-mock-builder (~2 days, unblocks 1 DEFER + foundation for Stryker.Core.UnitTest)
