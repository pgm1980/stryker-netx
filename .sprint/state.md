---
current_sprint: "2"
sprint_goal: "Code Excellence: C# 10–14 advanced features + High-End Best Practices to push the modernized v1.0.0-preview.1 codebase from 'modern' to 'High-End'"
branch: "feature/2-code-excellence"
started_at: "2026-04-30"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: true
semgrep_passed: false
tests_passed: false
documentation_updated: false
---

# Sprint 2 — Code Excellence

**GitHub-Issue:** [#2](https://github.com/pgm1980/stryker-netx/issues/2)
**Base-Tag:** `v1.0.0-preview.1` (Sprint 1 closed)
**Reference-Doc:** `_config/csharp-10-bis-14-sprachfeatures.md`
**Strategie:** 9 Sub-Phasen, autonome Ausführung per CLAUDE.md (Serena IMMER, Maxential für Architektur, Context7 vor neuen APIs, Semgrep vor Sprint-Close)

## Aktueller Phase-Stand

- [ ] **2.1** — Audit (Serena-driven counts pro Feature)
- [ ] **2.2** — `[GeneratedRegex]` Source Generators alle Regex-Sites
- [ ] **2.3** — Extension Members (C# 14) für `IProjectAnalysisExtensions`
- [ ] **2.4** — `CallerArgumentExpression` in Input.Validate + Guards
- [ ] **2.5** — Raw String Literals `"""` für HTML/JSON-Templates
- [ ] **2.6** — `field` Keyword + `record struct` Audit + Apply
- [ ] **2.7** — List Patterns + Type Aliases + Ext Property Patterns
- [ ] **2.8** — `System.Text.Json` Source Generators für JsonReporter
- [ ] **2.9** — Sprint-2-Closing: Lessons + DoD + Tag-Entscheidung

## Sprint-2-DoD

- [ ] Alle 9 Sub-Phasen ausgeführt
- [ ] `dotnet build stryker-netx.slnx` 0 warnings, 0 errors
- [ ] `dotnet test` 27/27 pass
- [ ] E2E `dotnet stryker-netx --solution Sample.slnx` 100% Mutation-Score
- [ ] Semgrep clean
- [ ] 0 neue file-scope-pragmas, 0 `<NoWarn>`, 0 `<Nullable>disable</Nullable>`
- [ ] Lessons-doc `_docs/sprint_2_lessons.md` (analog zu Sprint 1)
- [ ] Public API Stryker.* unverändert (1:1 ADR-001/003 spirit)
- [ ] memory_updated=true (MEMORY.md + Sprint-2-closed memory)
- [ ] documentation_updated=true
- [ ] semgrep_passed=true
- [ ] tests_passed=true
- [ ] GitHub-Issue #2 geschlossen
- [ ] housekeeping_done=true

## Verweis

`_config/csharp-10-bis-14-sprachfeatures.md` für Feature-Reference (vom Project-Owner via Claude Opus chat-mode kuratiert)

## Sprint Context (auto-saved before compaction at 2026-04-30T18:14:14Z)

### Current Branch
feature/2-code-excellence

### Last 10 Commits
```
ff28b89 chore(sprint-1): close Sprint 1 — DoD complete, Modernization-Sweep done
17cb0bf refactor(patterns): Sprint 1 Phase 10.5 + 10.6 — pattern matching modernization
5299748 perf(collections): Sprint 1 Phase 10.4 — convert hot-path lookups to FrozenDictionary/FrozenSet
8c52667 refactor(idioms): Sprint 1 Phase 10.3 — collection expressions + selective primary constructors
8b662e6 refactor(logging): Sprint 1 Phase 10.2 — port all 233 ILogger calls to LoggerMessage source-generators
6f5cc87 feat(buildalyzer-removal): Sprint 1 Phase 9b — replace Buildalyzer with Microsoft.CodeAnalysis.Workspaces.MSBuild
3f8763b feat(layer0+): Sprint 1 Phase 9a — Workspaces.MSBuild adapter (parallel to Buildalyzer)
d21add7 fix(integration): Sprint 1 Phase 8 — end-to-end mutation run on Sample (Buildalyzer-9 fix)
7e5f3d4 feat(integration): Sprint 1 Phase 7 — DoD setup (ArchUnit + FsCheck + BenchmarkDotNet + sample)
06d1d2c feat(layer4): Sprint 1 Phase 6 — port Stryker.CLI + identity migration
```

### Recently Changed Files
```
.editorconfig
.sprint/state.md
_docs/sprint_1_lessons.md
src/Stryker.CLI/Clients/StrykerNugetFeedClient.cs
src/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs
src/Stryker.CLI/ConsoleWrapper.cs
src/Stryker.CLI/FileConfigGenerator.cs
src/Stryker.CLI/FileConfigReader.cs
src/Stryker.CLI/Logging/ApplicationLogging.cs
src/Stryker.CLI/StrykerCli.cs
src/Stryker.Configuration/ExclusionPattern.cs
src/Stryker.Configuration/FilePattern.cs
src/Stryker.Configuration/Options/Input.cs
src/Stryker.Configuration/Options/Inputs/ConcurrencyInput.cs
src/Stryker.Configuration/Options/Inputs/DiffIgnoreChangesInput.cs
src/Stryker.Configuration/Options/Inputs/IgnoreMethodsInput.cs
src/Stryker.Configuration/Options/Inputs/IgnoreMutationsInput.cs
src/Stryker.Configuration/Options/Inputs/MutateInput.cs
src/Stryker.Configuration/Options/Inputs/ReportersInput.cs
src/Stryker.Configuration/Options/Inputs/TestProjectsInput.cs
```

### Uncommitted Changes
```
 M .sprint/state.md
```
