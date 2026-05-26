# BUG REPORT #9 FOLLOWUP_2 — filesystem-mcp-server v3.3.0 validation (Sprint 169 trigger)

**Status:** Sprint 169 trigger; verbatim extract of the message delivered 2026-05-25 by the filesystem-mcp-server team after their v3.3.0 validation run.
**Reporter:** filesystem-mcp-server, repo https://github.com/pgm1980/filesystem-mcp-server, branch `feature/optimization-roadmap-baseline`, tip commit `a28d08b`.
**Their validation document:** `BUG_REPORT_9_FOLLOWUP_2.md` in the reporter's repo (not mirrored here — only the extract below is committed for our regression-test corpus).

---

## Subject

> Sprint 169 — Single high-impact fix that closes 94% of remaining Safe Mode! warnings

## Headline numbers (v3.3.0 baseline on a 21,091-mutant Infrastructure run)

| Count | Category | Disposition |
|---|---|---|
| 184 | **A.1: nested-ternary `^(IsActive(?-1:?0:?2:1)` still emits double** | 🔴 ADR-047 partial — Sprint 169 fix target |
| 3 | A.2: direct `double → long` (long-typed positional/ref params) | ADR-047 mostly fixed; 3 residual sites |
| 1 | A.3: `ref long ← ref double` | ADR-047 mostly fixed |
| 3 | B.1: `byte[].AsSpan` overload collision with `MemoryExtensions.AsSpan(string?, ...)` | Honest-deferred 169 |
| 1 | B.2: `byte[].AsMemory` overload mismatch | Honest-deferred 169 |
| 4 | D: `PluginManager` codegen edge-case (CS0165/CS0161 after try-block removal) | Separate codegen issue (probably block-removal mutator) |
| **196** | **Total** | |

Pre-v3.3.0 baseline was ~70 Safe Mode warnings (the original Bug Report #9 §6.1 cluster table). v3.3.0 closed the LEAF-VALUE class but the WRAPPING ternary-expression class is still emitting double.

CompileError rate: 31.14 % (vs 39 % pre-v3.3.0, vs single-digit % target).

## Reporter's exemplar of the failing pattern

```text
[20:37:07 WRN] An unidentified mutation in ...SymlinkManagementService.cs
  resulted in a compile error (at 95:31) with id: CS0029,
  message: Cannot implicitly convert type 'double' to 'int'
  (Source code:
    ^(StrykerP3SCezYw0EYR4bJ.MutantControl.IsActive(17232)?-1:
      (StrykerP3SCezYw0EYR4bJ.MutantControl.IsActive(17231)?0:
        (StrykerP3SCezYw0EYR4bJ.MutantControl.IsActive(17230)?2:1))))
```

The four leaf literals (-1, 0, 2, 1) are each individually typed-correctly post-ADR-047, but the resulting ternary expression is inferred as `double` somewhere in the wrap path.

Other affected sites (consistent pattern: inline-constant inside an int/long/ref-int/ref-long lvalue or argument slot):

```
DiffService.cs:211:16    Cannot implicitly convert 'double' to 'int' (Source code: start)
DiffService.cs:211:23    Cannot implicitly convert 'double' to 'int' (Source code: end)
EditService.cs:273:42    Argument 2: cannot convert from 'double' to 'int' (Source code: start)
DiffService.cs:202:25    Argument 1: cannot convert from 'double' to 'int' (Source code: j)
ContentOpsService.cs:78:26   Cannot implicitly convert 'double' to 'int' (Source code: insertIndex)
```

## Reporter's suggested fix shape (educated guess)

```csharp
// pseudo-code — current:
return SyntaxFactory.ConditionalExpression(
    isActiveCheck,
    leafValueA,
    SyntaxFactory.ConditionalExpression(isActiveCheck2, leafValueB, ...));

// proposed: target-type-aware cast on the outer ternary, mirroring ADR-047 leaf dispatch
var ternary = SyntaxFactory.ConditionalExpression(...);
var targetType = _semanticModel.GetTypeInfo(originalNode).ConvertedType;
return targetType switch
{
    { SpecialType: SpecialType.System_Int32 } => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("int"), ternary),
    { SpecialType: SpecialType.System_Int64 } => SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("long"), ternary),
    ...
    _ => ternary
};
```

## Reporter's validation procedure for the Sprint-169 candidate build

```bash
dotnet stryker-netx \
    --project FsMcpServer.Infrastructure.csproj \
    --mutation-profile Stronger \
    --output _stryker_runs/v3.3.x-sprint169-validation \
    -r json
```

**Pass criteria the reporter would accept:**
- Safe Mode! warning count < 20 (vs current 196)
- CompileError rate < 15 % (vs current 31.14 %)
- Sub-categories A.2/A.3 residual sites also closed (low-hanging follow-up)

## Reporter's repo-side artefacts (available on request)

Available in the reporter's repo branch `feature/optimization-roadmap-baseline` (tip `a28d08b`):

- `_safe_mode_categories.txt` — exact 196-warning categorisation
- `_stryker_runs/v3.3.0-validation/reports/mutation-report.json` — full v3.3.0 mutation report
- `_stryker_330_full_validation.log` — verbatim Safe Mode warnings with source-code snippets
- 18 affected files for the A.1 cluster: DiffService, EditService, ContentOpsService, SymlinkManagementService, CodeMetricsService, MetadataService, BinaryDiffService, …
- Offer: 5-file minimum repro project on request

## Closing note from reporter

> Sprint 169 is a single-fix high-leverage opportunity. We'll watch the release notes and re-validate as soon as a pre-release drop is available. If you can ship the ternary fix even without addressing B.1/B.2, that alone closes our pain point for incremental TDD on this codebase.
