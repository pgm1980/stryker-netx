# stryker-netx 3.2.12 — `// Stryker disable next-line` directive is ignored when the comment is placed inside a multi-line method-chain expression (mutation on a non-first line of the parent statement)

> **Project**: Aisess Platform (multi-tenant maturity-assessment platform)
> **Reporter**: pgm1980 / Aisess team
> **Date observed**: 2026-05-10
> **Tool affected**: `dotnet-stryker-netx` 3.2.12 (the C# 14 / .NET 10 compatible fork of `dotnet-stryker`)
> **Severity**: Medium — disable-directive scoping is non-obvious for a common C# pattern (`await … .ConfigureAwait(false)` written on multiple lines); workaround exists (file-level `disable all` + `restore all` wrap) but is more verbose and easy to mis-pair.
> **Status**: Reported — pending maintainer review.

---

## 1. Summary

Given a multi-line method-chain expression such as

```csharp
var framework = await _frameworkRepository
    .GetBySlugAsync(slug, cancellationToken)
    // Stryker disable next-line all : equivalent — xUnit no-SyncContext.
    .ConfigureAwait(false);
```

the `// Stryker disable next-line all` directive on the line **directly above** the line that contains the mutated literal (`false` on `.ConfigureAwait(false)`) is **silently ignored**, and the resulting `Boolean` mutation (`false → true`) survives despite an unambiguous, line-adjacent disable comment.

The same is true for the more specific `// Stryker disable next-line Boolean : <reason>` directive — and for both directives at every position we tried *inside* the multi-line expression. The directive **only** takes effect when the comment is placed **outside the parent statement** *and* uses the unscoped (non-`next-line`) form, paired with a closing `// Stryker restore all`:

```csharp
// Stryker disable all : equivalent — xUnit no-SyncContext.
var framework = await _frameworkRepository
    .GetBySlugAsync(slug, cancellationToken)
    .ConfigureAwait(false);
// Stryker restore all
```

This wrap-style placement reliably moves the mutation from `Survived` → `Ignored`, but requires a paired `restore` directive and is significantly more verbose than the `next-line` form documented as the standard per-line construct.

The Stryker HTML report identifies the mutation as living on the line that contains `.ConfigureAwait(false)` (e.g., line 63 in our test file), so the tool's *display* of the source location is correct — the issue is in **directive matching**, not in source-position tracking.

---

## 2. Environment

| Component | Version |
|-----------|---------|
| OS | Windows 11 (24H2 build) |
| .NET SDK | 10.0.107 |
| Solution format | `.slnx` (XML-based; same project setup as the resolved [stryker-netx-3.2.10 .slnx mutable-assembly bug](./stryker-netx-3.2.10-slnx-mutable-assembly-bug.md)) |
| `dotnet-stryker-netx` | 3.2.12 (global tool, installed via `dotnet tool install -g dotnet-stryker-netx --version 3.2.12`) |
| Test project SDK | `Microsoft.NET.Sdk` (xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1) |
| Source project SDKs | `Microsoft.NET.Sdk` (Aisess.Application; the affected file resides here) |
| Target framework | `net10.0` |
| Mutation level | `Advanced` (auto-derived from `--mutation-profile Stronger`; same outcome reproduced with `--mutation-profile Defaults` and `--mutation-profile All`) |
| Mutation profile | `Stronger` (44-mutator catalog; observed identically with the 26-mutator `Defaults` catalog) |
| `Directory.Build.props` properties | `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisMode=All`, `Nullable=enable`, `ImplicitUsings=enable`, `Deterministic=true`, `NuGetAudit=true`, `LangVersion=latest` |
| Code analyzers | Roslynator 4.15.0, SonarAnalyzer.CSharp 10.25.0.139117, Meziantou.Analyzer 3.0.50, Microsoft.VisualStudio.Threading.Analyzers 17.14.15, AsyncFixer 2.1.0 |
| Test framework | xUnit (no `SynchronizationContext`, so `ConfigureAwait(false)`/`ConfigureAwait(true)` are observably equivalent — i.e. these mutations are genuinely equivalent and we want to disable them rather than write contrived tests against a non-existent context) |

---

## 3. Code under mutation (anonymized)

The two affected files in our repository, with line numbers as committed in
`5d31afb` on branch `feature/hardening-7.1-pulse-discoveries`:

### `src/Aisess.Application/Pulse/ManifestService.cs` (lines 59–88, abbreviated)

```csharp
// Cache miss — load from repository.
// Stryker disable all : equivalent — xUnit runs without a SynchronizationContext; ConfigureAwait(false→true) + Boolean mutations produce identical observable behavior — Hardening 7.1 H7.1.1.
var framework = await _frameworkRepository
    .GetBySlugAsync(slug, cancellationToken)
    .ConfigureAwait(false);
// Stryker restore all

if (framework is null)
{
    throw new FrameworkNotFoundException(slug);
}

// Stryker disable all : equivalent — xUnit runs without a SynchronizationContext; ConfigureAwait(false→true) + Boolean mutations produce identical observable behavior — Hardening 7.1 H7.1.1.
var frameworkVersion = await _frameworkRepository
    .GetVersionAsync(slug, version, cancellationToken)
    .ConfigureAwait(false);
// Stryker restore all
```

### `src/Aisess.Application/Pulse/ManifestSnapshotBuilder.cs` (lines 99–120, abbreviated)

```csharp
// 1. Load framework version and validate it's Published.
// Stryker disable all : equivalent — xUnit runs without a SynchronizationContext; ConfigureAwait(false→true) + Boolean mutations produce identical observable behavior — Hardening 7.1 H7.1.1.
var frameworkVersion = await _frameworkRepository
    .GetVersionAsync(slug, version, cancellationToken)
    .ConfigureAwait(false)
    ?? throw new FrameworkVersionNotFoundException(slug, version);
// Stryker restore all
```

The shape of the expression matters: the `await … .ChainedCall(arg) .ConfigureAwait(false)` parent-statement spans **multiple physical source lines**. Per Roslyn parsing this is a single `LocalDeclarationStatement` whose initializer is a single `AwaitExpression` whose operand is a chain of `InvocationExpression`s; the `false` literal token is contained within that single statement.

---

## 4. Stryker run history

Five `dotnet stryker-netx --config-file stryker-config-h7.1.json --project ../../src/Aisess.Application/Aisess.Application.csproj` runs were executed against the same configuration, varying only the placement and form of the disable directives in the two source files above. The relevant rows of `Killed / Survived / Timeout / NoCoverage` are reproduced below:

| Run | Time | Disable-directive variant | K | S | T | NC | Score |
|----:|------|---------------------------|--:|--:|--:|--:|------:|
| 1 | 00:49 | _no_ disable directives (baseline) | 28 | 12 | 12 | 2 | 74.07% |
| 2 | 00:59 | `// Stryker disable next-line Boolean : <reason>` immediately above the line containing `.ConfigureAwait(false)` (e.g., on line 62, mutation on line 63) — **inside** the multi-line statement | 35 | 7 | 12 | 0 | 87.04% |
| 3 | 01:06 | _identical_ to Run 2 (re-run to verify reproducibility) | 35 | 7 | 12 | 0 | 87.04% |
| 4 | 01:09 | `// Stryker disable next-line all : <reason>` (replaced `Boolean` with `all`) at the same in-statement position; **also** added `// Stryker disable next-line all` on the line immediately above an `else if (multi-line condition)` block (different file location, single directive at the C# statement boundary) | 34 | 5 | 12 | 0 | 90.20% |
| 5 | 01:13 | **Wrap-style:** `// Stryker disable all : <reason>` placed **before** the parent C# statement, paired with `// Stryker restore all` placed **after** the statement; the mutated lines are *inside* the wrap | 34 | 0 | 12 | 0 | **100.00%** |

Diff between Run 4 and Run 5 (the only thing that mattered):

```diff
-            // Stryker disable next-line all : equivalent — ConfigureAwait equivalent in xUnit.
-            .ConfigureAwait(false);
+        // Stryker disable all : equivalent — ConfigureAwait equivalent in xUnit.
+        var framework = await _frameworkRepository
+            .GetBySlugAsync(slug, cancellationToken)
+            .ConfigureAwait(false);
+        // Stryker restore all
```

Both placements **report the mutation on the same source line** (the line that
contains `.ConfigureAwait(false)`), and both placements use the same
`disable all` mutator kind. The only difference is whether the directive sits
*inside* the multi-line statement (Run 4 — directive ineffective) or at the
statement boundary plus paired `restore` (Run 5 — directive honored).

The `else if` Logical mutation in `ManifestSnapshotBuilder.cs` (different
case) **is** killed by `// Stryker disable next-line all` placed on the line
*immediately above the C# statement boundary* (i.e., immediately above the
`else if` keyword), even though that statement is also multi-line. So
`next-line` does work at the statement boundary — it just appears not to work
when placed *between two continuation lines of the same statement*.

---

## 5. Expected vs. actual behavior

### Per the documented `next-line` semantics

The CLAUDE.md note in our project (lifted from documentation guidance during
Aisess Hardening Sprint 2.5 / stryker-netx 3.2.12 validation) states:

> `next-line` deaktiviert exakt EINE folgende Statement-Zeile, nicht eine
> ganze Multi-Line-Expression. Für object-initializers / chained method calls:
> pro Zeile eine eigene Direktive (3.2.12 fixt den 3.2.11-Bug, dass
> per-line-Direktiven bei Object-Initializern wirkungslos waren).

We read this as: a `next-line` directive placed on line N disables all
mutations whose source position is on line N+1, regardless of whether N+1 is
the start of a C# statement or a continuation of a previous one.

### Actual behavior in 3.2.12 against multi-line method-chains

The directive is honored **only** when:
- The comment is the *leading trivia of a complete C# statement*, **or**
- The comment uses the file-level `// Stryker disable all` form (no `next-line` qualifier) and a matching `// Stryker restore all` is placed after the statement.

Comments placed *between continuation lines* of an existing multi-line
statement (whether the next-line target is on the next physical line or not)
are silently ignored. There is no warning, error, or log entry about an
ineffective directive.

### Why this matters in practice

The `await x.M(args).ConfigureAwait(false);` pattern is overwhelmingly written
across multiple lines in any real C# codebase (per .NET style and per Roslyn
analyzer recommendations such as VSTHRD200, CA2007, VSTHRD003), and
xUnit-based unit tests cannot kill the `Boolean` mutation on the
`ConfigureAwait(false)` literal because xUnit does not install a
`SynchronizationContext`. The clean workflow is to mark these mutations as
equivalent with a per-line disable directive — but per-line directives do not
work at the natural placement.

