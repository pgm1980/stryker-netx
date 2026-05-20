# stryker-netx — Anomalies & Bugs Report

> **Tool**: [`dotnet-stryker-netx`](https://www.nuget.org/packages/dotnet-stryker-netx) — the C# 14 / .NET 10 compatible fork of Stryker.NET. The upstream `stryker-mutator/stryker-net` repository is **not** the destination for these reports: it is the *original* Stryker.NET, which is **not** .NET-10-compatible, so any bug discovered against `dotnet-stryker-netx` belongs to the **fork maintainers**, not the upstream project. Forwarding-destination is to be chosen by the Aisess team.
> **Reporter project**: Aisess Platform (multi-tenant maturity-assessment platform, ~3 600 backend tests, 4-layer DDD-Onion + `.slnx` solution)
> **Reporter**: pgm1980 / Aisess team — for forwarding to stryker-netx maintainers
> **Compiled**: 2026-05-19 (covers field-observations across Aisess sprints v1.0.1 → v1.45.0; tool versions 3.2.10 → 3.2.13)
> **Format**: Each item is structured as **Symptom → Reproducer → Environment → Workaround → Severity → Status**.

## Table of contents

- [§0 Environment baseline (applies to every item unless overridden)](#0-environment-baseline-applies-to-every-item-unless-overridden)
- [§1 `.slnx` solution: mutable-assembly resolution returns 0 projects (3.2.10) — **RESOLVED in 3.2.11**](#1-slnx-solution-mutable-assembly-resolution-returns-0-projects-3210--resolved-in-3211)
- [§2 `--solution <path>.slnx` mode hangs silently for 50+ minutes without log output (3.2.12) — **OPEN**](#2---solution-pathslnx-mode-hangs-silently-for-50-minutes-without-log-output-3212--open)
- [§3 `--project <short-name>` rejected with multi-reference error even when explicitly supplied (3.2.13) — **OPEN — UX**](#3---project-short-name-rejected-with-multi-reference-error-even-when-explicitly-supplied-3213--open--ux)
- [§4 Initial test-run ignores `Category!=Integration` trait-filter — discovers 186 extra Integration tests that need Docker containers (3.2.13) — **OPEN**](#4-initial-test-run-ignores-categoryintegration-trait-filter--discovers-186-extra-integration-tests-that-need-docker-containers-3213--open)
- [§5 `// Stryker disable next-line` ignored inside multi-line method-chain expressions (3.2.12) — **OPEN — see dedicated issue file**](#5--stryker-disable-next-line-ignored-inside-multi-line-method-chain-expressions-3212--open--see-dedicated-issue-file)
- [§6 `// Stryker disable next-line all,Boolean` parses `all` as a literal mutator-kind name → error-log on every run (3.2.12) — **OPEN — DX**](#6--stryker-disable-next-line-allboolean-parses-all-as-a-literal-mutator-kind-name--error-log-on-every-run-3212--open--dx)
- [§7 `ConfigureAwait` is not a valid mutator-kind despite being a frequently-disabled construct (3.2.12) — **OPEN — DX/Docs**](#7-configureawait-is-not-a-valid-mutator-kind-despite-being-a-frequently-disabled-construct-3212--open--dxdocs)
- [§8 Disable-comments are parsed in *every* source file in the solution, even outside the `--mutate` filter set (3.2.12) — **OPEN — DX/Perf**](#8-disable-comments-are-parsed-in-every-source-file-in-the-solution-even-outside-the---mutate-filter-set-3212--open--dxperf)
- [§9 `// Stryker disable next-line` is wirkungslos on per-line directives within object-initializers and chained method calls (3.2.11) — **PARTIALLY RESOLVED in 3.2.12**](#9--stryker-disable-next-line-is-wirkungslos-on-per-line-directives-within-object-initializers-and-chained-method-calls-3211--partially-resolved-in-3212)
- [§10 Wishlist / feature-requests](#10-wishlist--feature-requests)
- [§11 Cross-references to dedicated issue files](#11-cross-references-to-dedicated-issue-files)

---

## §0 Environment baseline (applies to every item unless overridden)

| Component | Version |
|---|---|
| OS | Windows 11 (build 24H2) |
| .NET SDK | 10.0.107 |
| Solution format | `.slnx` (XML-based; **no** legacy `.sln` alongside) |
| Target framework | `net10.0` |
| Solution layout | 4-layer DDD-Onion: `src/Aisess.Domain`, `src/Aisess.Application`, `src/Aisess.Infrastructure`, `src/Aisess.Api` (`Microsoft.NET.Sdk.Web`) + 1 test project `tests/Aisess.Tests` (xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1) + 1 benchmark project (BenchmarkDotNet 0.15.8) + 1 CLI tool `tools/Aisess.Cli` |
| Test framework | xUnit 2.9.3 + AwesomeAssertions (Apache-2.0 fork of FluentAssertions 7.x) + Moq 4.20+ + FsCheck.Xunit 3.3.2 + ArchUnitNET 0.13.3 + Coverlet 8.0.0 |
| `Directory.Build.props` | `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisMode=All`, `Nullable=enable`, `ImplicitUsings=enable`, `Deterministic=true`, `NuGetAudit=true`, `LangVersion=latest` |
| Code analyzers | Roslynator 4.15.0, SonarAnalyzer.CSharp 10.25.0.139117, Meziantou.Analyzer 3.0.50, Microsoft.VisualStudio.Threading.Analyzers 17.14.15, AsyncFixer 2.1.0 |
| Test count | ≈ 3 654 unit + ≈ 186 integration (Testcontainers-gated; trait `[Trait("Category", "Integration")]`) |
| `dotnet test` filter used | `--filter "Category!=Integration"` (excludes container-dependent tests; reports 3 654 tests, 3 654 passing) |
| `dotnet-stryker-netx` install | `dotnet tool install -g dotnet-stryker-netx --version 3.2.13` |

---

## §1 `.slnx` solution: mutable-assembly resolution returns 0 projects (3.2.10) — **RESOLVED in 3.2.11**

### Symptom

Mutation testing cannot start against a `.slnx`-based solution. The project-analysis phase **successfully discovers and parses every project in the dependency graph** (test project + 4 source projects + 1 CLI) yet ultimately reports:

```
[INF] Could not find an assembly reference to a mutable assembly for project
      "…\tests\Aisess.Tests\Aisess.Tests.csproj". Will look into project
      references.
[DBG] Analyzing 0 projects.
[WRN] No project found, check settings and ensure project file is not corrupted.
```

and exits with `Failed to analyze project builds. Stryker cannot continue.`

### Status

✅ **RESOLVED in stryker-netx 3.2.11** (released 2026-05-07). H2 root-cause (over-aggressive `--project` substring filter mistakenly matching the test project) confirmed by the maintainer's diagnostic patch run; the fix loosens the filter and emits a clearer error message when the `"project"` config field accidentally points at the test project.

Full reproduction + maintainer-diagnostic-log + fix-verification is in [`_docs/issues/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md`](_docs/issues/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md).

---

## §2 `--solution <path>.slnx` mode hangs silently for 50+ minutes without log output (3.2.12) — **OPEN**

### Symptom

When running stryker-netx 3.2.12 in solution-wide mode against a multi-project `.slnx` solution, the tool **starts up cleanly, prints the analysis log lines, then enters a state where no further output is written to either stdout, stderr, or the `StrykerOutput/<timestamp>/logs/` directory**. The `dotnet-stryker-netx.exe` process remains alive (visible in `tasklist`), but produces no observable progress signal for at least 50 minutes — at which point we forcibly terminated it.

The `StrykerOutput/<timestamp>/` directory is created (a single `.gitignore` file inside) but no `logs/`, no `reports/`, no `mutants/` subdirectories are populated.

### Reproducer

Run, from inside the test-project directory (`tests/Aisess.Tests/`), against the v1.43.0 W2.a snapshot of the Aisess Platform:

```bash
dotnet stryker-netx \
  --solution ../../aisess-platform.slnx \
  --mutate "C:/claude_code/survey_server/src/Aisess.Application/Tenancy/PurgeApprovalUIService.cs" \
  --mutate "C:/claude_code/survey_server/src/Aisess.Domain/Tenancy/PurgeApprovalSource.cs" \
  --mutate "C:/claude_code/survey_server/src/Aisess.Domain/Tenancy/TenantPurgeApproval.cs" \
  --reporter html --reporter progress --mutation-profile Stronger
```

### Observed log up to the hang

```
Version: 3.2.12
[18:26:54 INF] Stryker will use a max of 4 parallel testsessions.
[18:26:54 INF] Analysis starting.
[18:26:54 INF] Analyzing 1 test project(s).
…(no further lines for 50+ minutes; output file size remains 0 bytes)
```

### Workaround

Drop `--solution` entirely and use the explicit project pointer (`--project <absolute-path>.csproj`) — see §3 below. The project-mode does produce log output (no silent hang), at the cost of a different bug (§4).

Note that `--solution` *is* the documented mode for multi-project mutation runs per the stryker-netx configuration documentation.

### Severity

**High** — the silent-hang is hard to distinguish from "mutation is in progress, just very slow"; the user has no way to tell from the log whether the process is making progress, in a deadlock, or waiting on an external resource (e.g., NuGet restore).

### Status

🔄 **Open**. Reproduced repeatedly on Windows 11 / SDK 10.0.107 / `.slnx` with version 3.2.12. Not retested with 3.2.13 because we pivoted to `--project` mode (§3) for our v1.43.0 carry-over investigation.

### Suggested diagnostic improvements

- Even one `INF`-level "Building solution…" or "Discovering tests in N projects…" log line would let the user distinguish active progress from a stuck process.
- A heartbeat-style "Initial test run elapsed: Nm Ms" emitted every 30 s during the long initial-test-run phase would solve the same UX problem at very low engineering cost.

---

## §3 `--project <short-name>` rejected with multi-reference error even when explicitly supplied (3.2.13) — **OPEN — UX**

### Symptom

When the test project references multiple source projects (a normal multi-layer DDD setup) and the user supplies `--project Aisess.Application` (the project's **short name**, matching the assembly name and `.csproj` basename), stryker-netx rejects the command with:

```
[17:42:28 INF] Analysis complete.
Stryker.NET failed to mutate your project. For more information see the logs below:

Test project contains more than one project reference. Please set the project
option (https://stryker-mutator.io/docs/stryker-net/configuration#project-file-name)
to specify which project to mutate.
Alternatively, run stryker-netx with --all-projects to mutate ALL referenced
source projects sequentially, or with --solution <path>.slnx for whole-solution
mode.
Choose one of the following references:

  C:/claude_code/survey_server/src/Aisess.Domain/Aisess.Domain.csproj
  C:/claude_code/survey_server/src/Aisess.Application/Aisess.Application.csproj
  …
```

The error message is misleading — the user **has** supplied a `--project` value; the tool simply doesn't accept the short-form.

### Reproducer

From `tests/Aisess.Tests/`:

```bash
# Fails with "Test project contains more than one project reference":
dotnet stryker-netx --project Aisess.Application \
  --mutate "C:/.../src/Aisess.Application/Tenancy/PurgeApprovalUIService.cs" \
  --reporter html --reporter progress

# Succeeds:
dotnet stryker-netx \
  --project "C:/claude_code/survey_server/src/Aisess.Application/Aisess.Application.csproj" \
  --mutate "C:/.../src/Aisess.Application/Tenancy/PurgeApprovalUIService.cs" \
  --reporter html --reporter progress
```

### Workaround

Supply the absolute `.csproj` path (Windows-style with forward slashes works on Windows; native backslashes work too).

### Severity

**Low — UX issue.** Functional once you know the workaround. But the error message **claims** "no `--project` set" when the user has clearly set it; this misleads casual users into thinking they forgot the flag entirely. A targeted error like "Project `Aisess.Application` resolved to multiple candidates; please disambiguate via absolute path" would be much friendlier.

### Status

🔄 **Open**. Reproduced on stryker-netx 3.2.13.

### Suggested fix

Either accept short-name matching when exactly one source-project matches the supplied string OR emit an unambiguous error like:

```
The --project value 'Aisess.Application' is ambiguous. Did you mean
`C:/.../src/Aisess.Application/Aisess.Application.csproj`?
Please supply the absolute path or the relative path including the
`.csproj` extension.
```

---

## §4 Initial test-run ignores `Category!=Integration` trait-filter — discovers 186 extra Integration tests that need Docker containers (3.2.13) — **OPEN**

### Symptom

Stryker's initial-test-run phase discovers **3 840 tests** in the test project, even though the same project run under `dotnet test --filter "Category!=Integration"` reports only **3 654 tests** (the additional 186 are tagged `[Trait("Category", "Integration")]` and only succeed when the project's Testcontainers fixtures bring up Postgres / Keycloak / MailHog).

In a typical CI / dev environment where these containers are explicitly excluded from the fast feedback loop, the initial test-run logs:

```
[18:27:35 INF] Number of tests found: 3840 for project
              C:\claude_code\survey_server\src\Aisess.Application\Aisess.Application.csproj.
              Initial test run started.
[18:36:53 WRN] 59 tests are failing. Stryker will continue but outcome will be impacted.
```

Those 59 "failing" tests are not actually broken — they are the Integration-tagged tests that need a Docker network the user deliberately did not bring up because mutation testing should run against the **unit** test suite only.

### Reproducer

From `tests/Aisess.Tests/`:

```bash
# Baseline — Aisess CI / dev expects this run to be green:
dotnet test ../../aisess-platform.slnx --filter "Category!=Integration"
# → 3654 tests, 3654 passed.

# Stryker's initial test-run:
dotnet stryker-netx \
  --project "C:/claude_code/survey_server/src/Aisess.Application/Aisess.Application.csproj" \
  --mutate "C:/claude_code/survey_server/src/Aisess.Application/Tenancy/PurgeApprovalUIService.cs" \
  --reporter html --reporter progress
# → "Number of tests found: 3840", "59 tests are failing"
```

The diff (3840 − 3654 = 186) matches exactly the number of `[Trait("Category", "Integration")]`-tagged tests in `Aisess.Tests`.

### Workaround

We have not yet found a stryker-netx CLI flag that filters tests by xUnit trait. Possible options (untested):

- Add a `<TestFilter>` element to `stryker-config.json` — **not documented as supporting xUnit traits**, only as the `--test-runner` filter expression.
- Use `<test-projects>` in the config to constrain to a *different* test assembly — not applicable when there is exactly one test project that holds both unit and integration tests.
- Refactor the test project into two projects: `Aisess.Tests.Unit` + `Aisess.Tests.Integration`. This is a larger structural change and not free.

### Severity

**Medium**. The user-facing impact is that Stryker either (a) drowns the mutation score in noise from Integration-tagged tests that aren't even relevant to the mutated source file, or (b) requires the user to bring up the full container stack for *every* mutation run. The mutation score formula `Score = (Killed + Timeout) / (Killed + Survived + Timeout + NoCoverage)` is meaningful only when the **baseline initial test-run** is 100 % green; failing Integration-tagged tests directly contaminate that baseline.

### Status

🔄 **Open**. Reproduced on stryker-netx 3.2.13.

### Suggested feature

A first-class `--test-filter "Category!=Integration"` CLI option (or equivalent `"test-filter"` config key) that is forwarded verbatim to the underlying `dotnet test` runner. This would mirror the equivalent flag in `dotnet test` and would let projects with mixed unit + integration test trees opt out of the slow lane.

---

## §5 `// Stryker disable next-line` ignored inside multi-line method-chain expressions (3.2.12) — **OPEN — see dedicated issue file**

### Symptom (short)

For a multi-line method-chain such as

```csharp
var x = await _repository
    .GetAsync(slug, ct)
    // Stryker disable next-line all : equivalent — xUnit no SyncContext
    .ConfigureAwait(false);
```

the directly-adjacent `// Stryker disable next-line all` directive is silently ignored. The `Boolean` mutation `false → true` on the `.ConfigureAwait(false)` line survives. The directive only works when it is placed **outside** the parent statement, paired with a closing `// Stryker restore all`:

```csharp
// Stryker disable all : equivalent — xUnit no SyncContext
var x = await _repository
    .GetAsync(slug, ct)
    .ConfigureAwait(false);
// Stryker restore all
```

### Severity

**Medium** — workaround exists (wrap-style) but is more verbose and prone to mis-pairing the `disable` / `restore` directives across edits.

### Status

🔄 **Open. Dedicated issue file** with full reproducer, environment, HTML-report excerpts, and three independent failure-mode tries: [`_docs/issues/stryker-netx-3.2.12-disable-directive-multiline-statement.md`](_docs/issues/stryker-netx-3.2.12-disable-directive-multiline-statement.md). Pending maintainer review since 2026-05-10.

This bug is **anchored in the Aisess `CLAUDE.md`** with the pattern enforcement:

> **Multi-Line-Method-Chains mit `.ConfigureAwait(false)` — Wrap-Style PFLICHT (Hardening-7.1-Discovery)**: Bei mehrzeiligen `await x.M(args).ConfigureAwait(false)` Chains greift `// Stryker disable next-line …` weder mit `Boolean`- noch mit `all`-Mutator-Kind, egal an welcher Position innerhalb der Multi-Line-Expression der Comment steht. Stryker's Direktiven-Matcher inspiziert offenbar nur Trivia auf Statement-Boundary, nicht innerhalb der Expression.

---

## §6 `// Stryker disable next-line all,Boolean` parses `all` as a literal mutator-kind name → error-log on every run (3.2.12) — **OPEN — DX**

### Symptom

The disable-directive syntax accepts a comma-separated list of mutator-kinds for fine-grained suppression. The all-encompassing `all` keyword (without a list) also works. But when the user writes `all` **as one of the elements of a list** — typically because they want to disable everything *plus* be explicit about which kinds they intend — stryker-netx interprets the literal token `all` as a mutator-kind name, fails to match it, and emits an error log:

```csharp
// Stryker disable next-line all,Boolean : equivalent
var x = ...;
```

logs roughly:

```
[ERR] Unknown mutator kind 'all' on file <path>:<line>
```

Even worse, this error log is emitted **every run, every time**, including when the file in question is not in the `--mutate` set (see §8). The directive `next-line all` (without the list) works fine.

### Reproducer

Apply this directive in any source file in the solution:

```csharp
// Stryker disable next-line all,Boolean : test
var x = false;
```

then run any mutation command. The error log appears at run-time and is logged for **every** stryker-netx execution, not just the one targeting that file.

### Workaround

Use either `// Stryker disable next-line all` (without list) **or** `// Stryker disable next-line Boolean` (just the kinds you want), but never both.

### Severity

**Low — DX issue.** No functional impact (the wider `all` is honored as expected), but the recurring error log creates "log fatigue" — users learn to ignore stryker-netx error output, which is dangerous because **real** errors are buried in the same channel.

### Status

🔄 **Open**. Reproduced on stryker-netx 3.2.12.

### Suggested fix

Treat `all` as a special token even when it appears as one of the comma-separated elements: silently un-list it (`all,Boolean` → `all`) and emit at most one info-log explaining the canonicalization. Or, less generously, emit a **warning** rather than an error, and emit it **once** per directive (not every run).

---

## §7 `ConfigureAwait` is not a valid mutator-kind despite being a frequently-disabled construct (3.2.12) — **OPEN — DX/Docs**

### Symptom

Documentation and community blog-posts about Stryker.NET sometimes refer to `ConfigureAwait` mutations as if they were a stand-alone mutator-kind. They are not — `ConfigureAwait(false → true)` mutations are emitted by the **Boolean** mutator (the boolean literal `false` is the target). Users who attempt the intuitive

```csharp
// Stryker disable next-line ConfigureAwait : equivalent under xUnit no-SyncContext
var x = await M().ConfigureAwait(false);
```

trigger the same "unknown mutator-kind" error as §6.

The list of valid mutator-kinds in stryker-netx 3.2.12 is (per source inspection):

```
Statement, Arithmetic, Block, Equality, Boolean, Logical, Assignment,
Unary, Update, Checked, Linq, String, Bitwise, Initializer, Regex,
NullCoalescing, Math, StringMethod, Conditional, CollectionExpression
```

`ConfigureAwait`, `Await`, `Async`, `Task` are not on the list — they are internal mutator class names, not user-facing kind names.

### Severity

**Low — DX/Docs.** Users encounter this when trying to disable `ConfigureAwait` mutations from xUnit-style tests where they're observably equivalent. The workaround is trivial (`Boolean`), but the discovery process is frustrating because the official mutator-kind list is not surfaced anywhere convenient in the CLI or the generated HTML report.

### Status

🔄 **Open — Docs**. The fix can be either (a) a dedicated `ConfigureAwait` mutator-kind for user-friendliness, or (b) a clearer mention in the disable-directive docs that ConfigureAwait mutations belong to the `Boolean` family. Option (b) is much cheaper.

### Suggested fix

Add a short paragraph to the stryker-netx disable-directive documentation that lists the recognized mutator-kinds with example mutations, and explicitly mentions that `ConfigureAwait(false → true)` is a `Boolean` mutation.

---

## §8 Disable-comments are parsed in *every* source file in the solution, even outside the `--mutate` filter set (3.2.12) — **OPEN — DX/Perf**

### Symptom

When the user supplies `--mutate <fileA.cs> --mutate <fileB.cs>` to constrain mutation to specific files, stryker-netx still **parses disable-comments in every source file in the solution** during analysis. A broken disable-directive in `fileZ.cs` (for example a typo'd mutator-kind name, see §6 and §7) produces an error log even when `fileZ.cs` is not in the `--mutate` set and will not be mutated this run.

We discovered this in the Aisess Platform when a stale `// Stryker disable next-line ConfigureAwait : reason` (invalid kind name) in `src/Aisess.Api/Middleware/TenantContextMiddleware.cs` continued to log an error on every mutation run, even when we were only mutating `Aisess.Application` files.

### Workaround

Fix the disable-directives project-wide (treat them like compiler warnings). Keep the directive syntax consistent throughout the codebase. We anchored this lesson in our `CLAUDE.md`:

> **stryker-netx scant ALLE Files nach Disable-Comments** — auch außerhalb des `--mutate`-Filters. Eine kaputte Disable-Direktive in einer nicht-mutierten Datei erscheint als Error-Log bei JEDEM Run. → Disable-Syntax projektweit konsistent halten.

### Severity

**Low — DX/Perf.** Not a correctness problem; the mutation set is still correctly constrained. But the error logs from unrelated files create confusion ("why is this file in my run, I didn't ask for it?") and a parsing overhead that scales with solution size.

### Status

🔄 **Open**. Reproduced on stryker-netx 3.2.12.

### Suggested fix

Either (a) scope disable-directive parsing to the `--mutate` filter set, or (b) clearly log "Validated disable-directives in N files (M errors)" once during analysis, then suppress per-line repeats.

---

## §9 `// Stryker disable next-line` is wirkungslos on per-line directives within object-initializers and chained method calls (3.2.11) — **PARTIALLY RESOLVED in 3.2.12**

### Symptom (3.2.11)

In versions ≤ 3.2.11, the per-line `// Stryker disable next-line` directive was **unreliable for object-initializers** (multi-line `new T { Prop1 = ..., Prop2 = ... }`) and **chained method calls** even when placed correctly on the previous line.

### Status

✅ **Partially resolved in 3.2.12** — per-line directives now apply correctly to object-initializers with the documented `// Stryker disable next-line <kinds> : <reason>` syntax. **However**, the residual issue documented in §5 (multi-line method-chains with `.ConfigureAwait(false)`) remains open in 3.2.12.

Anchored in the Aisess `CLAUDE.md`:

> **stryker-netx 3.2.12 fixt Bugs aus Aisess-Hardening-Sprint-2.5: spurious `configureawait`-Mutator-Error, unzuverlässige `next-line`-Direktive bei Object-Initializern.**

---

## §10 Wishlist / feature-requests

Compiling these from the Aisess team's experience operating stryker-netx through 8+ sprints of production-grade DDD-Onion software:

1. **`--test-filter <expression>` CLI flag** matching `dotnet test --filter` semantics, surfaced as a stryker-config-json key as well (resolves §4).
2. **Heartbeat-log during long initial test-runs** ("Initial test run in progress: Nm Ms elapsed", emitted every 30 s — resolves §2 silent-hang UX).
3. **Short-name resolution for `--project`** when exactly one match exists in the test project's references (resolves §3 misleading error).
4. **Disable-directive validation scoped to the `--mutate` filter set** (resolves §8).
5. **Heart-beat log even in `--solution` mode** — currently the solution-mode log goes silent for *hours* in larger solutions; some kind of progress signal would resolve §2.
6. **First-class `ConfigureAwait` mutator-kind alias** for the `Boolean` mutator when the literal is the argument of `.ConfigureAwait(…)` (resolves §7, also useful for fine-grained mutation profiles like `--mutation-profile ExcludeConfigureAwait`).
7. **Solution-wide disable-directive validation step at startup** that prints all parsed directives + warnings/errors in a single block, separate from per-mutator logs (resolves §6 + §8 log-fatigue).
8. **Better DX docs** — concrete examples for each mutator-kind with the corresponding disable-directive syntax (resolves §7).
9. **A `--break-after build` (or `--break-after initial-test-run`) flag** for diagnostic runs that should not perform any actual mutations. Currently, the only way to get the analysis output without running mutations is to wait for the entire initial test-run to complete; for a 3 600-test suite that is ≈ 9 minutes per diagnostic try.

---

## §11 Cross-references to dedicated issue files

| # | Title | File | Status |
|:-:|---|---|:-:|
| §1 | `.slnx` solution: 0 mutable projects | [`_docs/issues/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md`](_docs/issues/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md) | ✅ Resolved 3.2.11 |
| §5 | `next-line` ignored in multi-line method-chain | [`_docs/issues/stryker-netx-3.2.12-disable-directive-multiline-statement.md`](_docs/issues/stryker-netx-3.2.12-disable-directive-multiline-statement.md) | 🔄 Open |
| §1-§4 | Hardening-Sprint-2.5 validation report (3.2.12 baseline) | [`_docs/hardening sprint/stryker_netx_3.2.12_validation.md`](_docs/hardening%20sprint/stryker_netx_3.2.12_validation.md) | 📝 Internal |
| §2-§4 | v1.43.0 W2.a investigation (3.2.13 retry, --solution hang + --project short-name + test-discovery filter) | [`_docs/hardening sprint/v1.43.0-stryker-investigation.md`](_docs/hardening%20sprint/v1.43.0-stryker-investigation.md) | 📝 Internal |

---

## Distribution & forwarding

This file is **internal Aisess documentation**. It is **not** an open GitHub issue; the Aisess team decides when and to whom each finding is forwarded.

### What this report is NOT for

- ❌ **Not** for filing against `stryker-mutator/stryker-net` (the upstream original Stryker.NET) — that project is not .NET-10-compatible and the bugs documented here pertain to the **fork**, not the original.
- ❌ Not a public release: the file ships with the Aisess repository for engineering reference and is intended for selective curated forwarding (e.g., to the `dotnet-stryker-netx` fork maintainers via whatever channel they prefer, or kept internal as a workaround inventory).

### Provenance of the reproducers

All reproducers above are extracted from public commits on `pgm1980/aisess-platform` (tags `v1.0.0` … `v1.45.0`). Specific commit hashes are listed in the dedicated issue files (§11).

Each item carries an **independent reproducer** and is intentionally scoped to **one observable failure mode**, so the items can be triaged and split across separate downstream channels as appropriate without re-cutting the report.
