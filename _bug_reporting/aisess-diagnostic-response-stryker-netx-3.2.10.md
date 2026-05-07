# Diagnostic response — Aisess `.slnx` mutable-assembly-resolution bug

> **Counterpart to**: `_request/aisess-diagnostic-request-stryker-netx-3.2.10.md`
> **From**: Aisess platform team (pgm1980)
> **To**: stryker-netx maintainers
> **Date**: 2026-05-07
> **Verdict**: **H2 confirmed.** `mutableProjects.Length == 0` while the test project's `ProjectReferences` collection is fully populated (4/4) and Stage-1 reference candidates are present (4/4 Aisess DLLs in `References[Aisess]`).

---

## 1. What we ran

Followed the request §2 recipe exactly, with one mechanical adjustment:

- Built diagnostic stryker-netx from the working tree at branch
  `chore/158-aisess-bug-intake` (HEAD `c4149cf`, descendant of `v3.2.10`,
  no source-code changes between them — only `_bug_reporting/*.md` and
  `.sprint/state.md`). `git diff v3.2.10..HEAD` confirms zero `.cs`
  changes, so the diagnostic patch ran against effectively `v3.2.10`
  source.
- The diff in §2 of the request had to be wrapped in
  `#pragma warning disable Spectre1000` / `restore Spectre1000` because
  the project's Spectre.Console analyzer otherwise rejects the eight
  `Console.WriteLine` calls with rule `Spectre1000`. No other change.
- Built via `dotnet pack src/Stryker.CLI/Stryker.CLI.csproj -c Release -o ./nupkgs`
  → produced `dotnet-stryker-netx.0.0.0-localdev.nupkg` (the source-tree
  version is `0.0.0-localdev`, not `3.2.10` — the version property is
  set on tag-builds only).
- Installed via `dotnet tool install -g dotnet-stryker-netx --add-source ./nupkgs --version 0.0.0-localdev`.
- Ran from `tests/Aisess.Tests/` after `rm -rf StrykerOutput/`:
  ```
  dotnet stryker-netx --config-file stryker-config.json --diag 2>&1 | tee diag-output.txt
  ```
- Restored production `dotnet-stryker-netx 3.2.10` afterwards, and
  reverted the patch on the stryker-netx working tree
  (`git restore src/Stryker.Core/Initialisation/InputFileResolver.cs`).

The full 1316-line `diag-output.txt` is preserved at
`_response/diag-output-2026-05-07T14-26.txt` in this repo and available
on request — the lines below are the structurally significant subset.

---

## 2. The `[DIAG]` lines (verbatim)

```text
[DIAG] mutableProjectsAnalyses.Count = 1, analyzerTestProjects = 1, mutableProjects = 0
[DIAG]   testProject.ProjectFilePath = 'C:\claude_code\survey_server\tests\Aisess.Tests\Aisess.Tests.csproj'
[DIAG]     References.Count       = 353
[DIAG]     ProjectReferences.Count = 4
[DIAG]       ProjectRef = 'C:\claude_code\survey_server\src\Aisess.Domain\Aisess.Domain.csproj'
[DIAG]       ProjectRef = 'C:\claude_code\survey_server\src\Aisess.Application\Aisess.Application.csproj'
[DIAG]       ProjectRef = 'C:\claude_code\survey_server\src\Aisess.Infrastructure\Aisess.Infrastructure.csproj'
[DIAG]       ProjectRef = 'C:\claude_code\survey_server\src\Aisess.Api\Aisess.Api.csproj'
[DIAG]       References[Aisess] = 'C:\claude_code\survey_server\src\Aisess.Domain\bin\Debug\net10.0\Aisess.Domain.dll'
[DIAG]       References[Aisess] = 'C:\claude_code\survey_server\src\Aisess.Application\bin\Debug\net10.0\Aisess.Application.dll'
[DIAG]       References[Aisess] = 'C:\claude_code\survey_server\src\Aisess.Infrastructure\bin\Debug\net10.0\Aisess.Infrastructure.dll'
[DIAG]       References[Aisess] = 'C:\claude_code\survey_server\src\Aisess.Api\bin\Debug\net10.0\Aisess.Api.dll'
```