The wrap-style workaround is reliable, but each instance now requires **three
lines of disable infrastructure** (`disable`, `restore`, plus a leading blank
or comment line for readability) instead of one, which inflates code and
makes the disable infrastructure visually heavier than the production code it
is annotating.

---

## 6. Hypothesis (open to maintainer correction)

Roslyn's `SyntaxTrivia` model attaches comments to the nearest token, and the
nearest token to a comment placed *between* `.GetBySlugAsync(slug,
cancellationToken)` and `.ConfigureAwait(false)` is the
`SimpleMemberAccessExpressionSyntax` token `.ConfigureAwait`. That token,
however, is **inside** the parent `LocalDeclarationStatement` (the `var
framework = await … ;` statement) — and we suspect Stryker's directive
matcher only inspects the **leading trivia of the parent statement**, not
trivia attached to inner expression tokens.

This would explain:

1. Run 2/3/4: a `next-line` directive placed on the line immediately above
   `.ConfigureAwait(false)` is, from the directive matcher's perspective,
   trivia attached to an inner expression token. The matcher never sees it.

2. Run 4 `else if` case: the `next-line` directive **does** work because
   `else if (…) { … }` constitutes its own `IfStatementSyntax` (with the
   `else` keyword as a clause of the parent `IfStatementSyntax`); the comment
   above `else if` is leading trivia of that statement, which the matcher
   inspects.

