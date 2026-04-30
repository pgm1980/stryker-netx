# Suggested Commands

## Build & Test (.NET 10)
```bash
dotnet --version                                                  # Verify SDK 10.0.x
dotnet restore stryker-netx.slnx                                  # Restore packages (creates packages.lock.json)
dotnet build stryker-netx.slnx -c Release                         # Build solution
dotnet test stryker-netx.slnx -c Release \
    --collect:"XPlat Code Coverage"                               # Test with coverage (PFLICHT)
dotnet test --filter "FullyQualifiedName~UnitTests"               # Unit tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests"        # Integration tests only
dotnet pack stryker-netx.slnx -c Release -o ./nupkg/              # Pack NuGet packages
dotnet run --project benchmarks/<Project>.Benchmarks -c Release   # BenchmarkDotNet (Release-mode PFLICHT)
```

## Security Scan (CLAUDE.md PFLICHT before sprint close)
```bash
semgrep scan --config auto .                                      # Full security scan
semgrep scan --config auto --changed-files                        # Only changed files
```

## Git (atomic commands allowed via Bash; multi-step workflows via gh CLI)
```bash
git status                            # Working-tree state
git log --oneline -10                 # Recent commits
git diff                              # Unstaged changes
git diff --cached                     # Staged changes
git add <specific-files>              # Stage specific files (NOT git add -A blindly)
git commit -s -m "..."                # Commit with DCO sign-off (PFLICHT, ADR-008)
git push                              # Push current branch
git checkout -b feature/<n>-<desc>    # Create + switch feature branch
```

## GitHub CLI (multi-step workflows)
```bash
gh issue create --title "..." --body "..." --milestone "..." --label "..."
gh issue view <n> --comments
gh pr create --title "..." --body "..."
gh pr view <n>
gh release create v<X>.<Y>.<Z> --title "..." --notes-file <file>
gh repo view pgm1980/stryker-netx
gh api repos/.../...                   # Generic GitHub REST API
```

## System (Linux runtime per Serena onboarding)
- `ls`, `cd`, `find`, `grep`, `cat` — convention-forbidden in dev workflow (use Serena/Built-In tools); but allowed in adhoc shell when needed (e.g., piping in commit messages)
- File operations preferred: `Read`, `Edit`, `Write`, `Glob`, `Grep` (Built-In Claude Code tools)
- Code symbol operations preferred: Serena `find_symbol`, `get_symbols_overview`, etc.

## Sprint Workflow Commands
- Brainstorming: invoke `brainstorming` skill
- Architecture: invoke `architecture-designer` skill
- Plan/Spec: write to `_docs/architecture spec/` or `_docs/design spec/`
- Sprint state: edit `.sprint/state.md` per CLAUDE.md schema
- Memory: edit `MEMORY.md` (index) + `DEEP_MEMORY.md` (full 360°)
