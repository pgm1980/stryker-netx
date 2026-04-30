# Contributing to stryker-netx

Thanks for your interest in contributing to **stryker-netx** — a 1:1 port of [Stryker.NET](https://github.com/stryker-mutator/stryker-net) 4.14.1 to C# 14 / .NET 10.

This project is currently in a private repository during initial development. When it goes public, this document defines the contribution workflow.

---

## Code of Conduct

By participating, you agree to abide by the [Code of Conduct](CODE_OF_CONDUCT.md). Report unacceptable behavior to the maintainers.

---

## Developer Certificate of Origin (DCO)

All contributions to **stryker-netx** are subject to the [Developer Certificate of Origin (DCO)](https://developercertificate.org/). The DCO is a lightweight way for contributors to certify that they wrote (or have the right to submit) the code they are contributing under the project's Apache 2.0 license.

### How to sign your commits

Every commit must include a `Signed-off-by` trailer. Use the `-s` flag:

```bash
git commit -s -m "feat(core): your change description"
```

This adds a line like:
```
Signed-off-by: Your Real Name <your-email@example.com>
```

The name and email must match a real identity (GitHub-verified email recommended). The DCO check on PRs will block unsigned commits.

### What the DCO certifies

By signing off, you certify that:

1. The contribution was created in whole or in part by you, and you have the right to submit it under the project's Apache 2.0 license; **or**
2. The contribution is based on previous work that, to the best of your knowledge, is covered under an appropriate open-source license and you have the right to submit it under the project license; **or**
3. The contribution was provided directly to you by some other person who certified (1) or (2), and you have not modified it.
4. You understand and agree that this project and the contribution are public, and that a record of the contribution (including your sign-off) is maintained indefinitely.

Full DCO text: https://developercertificate.org/

---

## Contribution Workflow

### 1. Discussion First (for non-trivial changes)

Open an issue describing your proposed change before investing significant time. This ensures alignment with the project's 1:1-port-with-modernization scope.

### 2. Branch and PR

```bash
# Fork the repo on GitHub, then:
git clone https://github.com/<your-github-user>/stryker-netx.git
cd stryker-netx
git checkout -b feature/<issue-number>-short-description

# Make your changes...

git add <specific-files>
git commit -s -m "feat(scope): description"
git push origin feature/<issue-number>-short-description

# Open a PR via gh CLI:
gh pr create --title "..." --body "..."
```

Branch naming: `feature/<issue-nr>-...`, `fix/<issue-nr>-...`, `chore/...`, `docs/...`. See [_config/development_process.md](_config/development_process.md) for the full Scrum-based workflow.

### 3. Conventional Commits

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

- `feat(scope): add new mutator for ...`
- `fix(core): handle null in MsBuildHelper`
- `chore(deps): bump Buildalyzer to 9.0.1`
- `docs: update README compatibility section`
- `test: add property tests for EqualityMutator`
- `refactor(cli): extract IStrykerCommandLine wrapper`

The `<scope>` is typically the affected module (`core`, `cli`, `abstractions`, `solutions`, `testrunner`, `configuration`, etc.).

---

## Code Standards

This project enforces strict code quality. See [CLAUDE.md](CLAUDE.md) for the full set of directives. Key points:

### Build & Tests Must Be Green

- `dotnet build` must succeed with **0 Warnings, 0 Errors** (TreatWarningsAsErrors is active)
- `dotnet test` must pass all tests (Unit + Integration + ArchUnit + FsCheck Properties)
- `semgrep scan --config auto` must produce no new findings

### Analyzer Rules Are Mandatory

The build enforces:
- **Roslynator.Analyzers** (code-quality, simplifications, best practices)
- **SonarAnalyzer.CSharp** (security, reliability, maintainability)
- **Meziantou.Analyzer** (.NET-specific best practices)

`#pragma warning disable` is **forbidden** without a documented justification comment directly above it. Severity adjustments must go in `.editorconfig` with explanatory comments.

### Code Conventions

- **`sealed`** by default for non-inheritable classes
- **XML doc-comments** required on all public APIs
- **`ConfigureAwait(false)`** on all `await` calls in library code
- **Exception handling**: `catch (Exception ex) when (ex is not OperationCanceledException) { ... }`
- **Namespace** must match directory structure
- **Test framework**: xUnit + FluentAssertions + Moq + ArchUnitNET + FsCheck

### Tooling Pipelines

For Code-Symbol-Analyse use **Serena MCP** instead of grepping. For new APIs, consult **Context7 MCP** before usage. For complex architectural decisions, use **Sequential Thinking (Maxential)** for at least 10 reasoning steps. See CLAUDE.md for details.

---

## Test-Driven Development

Per the project policy, new features and bug fixes follow **TDD** (Red → Green → Refactor):

1. Write a failing test first
2. Make the test pass with minimal code
3. Refactor (with tests still green)

For property-based tests: use **FsCheck.Xunit** to express invariants (roundtrips, idempotence, commutativity).

For architecture rules: add **ArchUnitNET** tests when introducing new namespaces or layers.

---

## Documentation

- Update relevant docs alongside code changes (README, `_docs/`, ADRs)
- Public APIs need XML doc comments
- Significant decisions require a new ADR in `_docs/architecture spec/architecture_specification.md`
- Update `MEMORY.md` / `DEEP_MEMORY.md` for surprising or non-obvious findings

---

## Pull Request Checklist

Before requesting review, ensure:

- [ ] All commits are DCO-signed (`git commit -s`)
- [ ] Conventional Commit message format used
- [ ] `dotnet build` passes with 0 warnings, 0 errors
- [ ] `dotnet test` passes (all tests green)
- [ ] `semgrep scan --config auto` produces no new findings
- [ ] New public APIs have XML doc comments
- [ ] Architecture rules (ArchUnitNET) updated if new layers introduced
- [ ] Property tests (FsCheck) added for new invariants
- [ ] Performance benchmarks (BenchmarkDotNet) added for new hot paths
- [ ] Documentation updated (README / `_docs/` / ADRs)
- [ ] `MEMORY.md` updated if new surprising findings emerged

---

## Scope and Non-Goals

**In scope** for stryker-netx:
- 1:1 functional parity with Stryker.NET 4.14.1 CLI flags, config schema, and reporter outputs
- C# 14 / .NET 10 compatibility (port, modernize, fix .NET-10 incompatibilities)
- Code quality improvements (analyzer suite, ArchUnit tests, FsCheck properties)
- Internal modernization (sealed default, ConfigureAwait, etc.)

**Out of scope** for 1.0.0-preview.1:
- New mutator types not in Upstream 4.14.1
- Breaking changes to CLI flags or config schema
- IDE integrations (VS Extension, Rider Plugin)
- Visual Basic / F# / non-C# source mutation
- NativeAOT publishing (deferred — see ADR-006)
- Migration of older Stryker.NET config formats

For ideas outside this scope, please open a discussion issue first.

---

## Getting Help

- **Issues**: https://github.com/pgm1980/stryker-netx/issues
- **Discussions**: (when public) https://github.com/pgm1980/stryker-netx/discussions
- **Original Stryker.NET community** (for general mutation-testing questions): https://stryker-mutator.io/

---

## Acknowledgements

This project is a fork of and pays full attribution to the original [Stryker.NET](https://github.com/stryker-mutator/stryker-net) project by Richard Werkman, Rouke Broersma, and the Stryker Mutator community. See [NOTICE](NOTICE) for full attribution.
