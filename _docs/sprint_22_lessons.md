# Sprint 22 — `--mutation-profile` CLI/JSON wiring + profile-comparison E2E: Lessons Learned

**Sprint:** 22 (2026-05-01, autonomous run)
**Branch:** `feature/22-mutation-profile-cli`
**Base:** v2.8.0 (Sprint 21 closed)
**Final Tag:** `v2.9.0`
**Type:** Production-code plumbing (5 files) + 3 new E2E tests. Test-infrastructure delta only — no new mutator or filter.

## Sprint Outcome

| Metric | Result |
|--------|--------|
| `--mutation-profile` CLI flag | ✅ Registered in `CommandLineConfigReader.PrepareCliOptions` under `InputCategory.Mutation` |
| `mutation-profile` JSON config key | ✅ Wired in `FileBasedInput` + `FileConfigReader.ApplyMutationInputs` + `FileConfigGenerator` (init template) |
| `MutationProfileInput` exposed on `IStrykerInputs` | ✅ Interface gap closed (was only on concrete `StrykerInputs`) |
| Per-profile cache + 3 new E2E `[Fact]`s | ✅ `StrykerRunCacheFixture.GetDefaultsRunAtAdvancedLevel/GetStrongerRunAtAdvancedLevel/GetAllRunAtAdvancedLevel` |
| Solution-wide tests | ✅ 386 (Core) + 10 (Architecture) + 13 (E2E) + 17 (Sample) = **426 green** |
| Sample E2E (Defaults @ Standard) | ✅ 100 % mutation score (5/5 mutants killed) — backwards-compat preserved |
| Tag | `v2.9.0` |

## What landed

### Production code (5 files)

1. **`src/Stryker.Configuration/Options/IStrykerInputs.cs`** — added `MutationProfileInput MutationProfileInput { get; init; }` to the interface (was missing — only on concrete `StrykerInputs`).
2. **`src/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs`** — `AddCliInput(inputs.MutationProfileInput, "mutation-profile", null, category: InputCategory.Mutation)` next to the existing `--mutation-level`. No short option (`-l` / `-m` already taken in the Mutation category).
3. **`src/Stryker.CLI/FileBasedInput.cs`** — `[JsonPropertyName("mutation-profile")] public string? MutationProfile { get; init; }` mirroring `MutationLevel`.
4. **`src/Stryker.CLI/FileConfigReader.cs`** — extracted **`ApplyMutationInputs`** helper to host the 5 mutation-related JSON-to-input mappings (`Mutate`, `MutationLevel`, `MutationProfile`, `IgnoreMutations`, `IgnoreMethods`). Adding the `MutationProfile` line tipped `ApplyTopLevelInputs` over MA0051's 40-statement limit; same lesson as Sprint 13.
5. **`src/Stryker.CLI/FileConfigGenerator.cs`** — `MutationProfile = inputs.MutationProfileInput.SuppliedInput ?? inputs.MutationProfileInput.Default,` so `stryker init` documents the new key in generated `stryker-config.json` files.

### E2E tests (2 files touched)

- **`tests/Stryker.E2E.Tests/Infrastructure/StrykerRunCacheFixture.cs`** — three new cached-run accessors at `--mutation-level Advanced`. Sprint 21's deferred D5 design note replaced with the Sprint 22 status.
- **`tests/Stryker.E2E.Tests/SampleE2EProfileTests.cs`** — three new `[Fact]`s:
  - `All_TotalIsStrictlyGreaterThan_Defaults` — All produces 13, Defaults produces 5 on Calculator.cs at Level=Advanced.
  - `Stronger_TotalIsAtLeast_Defaults` — Stronger ⊇ Defaults by `[MutationProfileMembership]` flag construction.
  - `All_FileMap_ContainsEveryDefaultsFile` — file-map subset relation.

## Process lessons

### 1. **MutationLevel × MutationProfile orthogonality is non-obvious — caught during smoke-test, not planning**

The original task spec said "`--mutation-profile All` should produce more mutants than `--mutation-profile Defaults`." That **only holds at Level ≥ Advanced** on Sample.Library. Almost every Stronger-only / All-only mutator added in Sprints 9–14 has `MutationLevel.Advanced` or `MutationLevel.Complete`. Stryker's two filters compose conjunctively: a mutator must pass *both* the level test and the profile test. So at the default `Level=Standard`, all three profiles produce the same 5 mutants on Calculator.cs — the comparison is vacuous, the test would have been flaky.

**Fix:** all three new cached runs pass `--mutation-level Advanced` explicitly. The fixture doc-string spells out the reason.

### 2. **`--mutation-level Complete` + `--mutation-profile All` triggers a pre-existing crash**

```
System.InvalidCastException: Unable to cast object of type
'Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax' to type
'Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax'.
   at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter.VisitQualifiedName(QualifiedNameSyntax node)
```

A `Complete`-level mutator (UoiMutator / NakedReceiverMutator / GenericConstraintMutator / ExceptionSwapMutator / etc.) is producing a malformed `QualifiedNameSyntax` whose right-hand operand is a `ParenthesizedExpressionSyntax` instead of a `NameSyntax`. Roslyn's rewriter then can't visit it. **This is unrelated to Sprint 22's CLI plumbing** — it's a latent bug in one of the v2.x operators that has been dormant because no E2E ever ran the All-profile + Complete-level combination. Out of scope for this sprint; roadmapped as a follow-up.

