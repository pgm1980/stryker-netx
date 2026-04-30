# Sprint 2 — Code Excellence: Lessons Learned

**Sprint:** 2 (2026-04-30, autonomous run)
**Branch:** `feature/2-code-excellence`
**Base-Tag:** `v1.0.0-preview.1`
**Reference:** `_config/csharp-10-bis-14-sprachfeatures.md`
**Strategy:** 9 sub-phases (Audit + 7 modernizations + Closing), strict CLAUDE.md compliance (Serena, Maxential, Context7, Semgrep)

## Sprint Outcome

| Metric | Result |
|--------|--------|
| Sub-phases executed | 9 / 9 |
| Build status | 0 warnings, 0 errors solution-wide (15 projects) |
| Test status | 27 / 27 pass (17 Sample.Tests + 10 Architecture.Tests) |
| E2E (`dotnet stryker-netx --solution Sample.slnx`) | 100.00 % Mutation-Score (5/5 mutants killed) |
| E2E (`dotnet stryker-netx --config-file stryker-config.json`) | 100.00 % Mutation-Score (validates source-gen JSON path) |
| Semgrep scan | 0 findings on every changed-file batch |
| Public API drift (Stryker.* libraries) | none |
| `Stryker.CLI.IExtraData` API | `init` → `set` on ExtraData (mandatory for source-gen, internal-CLI scope) |

## Sub-Phase Highlights

### 2.1 — Audit
- Inventoried candidates per feature category via grep (counts, files, line numbers).
- Flagged risks **before** refactoring: scope-management saved hours later.
- **Lesson:** Without the audit, several sub-phases would have over-converted (RSL, ArgumentNullException, record struct).

### 2.2 — `[GeneratedRegex]` Source Generators
- 6 sites in 4 files; clean conversion with no surprises.
- Source generator emits Compiled-equivalent code at build time → `RegexOptions.Compiled` removed (would be wasted work otherwise).
- `TimeSpan.FromMilliseconds(N)` constructor argument has to be replaced by integer `matchTimeoutMilliseconds: N`.
- **Lesson:** Quick-win confirmed. ~30 minutes of work, immediate AOT/perf benefits.

### 2.3 — Extension Members C# 14
- Major refactor: 30 methods (26 + 3 + 1 private) reorganised into `extension(IProjectAnalysis projectAnalysis) { ... }` blocks.
- The compiler emits classic `static M(this T t, ...)` entry points → callers untouched (verified by full E2E).
- **Two analyzer false-positives** had to be suppressed:
  - **CA1708** ("members differ only by case") fires when a class hosts two `extension(...)` blocks with different receiver types. Documented suppression at class scope.
  - **CA1822 / S2325** ("can be static") fires on extension methods that don't access the receiver — preserved with `_ = projectAnalysis;` discard, matching the pre-refactor pattern.
- **Lesson:** Roslyn analyzer cohort (Roslynator/Sonar/Meziantou) hasn't fully caught up to C# 14 extension members. Pragma suppressions with explicit justification are appropriate. Re-evaluate after analyzer 4.16+.