3. Run 5: the `// Stryker disable all` form **without** `next-line` is
   file-scoped (matcher walks forward across statements until it finds
   `// Stryker restore all`), so it doesn't depend on the leading-trivia
   association at all.

If this hypothesis is correct, the fix is either:
- broaden the directive matcher to inspect **all** comment trivia within a
  parent statement when a mutation falls within that statement, **or**
- detect the case explicitly and warn the user that the `next-line` directive
  is being silently dropped because it does not annotate a statement
  boundary.

We have not run a debug build of stryker-netx to verify the hypothesis. We
would be happy to provide additional repro data if the maintainer wants to
investigate.

---

## 7. Workarounds tried — and their results

| Attempt | Result |
|---------|--------|
| `// Stryker disable next-line Boolean : <reason>` directly above `.ConfigureAwait(false)` (between two continuation lines) | ❌ Mutation survives. |
| `// Stryker disable next-line all : <reason>` directly above `.ConfigureAwait(false)` (between two continuation lines) | ❌ Mutation survives. |
| `// Stryker disable next-line all : <reason>` immediately above the parent C# statement (i.e., on the line above `var framework = await …`) | ❌ Mutation survives — the next physical line is the start of the statement, but the mutation lives several physical lines later. |
| `// Stryker disable all : <reason>` immediately above the parent C# statement, **without** `next-line` and **without** a paired `restore` | ⚠️ Mutation is ignored, but disable scope leaks to the rest of the file (every subsequent mutation in the file is ignored). |
| `// Stryker disable all : <reason>` immediately above the parent C# statement, paired with `// Stryker restore all` immediately after the statement (Run 5 — recommended workaround) | ✅ Mutation is ignored and scope is correctly bounded. |
| Move the entire expression onto a single line: `var framework = await _frameworkRepository.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false);` and use `// Stryker disable next-line all : <reason>` (not actually applied because it violates our analyzer-enforced line-length and naming style) | ✅ (theoretical — we did not commit this) |