### 3. **The McMaster Option registration block lives in `CommandLineConfigReader.PrepareCliOptions`, not in `StrykerCli.cs`**

The task instruction pointed at `src/Stryker.CLI/StrykerCli.cs`, which is the entry-point class — but it does *not* register CLI options; it just builds the McMaster `CommandLineApplication` and delegates to `CommandLineConfigReader.RegisterCommandLineOptions`. All `AddCliInput(...)` calls live in `CommandLineConfigReader.PrepareCliOptions` (one block grouped by `InputCategory`). The instruction's intent was right (CLI-surface project), the file pointer was directional. Always grep for the actual pattern (`AddCliInput(inputs.MutationLevelInput`) before reaching for the named file.

### 4. **MA0051 trips again — same recipe as Sprint 13**

`ApplyTopLevelInputs` was at 40 statements (Meziantou's `MA0051` hard ceiling). Adding the `MutationProfile` JSON-mapping line bumped it to 41 → build error. The fix is the canonical one from Sprint 13's lessons: extract a focused helper (`ApplyMutationInputs`) for the related cluster (Mutate / MutationLevel / MutationProfile / IgnoreMutations / IgnoreMethods). Net result: `ApplyTopLevelInputs` drops to 31 statements, new `ApplyMutationInputs` is 5 — both well under the limit and arguably more readable than the original undifferentiated block.

### 5. **`MutationProfileInput` was missing from `IStrykerInputs` — interface gap caught while mapping the wiring**

Sprint 6 added `MutationProfileInput` as a property on the concrete `StrykerInputs` class but **omitted it from the `IStrykerInputs` interface**. This worked for in-process callers that already had the concrete type, but it would have prevented mock-based unit tests that rely on the interface from setting a custom profile. Sprint 22 closes the gap — the interface now has the property and `FileConfigReader` can plumb through it. The lesson: when adding a property to a configuration POCO, verify both the interface AND the implementation; the compiler doesn't enforce it the way it does for instance methods (init-only properties on an interface are an opt-in surface).

### 6. **Smoke-test sequence: `None` first, then `All`, then matrix**

To verify CLI plumbing works end-to-end, the cleanest probe is `--mutation-profile None` — if your wiring works, you get `0 mutants created`; if it doesn't, you still get the default 5. This unambiguously distinguishes "flag not bound" from "flag bound but filter-orthogonal". I almost gave up on the wiring after seeing All == Defaults at Standard level; the `None` probe rescued the diagnosis in one command. Always include a "must-zero" sentinel value in the smoke matrix.

## v2.9.0 progress map

```
[done]    Sprint 22 → CLI/JSON wiring for --mutation-profile + 3 profile-comparison E2E → v2.9.0   ⭐ MINOR ⭐
```

## Out of scope (deferred)

- **`Complete`-level + `All`-profile crash** — pre-existing `VisitQualifiedName` `InvalidCastException` in one of the v2.x operators. Needs a focused debugging sprint to bisect across the candidate Complete-level mutators (UoiMutator, NakedReceiverMutator, GenericConstraintMutator, ExceptionSwapMutator, MethodBodyReplacementMutator, AsSpanAsMemoryMutator, ArgumentPropagationMutator, ConstructorNullMutator, RorMatrixMutator, SpanReadOnlySpanDeclarationMutator) and identify which one emits the malformed `QualifiedNameSyntax`. Tracked in `_docs/sprint_22_lessons.md` § 2.
- **Profile×Level matrix expansion in E2E** — only `Level=Advanced` is exercised. Adding `Level=Complete` runs is blocked on the bug above.
- **`stryker init` integration test** — the FileConfigGenerator change is exercised manually but not asserted via an E2E run of the `init` subcommand.
- **CI workflow update** — Sprint 21 already deferred this; remains deferred. Tests pass via `dotnet test` locally and on any developer machine; integrating `tests/Stryker.E2E.Tests/` into `.github/workflows/*.yml` is its own scoped task.

## Recommended next sprint candidate

**Sprint 23: `Complete`-level + `All`-profile crash bisect & fix**
- Add a tracing log to `CsharpMutantOrchestrator.GenerateMutationsForNode` that records the mutator class name + offending `OriginalNode.Kind()` immediately before the rewriter is invoked.
- Run with `--mutation-level Complete --mutation-profile All` against Sample.slnx and capture the last-mutator-name before the `InvalidCastException`.
- Triage which mutator emits a `QualifiedNameSyntax` with a `ParenthesizedExpressionSyntax` right-hand operand (must be `SimpleNameSyntax` per Roslyn contract).
- Fix the root cause; add a unit test in `tests/Stryker.Core.Tests/` that constructs a `QualifiedName` mutation and asserts the rewriter doesn't throw.
- Re-enable a `Profile=All + Level=Complete` cached fixture in the E2E suite and add a Fact `All_AtCompleteLevel_DoesNotCrash`.
