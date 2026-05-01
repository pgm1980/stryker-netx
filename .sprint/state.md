---
current_sprint: "22"
sprint_goal: "Wire --mutation-profile CLI flag + JSON config + per-profile cached E2E tests → v2.9.0"
branch: "feature/22-mutation-profile-cli"
started_at: "2026-05-01"
housekeeping_done: true
memory_updated: true
github_issues_closed: true
sprint_backlog_written: true
semgrep_passed: true
tests_passed: true
documentation_updated: true
---

# Sprint 22 — MutationProfile CLI/Config plumbing + E2E

**Base-Tag:** `v2.8.0` (Sprint 21 closed)
**Final-Tag:** `v2.9.0`
**Reference:** Closes the v2.7.0 design-note debt recorded in `tests/Stryker.E2E.Tests/Infrastructure/StrykerRunCacheFixture.cs` — `--mutation-profile` was reachable only via in-process `new StrykerOptions { MutationProfile = ... }`; CLI/JSON surface was the missing wire.

## Architecture decisions

- **D1**: Add `MutationProfileInput` to `IStrykerInputs` interface (only `StrykerInputs` exposed it; the interface omitted it — picked up while mapping the wiring).
- **D2**: CLI registration follows `--mutation-level` pattern in `CommandLineConfigReader.PrepareCliOptions` (NOT `StrykerCli.cs`; user mention referred to the CLI entry-point project as a whole).
- **D3**: No short option for `--mutation-profile` — all single-letter slots in `InputCategory.Mutation` (`-l`, `-m`) are taken; collision-free `null` short keeps help text clean.
- **D4**: `FileBasedInput`: new `[JsonPropertyName("mutation-profile")] public string? MutationProfile { get; init; }` mirrors `MutationLevel` exactly. `FileConfigSerializerContext` is source-gen-driven on `FileBasedInputOuter` — new property is picked up automatically.
- **D5**: `FileConfigGenerator` adds the field so `stryker init` documents the new key in generated configs.
- **D6**: Per-profile E2E cache: `StrykerRunCacheFixture` gets `GetDefaultsRun() / GetStrongerRun() / GetAllRun()` cached helpers, all `--reporter json` for parseability. Three new `[Fact]`s in `SampleE2EProfileTests`: All > Defaults, Stronger ≥ Defaults, All ⊇ Defaults file-map. Wall-clock budget: ~25 s per profile, 2 new profile-runs cached → ~50 s additional.