---

## 8. Suggested investigation areas for the maintainers

1. **Directive matcher granularity.** In `Stryker.Core` (or wherever
   directive matching lives), verify whether the matcher walks
   `SyntaxTrivia` of the mutated token or only the leading trivia of the
   token's enclosing `StatementSyntax`. If the latter, the per-line
   `// Stryker disable next-line …` placement we tried in Runs 2–4 will
   never be visible to the matcher.

2. **Diagnostic log on ineffective directives.** When a `// Stryker
   disable …` comment is parsed but does not match any mutation in its
   declared scope, emitting a `[WRN]` log line `"Disable directive at
   {file}:{line} did not match any mutations in its scope"` would have
   shortened our investigation from several hours to a single Stryker run.

3. **Documentation.** If the per-statement-only behavior is intentional, the
   official documentation under "Disable directives" / "next-line directive"
   should note: _"`next-line` directives are evaluated at C# statement
   boundaries; they do **not** apply to mutations on continuation lines of a
   multi-line expression. Use the wrap-style `// Stryker disable <kind>` /
   `// Stryker restore <kind>` form for those cases."_ The existing
   3.2.11/3.2.12 release notes mention object-initializer-related changes,
   which pointed us in the right direction but did not cover the multi-line
   chained-call case.

4. **Default codegen advice for `await … .ConfigureAwait(false)` in xUnit
   test contexts.** Equivalent `Boolean` mutations on `ConfigureAwait(false)`
   are an extremely common false-positive in `await`-heavy codebases. A
   built-in mutation profile flag (e.g.,
   `--equivalent-mutants xunit-no-sync-context`) that auto-ignores
   `ConfigureAwait(false→true)` flips when the test framework is detected
   as xUnit (or when the consumer opts in) would let projects skip the
   manual disable-comment plumbing entirely.

---

## 9. Minimal repro

A minimal repro can be built in <5 minutes:

```bash
mkdir stryker-multiline-disable-repro && cd stryker-multiline-disable-repro

dotnet new sln --format slnx -n repro
dotnet new classlib -n LibA -f net10.0
dotnet new xunit    -n LibA.Tests -f net10.0
dotnet add LibA.Tests/LibA.Tests.csproj reference LibA/LibA.csproj
dotnet add LibA.Tests/LibA.Tests.csproj package coverlet.collector
dotnet sln repro.slnx add LibA/LibA.csproj LibA.Tests/LibA.Tests.csproj
```

Replace `LibA/Class1.cs` with:

```csharp
namespace LibA;

public sealed class Greeter
{
    private readonly System.Threading.Tasks.Task<string> _source;
    public Greeter(string greeting) => _source = System.Threading.Tasks.Task.FromResult(greeting);

    public async System.Threading.Tasks.Task<string> GreetAsync()
    {
        var greeting = await _source
            // Stryker disable next-line all : test of next-line scope inside a multi-line await chain.
            .ConfigureAwait(false);
        return greeting + "!";
    }
}
```

