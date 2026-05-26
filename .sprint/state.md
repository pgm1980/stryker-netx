---
current_sprint: "169"
sprint_goal: "P0 ConstantReplacementMutator type-aware emission — extends ADR-047 to the SECOND constant-emitting mutator (Sprint 14 / v2.1.0). Reporter's BUG_REPORT_9_FOLLOWUP_2 categorised 196 residual Safe Mode! warnings on v3.3.0: 184 A.1 (nested-ternary double) + 3 A.2 + 1 A.3, all rooted in ConstantReplacementMutator.MakeNumericMutation calling Convert.ToDouble + Literal(string, double). Same fix shape as ADR-047 Branch B: switch-expression on object newValue → typed SyntaxFactory.Literal(T). Target tag v3.3.1 (patch — same surface as v3.3.0, no API break)."
branch: "feature/169-ternary-emit-typed-mutation-placer"
started_at: "2026-05-25"
housekeeping_done: false
memory_updated: false
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 169 (v3.3.1 prep)

## Trigger

Reporter follow-up: BUG_REPORT_9_FOLLOWUP_2 (filesystem-mcp-server, 2026-05-25)
archived at `_bug_reporting/BUG_REPORT_9_FOLLOWUP_2_extract.md`. Their v3.3.0
validation: 196 residual Safe Mode! warnings + 31.14 % CompileError rate
(vs 39 % pre-v3.3.0 baseline, vs single-digit target). Pass criteria for
v3.3.1: < 20 Safe Mode warnings + < 15 % CompileError rate.

## Reporter's diagnosis vs actual root cause

**Reporter's hypothesis (educated guess from outside):** the nested ternary
wrapper `^(IsActive(?-1:?0:?2:1)` needs a target-type cast outer the whole
expression. Suggested fix shape would have added a `CastExpression` around
the `ConditionalInstrumentationEngine`'s emitted wrapper.

**Actual root cause (verified from inside the codebase):** the issue is
NOT in the wrapper — it's in the SECOND constant-emitting mutator,
`ConstantReplacementMutator` (Sprint 14 / v2.1.0, PIT CRCR). That mutator
still calls `Convert.ToDouble + Literal(string, double)` at
`src/Stryker.Core/Mutators/ConstantReplacementMutator.cs:104-118` — exactly
the anti-pattern ADR-047 fixed in `InlineConstantsMutator`. Only one of
the two mutators was fixed in Sprint 168.

Concrete trace of reporter's exemplar `?-1:?0:?2:1` with source-literal `1`:

| Leaf | Source | Emitter | Type after Sprint 168 |
|------|--------|---------|----------------------|
| `2` | `1` | `InlineConstantsMutator` (c+1) | ✅ int (typed Literal(int)) |
| `0` | `1` | `InlineConstantsMutator` (c-1) | ✅ int (typed Literal(int)) |
| `-1` | `1` | `ConstantReplacementMutator` (→-1 axis) | ❌ **double** (still Convert.ToDouble) |
| `1` | `1` | (original, user-written) | ✅ int |

The ConditionalExpression's common-type inference picks `double` because
one branch is double → entire ternary typed `double` → CS0029 on the
int slot. The wrapper is correct; the leaf is wrong.

## Fix scope

Apply the ADR-047 Branch B pattern to `ConstantReplacementMutator`:
- Replace `MakeNumericMutation(IConvertible)` with `Make(object)` +
  `switch`-expression dispatching to typed `SyntaxFactory.Literal(T)`.
- Extend coverage from {int, long, float, double} → {int, uint, long,
  ulong, float, double, decimal} for parity with the InlineConstants
  catalogue.
- Handle `decimal.MaxValue + 1m` / `decimal.MinValue - 1m` overflow with
  try/catch silent-skip (same as ADR-047).

**Expected impact on reporter's codebase:**
- A.1 (184) + A.2 (3) + A.3 (1) all close = **188 of 196 = 95.9 %**
- CompileError rate drops from 31.14 % toward typical 5-10 %.

## Out of scope (B.1/B.2/D in reporter's table — honest-deferred)

- B.1 (3 sites): `byte[].AsSpan` overload mismatch in `AsSpanAsMemoryMutator`
- B.2 (1 site): `byte[].AsMemory` overload mismatch in same mutator
- D (4 sites): `PluginManager` CS0165/CS0161 — likely separate codegen
  issue in a block-removal mutator (CS0165 = unassigned local, CS0161 = no
  return value from method-with-non-void-return). Different code-path from
  numeric-literal emission.

These are the remaining 8 of 196 = 4.1 %. Reporter explicitly said the
ternary fix alone "closes our pain point for incremental TDD on this
codebase" — defer until they raise a re-test follow-up.

## Status

- [ ] Branch `feature/169-ternary-emit-typed-mutation-placer` opened
- [ ] Maxential confirmation of root-cause + Branch-B-mirror plan
- [ ] P0 implementation (ConstantReplacementMutator type-aware) + TDD-first regression tests
- [ ] ADR-049 (extends ADR-047 to ConstantReplacementMutator)
- [ ] Build/test/semgrep green
- [ ] PR + merge + tag v3.3.1 + release + NuGet
- [ ] MEMORY.md `project_sprint169_closed.md` + index entry
