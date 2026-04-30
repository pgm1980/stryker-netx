# Task Completion Checklist

When completing any code change in stryker-netx, run through this checklist BEFORE claiming done:

## Build & Test (PFLICHT)
- [ ] `dotnet build stryker-netx.slnx -c Release` — exits with 0 warnings, 0 errors (TWAE active)
- [ ] `dotnet test stryker-netx.slnx -c Release --collect:"XPlat Code Coverage"` — all tests green
- [ ] No `#pragma warning disable` introduced without inline comment justification
- [ ] No `<NoWarn>` added to csproj

## Code Quality (CLAUDE.md)
- [ ] FluentAssertions used in tests (NOT `Assert.Equal`, NOT Shouldly)
- [ ] `ConfigureAwait(false)` on all `await` in library code
- [ ] `catch (Exception ex) when (ex is not OperationCanceledException)` at system boundaries
- [ ] `sealed` for non-inheritable classes
- [ ] XML-doc comments on public APIs

## Security (PFLICHT for security-relevant code)
- [ ] `semgrep scan --config auto .` — no new findings
- [ ] No secrets in code/config (Semgrep `secrets.detected` rule)
- [ ] `dotnet list package --vulnerable` — no high-severity findings

## Architecture
- [ ] ArchUnitNET tests pass (when `tests/Stryker.Architecture.Tests/` exists from Phase 7)
- [ ] New namespaces/layers covered by Architecture-Tests
- [ ] No circular dependencies

## Tooling Compliance
- [ ] Used Serena (find_symbol, get_symbols_overview) for code-symbol navigation
- [ ] Consulted Context7 BEFORE using a new API (Buildalyzer 9, Roslyn updates, etc.)
- [ ] Sequential Thinking (Maxential) for architecture decisions ≥10 thoughts; ≥3 for trade-offs
- [ ] Tree of Thoughts for multi-option exploration when ≥2 valid solutions exist

## Git Hygiene
- [ ] Conventional Commits (`type(scope): description`)
- [ ] DCO sign-off (`git commit -s` — Signed-off-by trailer)
- [ ] Branch named `feature/<issue-nr>-<short-desc>` or `fix/<issue-nr>-<short-desc>`
- [ ] Commit references the GitHub issue (`Refs #N` or `Closes #N`)

## Sprint Hygiene (at sprint close)
- [ ] `.sprint/state.md` items updated (memory_updated, documentation_updated, semgrep_passed, tests_passed)
- [ ] `MEMORY.md` and `DEEP_MEMORY.md` updated with surprising/non-obvious findings
- [ ] GitHub issues closed with reference commit
- [ ] Sprint tag (when epic milestone reached)

## After Subagent Returns (CLAUDE.md PFLICHT)
- [ ] Main session re-runs `dotnet build` + `dotnet test` + `semgrep scan` (trust but verify)
- [ ] Spot-check Serena `get_symbols_overview` on new files
- [ ] Verify FluentAssertions used (not Assert.Equal)
- [ ] Verify ArchUnitNET-Tests added if new namespaces introduced