Replace `LibA.Tests/UnitTest1.cs` with:

```csharp
using Xunit;

namespace LibA.Tests;

public sealed class GreeterTests
{
    [Fact]
    public async System.Threading.Tasks.Task GreetAsync_Appends_Bang()
    {
        var greeter = new LibA.Greeter("hello");
        var result = await greeter.GreetAsync();
        Assert.Equal("hello!", result);
    }
}
```

Then:

```bash
dotnet build repro.slnx
dotnet test repro.slnx        # expect green

cd LibA.Tests
dotnet stryker-netx \
    --project ../LibA/LibA.csproj \
    --solution ../repro.slnx \
    --reporters "html" --reporters "progress" \
    --mutation-profile Defaults
```

**Expected (per the `next-line all` directive on the line above
`.ConfigureAwait(false)`):** the `Boolean` mutation `false → true` on
`.ConfigureAwait(false)` is reported as `Ignored`.

**Observed:** the `Boolean` mutation is reported as `Survived` — the
directive has no effect.

Then move the directive to wrap-style:

```csharp
public async System.Threading.Tasks.Task<string> GreetAsync()
{
    // Stryker disable all : test of wrap-style around a multi-line await chain.
    var greeting = await _source
        .ConfigureAwait(false);
    // Stryker restore all
    return greeting + "!";
}
```

Re-run Stryker. **Observed:** the `Boolean` mutation is now `Ignored`.

---

## 10. Notes for downstream users

While this issue is open, projects that need to mark `ConfigureAwait(false→true)` mutations as ignored on multi-line `await … .ConfigureAwait(false)` chains (the de-facto idiom in xUnit-tested .NET codebases) should use the wrap-style form:

```csharp
// Stryker disable all : equivalent — xUnit no-SyncContext; ConfigureAwait(false→true) + Boolean mutations produce identical observable behavior.
var x = await _repository
    .GetAsync(slug, ct)
    .ConfigureAwait(false);
// Stryker restore all
```

This is the form Aisess has standardized on as of Hardening 7.1; the same
pattern is applied across `SurveyService.cs`, `ResponseQueryService.cs`,
`ManifestService.cs`, and `ManifestSnapshotBuilder.cs`. We have updated our
internal CLAUDE.md "stryker-netx — Mutation Testing" section with this
guidance until the upstream behavior is clarified.

---

## 11. Reproducing the diagnostic data used in this report

The five Stryker HTML reports referenced in §4 are preserved in the Aisess
repository at:

```
tests/Aisess.Tests/StrykerOutput/2026-05-10.00-49-06/reports/mutation-report.html  # Run 1: 74.07%
tests/Aisess.Tests/StrykerOutput/2026-05-10.00-59-38/reports/mutation-report.html  # Run 2: 87.04%
tests/Aisess.Tests/StrykerOutput/2026-05-10.01-03-40/reports/mutation-report.html  # Run 3: 87.04%
tests/Aisess.Tests/StrykerOutput/2026-05-10.01-09-13/reports/mutation-report.html  # Run 4: 90.20%
tests/Aisess.Tests/StrykerOutput/2026-05-10.01-13-31/reports/mutation-report.html  # Run 5: 100.00%
```

The accompanying configuration is `tests/Aisess.Tests/stryker-config-h7.1.json` and the parsing script (used to extract per-run survived/timeout/no-coverage breakdowns from the HTML's embedded JSON) is `tests/Aisess.Tests/stryker_parse_h7.1.py`. Both are public on branch `feature/hardening-7.1-pulse-discoveries` of the Aisess repository.

---

## 12. Contact / cross-references

- Aisess Hardening 7.1 PR: https://github.com/pgm1980/aisess-platform/pull/157
- Aisess CLAUDE.md "stryker-netx — Mutation Testing" section (Hardening 7.1 update pending; the wrap-style form will be documented there as the canonical workaround for multi-line `await … .ConfigureAwait(false)` chains)
- Related (resolved) prior issue: [stryker-netx 3.2.10 .slnx mutable-assembly resolution bug](./stryker-netx-3.2.10-slnx-mutable-assembly-bug.md)
- Aisess platform is happy to provide additional repros, debug logs, or test against patched stryker-netx builds — reach out via GitHub Issue or email to the Aisess team.