## 3. The trailing trio (verbatim)

```text
[16:26:16 INF] Could not find an assembly reference to a mutable assembly for project C:\claude_code\survey_server\tests\Aisess.Tests\Aisess.Tests.csproj. Will look into project references.
[16:26:16 DBG] Analyzing 0 projects.
[16:26:16 WRN] No project found, check settings and ensure project file is not corrupted.
Use --diag option to have the analysis logs in the log file.
[16:26:16 INF] Analysis complete.
[16:26:16 INF] Time Elapsed 00:00:06.1230502
Failed to analyze project builds. Stryker cannot continue.
```

---

## 4. Hypothesis discrimination

Mapped against your §3 decision table:

| Your predicted observation | Our reading |
|----------------------------|-------------|
| **`mutableProjects = 0`** → **H2 confirmed** | ✅ `mutableProjects = 0` |
| `mutableProjects ≥ 1` and `testProject.ProjectReferences.Count = 0` → **H6** | ✗ `ProjectReferences.Count = 4` (Roslyn populated correctly) |
| `mutableProjects ≥ 1` with case/path mismatch → **H1** | ✗ Stage 1/2 never reach a comparison because `mutableProjects` is the empty array |
| `mutableProjects ≥ 1` with separator/path-form mismatch → **H7** | ✗ same as above |

**H2 is the right lever.** Loosen / remove the substring filter at
`InputFileResolver.cs:441-447`; the `"project"` config field is being
interpreted too aggressively for the `.slnx` code path.

---

## 5. Additional observations (beyond the H2 confirmation)

These are not necessary to pick the fix, but they tighten the picture
and may be useful for the integration test you mentioned in §3.

### 5.1 Where the source projects vanish

`mutableProjectsAnalyses.Count = 1` is the smoking gun. By the time
control reaches `FindMutableAnalyses`, *only the test project remains in
the collection.* The four `Aisess.{Domain,Application,Infrastructure,Api}`
projects have already been excluded upstream — most likely inside
`AnalyzeAllNeededProjects(... ScanMode.ScanTestProjectReferences)` at
the point where the `--project` / config-`"project"` filter is applied.

This is consistent with the verbose `[VRB] **** Project analysis result ****`
blocks we observed in our bug report's `--diag` log: every one of the
five `.csproj` files (1 test + 4 source) is parsed with `Succeeded: True`
by Buildalyzer. The Buildalyzer pass works. The downstream filter
discards the source-project results before they reach
`FindMutableAnalyses`.

### 5.2 Roslyn / `ProjectReferences` is *not* the problem

