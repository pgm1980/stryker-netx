---
current_sprint: "168"
sprint_goal: "P0 InlineConstantsMutator type-aware literal emission (closes Bug Report #9 Anomaly #4 — 70+ Safe Mode! warnings, ~39% CompileError rate on real codebases). P1 Doku-Update (README + CLAUDE.md): testhost lock workaround, cold-run wall-clock, dotnet-tools.json rollForward pitfall. P2 ADR-047 documenting Anomaly #7 coverage-instance limitation. Target tag v3.3.0 (minor — additive type-system extension in mutator; no API break)."
branch: "feature/168-mutator-type-awareness-and-doc-improvements"
started_at: "2026-05-25"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 168 (v3.3.0 prep)

## Trigger

Bug Report #9 from filesystem-mcp-server Sprint 56 (2026-05-25), filed at
`_bug_reporting/BUG_REPORT_FOR_STRYKERNETX.md`. 7 items total; triage
result:

| # | Item | Resolution |
|---|---|---|
| Bug #1 | `--mutate` trees[N] | ✅ Already fixed in v3.2.19 (Sprint 167 PR #259); reporter's `rollForward=false` tool-manifest pinned them on v3.2.18 |
| Bug #2 | testhost.exe DLL file-lock | ❌ Not stryker-netx (vstest-internal); P1 doc-update |
| Anomaly #3 | 157 false-fail tests cold-run | ❓ Not Stryker code bug (concurrency default is already `ProcessorCount/2`); P1 doc-update |
| **Anomaly #4** | **Safe Mode! `double` cascade (70+ methods)** | **P0 — confirmed bug in `InlineConstantsMutator.MakeNumericMutation`** |
| Anomaly #5 | `--reporters` plural rejected | ✅ Already supported (`RewriteReportersAlias` in `StrykerCli.cs:66+119`) |
| Anomaly #6 | 5× cold-run variance | ❌ JIT warm-up (universal); P1 doc-update |
| **Anomaly #7** | **Mock<ILogger> coverage matrix miss** | **P2 — confirmed design limit; ADR-047 documenting workaround pattern** |

## P0 — InlineConstantsMutator type-awareness

### Root cause

`InlineConstantsMutator.MakeNumericMutation` at `src/Stryker.Core/Mutators/InlineConstantsMutator.cs:63-68`
calls:

```csharp
SyntaxFactory.Literal(Convert.ToString(newValue, ...)!,
                      Convert.ToDouble(newValue, ...))     // double overload
```

Roslyn's `SyntaxFactory.Literal(string, double)` overload **always** emits a
`NumericLiteralToken` with `Token.Value` of type `double`. Even though the
outer `ApplyMutations` switch correctly recognises the original `token.Value` is
`int`/`long`/`float`, the emitted mutation discards that type and re-encodes
as double.

Roslyn then refuses `int x = 6.0;` with CS0266 → Stryker enters Safe Mode and
drops **all** mutants in that method (cascading effect: 70+ methods × ~10
mutants each = ~700 false-CompileError mutants on the reporter's codebase).

### Planned fix

Replace the generic `MakeNumericMutation(IConvertible)` helper with typed
helpers / `switch` on the original `token.Value`:

```csharp
// int → Literal(int)
// long → Literal(long)
// double → Literal(double)
// float → Literal(float)
// decimal → Literal(decimal)
```

Roslyn has dedicated overloads per type that emit the correct `Token.Value`
type and the matching literal suffix (`L`, `f`, `m`).

Also extend coverage to `decimal` literals (currently the switch only handles
4 types). The current outer switch already discriminates correctly — we just
need to fix the emit-side.

### Out of scope

- Other "Safe Mode!" patterns mentioned in the bug report (`byte[].AsSpan`
  overload mismatch, `char → string` mutations, `^(-1:0:2:1)` ternary
  emission) — these are *different* mutators with separate code-paths.
  Filed as follow-up items in MEMORY.md `sprint168_closed.md`.

## P1 — Doc-updates

- `README.md`: testhost-lock workaround (`--output <separate-path>`),
  cold-run wall-clock note, `dotnet-tools.json` rollForward pitfall.
- No CLAUDE.md changes planned (project-internal; the doc-updates are
  user-facing).

## P2 — ADR-047 (informational, no code change)

Documents the coverage-matrix instance-attribution limitation observed in
Anomaly #7. Recommends class-fixture `Mock<ILogger>` pattern as the
workaround. Marks proper SUT-instance-aware coverage as v3.4.x+ candidate.

## Status

- [x] Branch `feature/168-mutator-type-awareness-and-doc-improvements` opened
- [x] Maxential 14 thoughts + 3 ToT branches A/B/C → B chosen 0.875 + merged
- [x] P0 implementation (InlineConstantsMutator type-aware) + 12 regression tests TDD-first
- [x] P1 doc-updates (README known-limitations 4 new bullets)
- [x] P2 ADR-047 + ADR-048 written to architecture_specification.md + Änderungshistorie entry 0.31.0
- [x] Build 0/0, tests 2121 green (+16), semgrep clean
- [x] PR #261 squash-merged (`f5b537e` on main); Tag `v3.3.0` pushed; release.yml fired: NuGet HTTP 201 Created (1.5s), GitHub Release with `dotnet-stryker-netx.3.3.0.nupkg` asset
- [x] MEMORY.md `project_sprint168_closed.md` (5 lessons) + index entry
- [x] `housekeeping_done: true`