### 2.4 — `ArgumentNullException.ThrowIfNull`
- 37 audit candidates → only 19 actually refactored. **Categorisation matters.**
- 19 sites were field-init `?? throw` in **primary constructors** — kept as-is (that IS the modern primary-ctor idiom; converting back to explicit ctor + ThrowIfNull would expand code and fight C# 12).
- 19 sites in **explicit constructors** were converted to `ThrowIfNull(x); _field = x;` for the CallerArgumentExpression-driven error messages.
- **Lesson:** Audit-by-grep over-counts when patterns share a textual signature but diverge structurally (primary-ctor vs explicit-ctor). Always categorise before refactoring.

### 2.5 — Raw String Literals
- 29 audit candidates → only 7 actually refactored (escape-elimination or multi-line interp gain).
- Multi-line `@"..."` text without embedded quotes is **functionally equivalent** to RSL — converting them is diff noise.
- Real wins: SourceProjectNameInput.cs (`""` escape eliminated), CoverageCollector.cs / VsTestContextInformation.cs / SseEvent.cs (XML/SSE templates with cleaner indentation).
- **Lesson:** RSL is a tool, not a doctrine. Convert when there's an escape to remove or genuine indentation gain.

### 2.6 — `field` Keyword + `record struct`
- 9 backing-field audit candidates → only 3 converted (Position._line/_column, StrykerOptions._configuration). The rest had `ref` access (Interlocked) or external read paths that prevent `field` substitution.
- **`record struct`: 15 sealed records → 0 conversions.** None of the protocol DTOs in `Stryker.TestRunner.MicrosoftTestPlatform.Models` are in tight allocation loops. Microsoft's own guidance reserves record struct for sub-16-byte tight-loop allocations; converting these would change reference→value semantics with no measurable benefit.
- **Lesson:** "Modern idiom" is not "always preferred". `record struct` is for narrow performance scenarios.

### 2.7 — List Patterns + Type Aliases + Property Patterns
- 2 high-value list-pattern conversions (`StaticInstrumentationEngine.Revert`, `DefaultInitializationEngine.Revert`) — eliminate temporal-coupling between `Count` check + indexer access.
- `Count == 0` not converted to `is []`: many sites use types where `IReadOnlyList<T>` is not the nominal interface, and `is []` requires the reader to confirm list-pattern eligibility.
- Type aliases: 6 existing situational uses; no new aliases warranted.
- Extended property patterns: no clear candidates (most nested member access in this codebase is shallow).
- **Lesson:** Apply list patterns where they replace an exact `Count==N + indexer` pair; otherwise the older form is universally understood.

### 2.8 — `JsonSerializerContext` Source Generators (selective)
- Scoped to `FileBasedInputOuter` (CLI config). `JsonReport` pipeline left for a future sprint due to interface-polymorphism converters.
- **Major hazard discovered**: System.Text.Json source generator + `init` setters + `[JsonExtensionData]` is **incompatible**. The generator promotes `init` properties to synthetic-deserialization-constructor parameters, and `[JsonExtensionData]` cannot be a constructor parameter.
- Required API change: `IExtraData.ExtraData { get; init; }` → `{ get; set; }` (and on all 5 implementers). Stryker.CLI internal scope; library API unchanged.
- Added explicit `[JsonConstructor] public X() { }` parameterless constructors for resilience.
- **Lesson:** Source generators surface upstream type-design constraints that reflection silently tolerates. Always validate with a real round-trip (here: `stryker-config.json` E2E).

## Architectural / Process Lessons

1. **Audit-Refactor-Verify cadence is non-negotiable.** Every sub-phase: grep → categorise → refactor selected → build → tests → semgrep → E2E. The audit step prevented over-refactoring; the E2E step caught the JsonSerializerContext init/JsonExtensionData regression that local tests missed (no test exercised stryker-config.json deserialization).

2. **Analyzer false-positives are legitimate fix targets, but must be documented.** CA1708 on extension blocks and CA1822 on receiver-discarding extension members are both real C# 14 vs. analyzer-4.x mismatches. Pragma suppression with a sentence-long rationale is the right answer; silent suppression or feature avoidance is not.

3. **"Modern idiom" is context-sensitive.** Field-init `?? throw` IS modern for primary constructors. `record struct` is NOT the modern default for DTOs. RSL is NOT preferred for plain multi-line text. Each sub-phase had at least one "obvious" conversion that was wrong on closer inspection.

4. **Source generators are upstream-prescriptive.** GeneratedRegex/LoggerMessage are pure-additive (no API change). JsonSerializerContext is upstream-prescriptive — it dictates what shapes your types can take. Plan API budget accordingly.

5. **Phase-10.8 commit cherry-pick after Sprint 1 close was a clean recovery pattern.** When the feature branch was created from Sprint 1 close (ff28b89) but the canonical Sample.slnx commit lived on `main` (91c3cdb), `git cherry-pick` brought it into the feature branch without conflicts — preserved linear history without merge-commit churn.

## Comparison to Sprint 1

| Dimension | Sprint 1 | Sprint 2 |
|-----------|----------|----------|
| Goal | Port + bootstrap | Modernize |
| Phases | 10 (orig 7, +3 follow-up) | 9 (planned, executed cleanly) |
| Commits | 14 | 8 (Audit + 7 sub-phases) + 1 closing |
| New 3rd-party deps | 4 (Coverlet, FluentAssertions, FsCheck, BenchmarkDotNet, ArchUnitNET, **MSBuildWorkspace**) | 0 |
| Public API changes | 0 (1:1 port) | 0 (Stryker.* libs); 1 internal (Stryker.CLI.IExtraData init→set) |
| Build/Test/E2E delta | 0/0 → 0/0; 27/27; 100 % | 0/0 → 0/0; 27/27; 100 % |
| Lessons-doc length | ~250 lines | ~180 lines |
| Subagent-driven shortcut attempts | 3 (all rejected) | 0 (none attempted — autonomy mode held the line) |

Sprint 2 ran cleaner because Sprint 1 had already established the rhythm: Serena-first, Semgrep-before-close, no `<NoWarn>`, no `Nullable=disable`, no file-scope pragmas, and **no shortcut deferrals**.