Our previous report flagged H6 ("`MSBuildWorkspace.OpenProjectAsync`
does not populate `ProjectReferences`") as a possible suspect because
the bare error message *suggested* an empty project-reference graph.
The diag confirms this is not the case:

- `testProject.ProjectReferences.Count = 4`
- All four entries are full absolute paths, no truncation, no case
  variation, no separator weirdness — they match exactly what
  `Aisess.Tests.csproj` declares.

So Roslyn / `MSBuildWorkspace` work correctly under `.slnx` for our
project-reference graph. The `Aspire.AppHost.Sdk/13.2.0` project under
`/tools/` did not destabilise the workspace. **H6 is dead.**

### 5.3 `References` (Stage 1) likewise carries the right candidates

`References.Count = 353` includes (filtered to `Aisess` substring):

```
C:\claude_code\survey_server\src\Aisess.Domain\bin\Debug\net10.0\Aisess.Domain.dll
C:\claude_code\survey_server\src\Aisess.Application\bin\Debug\net10.0\Aisess.Application.dll
C:\claude_code\survey_server\src\Aisess.Infrastructure\bin\Debug\net10.0\Aisess.Infrastructure.dll
C:\claude_code\survey_server\src\Aisess.Api\bin\Debug\net10.0\Aisess.Api.dll
```

All four DLL paths are correctly resolved to their `bin/Debug/net10.0/`
output paths (no `obj/` ref-only forms, no `Microsoft.Extensions.AI` style
generated reference assemblies). So once `mutableProjects` is non-empty,
both Stage 1 (`ScanAssemblyReferences`) and Stage 2 (`ScanProjectReferences`)
*should* find matches without any path-normalisation work — barring a
case-sensitive comparator, which we can no longer rule in or out from
this run because Stage 1/2 never get to compare anything.

So **H1 stays a latent risk** but is not the active fault in our case.
You may want to fix it pre-emptively when you touch this code anyway,
since on Windows `StringComparer.Ordinal` against MSBuild-supplied paths
is fragile.

### 5.4 `Analyzing 0 projects` is misleading wording

The `[DBG] Analyzing 0 projects` message printed right after the
`Will look into project references` info line is not literally about
"how many `<ProjectReference>` items the test project declares" — the
test project clearly declares 4. It seems to be a debug log emitted
*inside* `ScanProjectReferences` after the lookup against `mutableProjects`
returns no match because `mutableProjects` has 0 entries. Consider
re-wording this log on the fix branch — its current phrasing pointed
several reviewers (including us) at H6 / a `.csproj`-parse problem
when in fact the upstream filter is the culprit.

A friendlier message that names the actually-empty collection would
have been a 1-day instead of 1-week investigation:

```
[DBG] No mutable projects in mutableProjects collection (0 entries);
       cannot match testProject.ProjectReferences (4 entries).
       Source-project filter (-p / config "project") may have excluded all candidates.
```

---

## 6. What we will deploy on our side while you ship the fix

Now that we know the upstream filter is the cause, we'll try
**removing the `"project"` field** from `stryker-config.json` before we
maintain a parallel `.sln` file. The current value
`"project": "Aisess.Tests.csproj"` matches the test project itself
(it is the one project that should *never* be the source-under-test);
dropping it should leave the auto-discovery to do its thing. We will
ping back with the result if that turns out to be the dev-side
workaround.

If it doesn't work and the fix is not yet shipped, our fallback is the
`.sln`-alongside-`.slnx` scheme noted in §11 of our original bug report.

---

## 7. Build-environment notes (for repro)

| Component | Version |
|-----------|---------|
| OS | Windows 11 (24H2) |
| `dotnet --version` | 10.0.107 |
| stryker-netx upstream tag at run | `v3.2.10` (HEAD `c4149cf`, no `.cs` delta) |
| Diagnostic build version installed | `0.0.0-localdev` (via `dotnet pack`) |
| Solution format | `.slnx`, 7 projects (4 src + 1 tests + 1 benchmarks + 1 tools/Aisess.AppHost using `Aspire.AppHost.Sdk/13.2.0`) |
| Test SDK | xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1 |
| Patch deviation | `#pragma warning disable Spectre1000` / `restore` wrapping the diag block (otherwise `Spectre1000` analyser rejects `Console.WriteLine`) |

The patched `InputFileResolver.cs` was reverted with `git restore` after
the diagnostic run so the stryker-netx working tree is back to the
intake state.

---

## 8. Cross-references

- Original bug report: `_docs/issues/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md`
- Maintainer request: `_request/aisess-diagnostic-request-stryker-netx-3.2.10.md`
- Full diag transcript: `_response/diag-output-2026-05-07T14-26.txt` (1316 lines)
- Aisess Sprint 1 PR (where the bug was first observed): https://github.com/pgm1980/aisess-platform/pull/142
- Aisess `MEMORY.md` "Bekannte offene Punkte" #1
