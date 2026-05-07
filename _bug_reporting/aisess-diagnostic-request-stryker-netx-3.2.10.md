# Diagnostic request — Aisess `.slnx` mutable-assembly-resolution bug (stryker-netx 3.2.10)

> **Counterpart to**: `_bug_reporting/stryker-netx-3.2.10-slnx-mutable-assembly-bug.md`
> **From**: stryker-netx maintainers
> **To**: Aisess platform team
> **Goal**: a single 5-line patch + one diagnostic run yields exactly the data we need to pick the correct fix from three competing hypotheses.

---

## 1. Context

We have read the bug-report end-to-end and audited the corresponding code paths in
`stryker-netx` v3.2.10. Three hypotheses survive the static analysis; they fail
in distinguishable ways at runtime, so a single diagnostic run nails the right
one.

| # | Hypothesis | What you'd see |
|---|-----------|----------------|
| **H1** | Stage 2 path-comparison uses `StringComparer.Ordinal` (case-sensitive). On Windows that is fragile because paths are case-insensitive. | `mutableProjects` non-empty, `testProject.ProjectReferences` non-empty, but the strings differ in case/format → no match. |
| **H2** | The `mutableProjects` set is empty after the project-name filter (`stryker-config.json` `"project"` field) excludes all 4 source projects. | `mutableProjects.Length == 0`. |
| **H6** | Roslyn `MSBuildWorkspace.OpenProjectAsync` does not populate `roslynProject.ProjectReferences` for the test project — possibly because the `Aspire.AppHost.Sdk/13.2.0` project under `/tools/` triggers a workspace-load diagnostic that destabilises the project-reference graph. | `testProject.ProjectReferences.Count == 0` even though the `.csproj` declares 4 `<ProjectReference>` items. |

---

## 2. The patch (one insertion, no behavioural change)

Clone `stryker-netx` at tag `v3.2.10`, apply the diff below to
`src/Stryker.Core/Initialisation/InputFileResolver.cs` (the file that produced
the `Could not find an assembly reference …` and `No project found …` log
messages), build, install the diagnostic build as a global tool, run once.

### Diff

The patch goes inside `FindMutableAnalyses` (the method that decides whether
to enter Stage 1 / Stage 2). Only the marked block is added — surrounding code
is unchanged.

```csharp
private (Dictionary<IProjectAnalysis, List<IProjectAnalysis>>, List<IProjectAnalysis>)
    FindMutableAnalyses(List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> mutableProjectsAnalyses)
{
    var analyzerTestProjects = mutableProjectsAnalyses
        .Where(p => p.isTest).SelectMany(p => p.result)
        .Where(p => p.BuildsAnAssembly())
        .ToList(); // <<< ToList added so the diag block can enumerate twice

    var mutableProjects = mutableProjectsAnalyses
        .Where(p => !p.isTest).SelectMany(p => p.result)
        .Where(p => p.BuildsAnAssembly()).ToArray();

    // ────────────── [STRYKER-NETX DIAG PATCH — START] ──────────────
    // Temporary instrumentation to disambiguate H1 / H2 / H6.
    // Remove this block after the diagnostic data has been captured.
    Console.WriteLine($"[DIAG] mutableProjectsAnalyses.Count = {mutableProjectsAnalyses.Count}, " +
                      $"analyzerTestProjects = {analyzerTestProjects.Count}, " +
                      $"mutableProjects = {mutableProjects.Length}");
    foreach (var mp in mutableProjects)
    {
        Console.WriteLine($"[DIAG]   mutable.ProjectFilePath = '{mp.ProjectFilePath}'");
        Console.WriteLine($"[DIAG]   mutable.GetAssemblyPath = '{mp.GetAssemblyPath()}'");
    }
    foreach (var tp in analyzerTestProjects)
    {
        Console.WriteLine($"[DIAG]   testProject.ProjectFilePath = '{tp.ProjectFilePath}'");
        Console.WriteLine($"[DIAG]     References.Count       = {tp.References.Count}");
        Console.WriteLine($"[DIAG]     ProjectReferences.Count = {tp.ProjectReferences.Count}");
        foreach (var pr in tp.ProjectReferences)
        {
            Console.WriteLine($"[DIAG]       ProjectRef = '{pr}'");
        }
        foreach (var r in tp.References.Where(r =>
            r.Contains("Aisess", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"[DIAG]       References[Aisess] = '{r}'");
        }
    }
    // ────────────── [STRYKER-NETX DIAG PATCH — END] ──────────────

    var mutableToTestMap = mutableProjects.ToDictionary(p => p, _ => new List<IProjectAnalysis>());
    var unusedTestProjects = new List<IProjectAnalysis>();
    // for each test project
    foreach (var testProject in analyzerTestProjects)
    {
        if (ScanAssemblyReferences(mutableToTestMap, mutableProjects, testProject))
        {
            continue;
        }

        LogNoAssemblyRef(_logger, testProject.ProjectFilePath);
        // we try to find a project reference
        if (!ScanProjectReferences(mutableToTestMap, mutableProjects, testProject))
        {
            unusedTestProjects.Add(testProject);
        }
    }

    return (mutableToTestMap, unusedTestProjects);
}
```

### Build + install

```powershell
git clone https://github.com/pgm1980/stryker-netx.git
cd stryker-netx
git checkout v3.2.10
# ── apply the diff above to src/Stryker.Core/Initialisation/InputFileResolver.cs ──
dotnet pack src/Stryker.CLI/Stryker.CLI.csproj -c Release -o ./nupkgs
dotnet tool uninstall -g dotnet-stryker-netx 2>$null
dotnet tool install   -g dotnet-stryker-netx --add-source ./nupkgs --version 3.2.10
```

### Run

```bash
cd <aisess-repo>/tests/Aisess.Tests
rm -rf StrykerOutput/
dotnet stryker-netx --config-file stryker-config.json --diag 2>&1 | tee diag-output.txt
```

The `[DIAG]` lines will be interleaved with normal stryker-netx output. Send
**every line that starts with `[DIAG]`** back, plus the very last lines
(the `Could not find … / No project found / Failed to analyze project builds`
trio).

---

## 3. How we read the output

| Observation | Hypothesis | Fix direction (for our reference, not yours) |
|------------|------------|----------------------------------------------|
| `mutableProjects = 0` | **H2** confirmed | Loosen / remove the substring filter at `InputFileResolver.cs:441-447`; the `"project"` config field is being interpreted too aggressively. |
| `mutableProjects ≥ 1` and `testProject.ProjectReferences.Count = 0` | **H6** confirmed | Change `RoslynProjectAnalysis._projectReferences` (Z. 61-64) to fall back to `evaluationProject.GetItems("ProjectReference")` when Roslyn's `ProjectReferences` collection is empty — that bypasses any Aspire-SDK / `.slnx`-Folders interaction with `MSBuildWorkspace`. |
| `mutableProjects ≥ 1` and `testProject.ProjectReferences.Count ≥ 1` and the printed `mutable.ProjectFilePath` strings differ from the printed `ProjectRef` strings only in case or path-form | **H1** confirmed | Replace `StringComparer.Ordinal` at `InputFileResolver.cs:590` with an `OrdinalIgnoreCase` comparison on `Path.GetFullPath`-normalised values (the same pattern `RoslynProjectAnalysis.BuildAllReferences` already uses for de-duplication). |
| `mutableProjects ≥ 1` and `testProject.ProjectReferences.Count ≥ 1` and the strings *are* identical, but Stage 1 still misses on `References[Aisess]` paths | New hypothesis (H7) — `mutableProject.GetAssemblyPath()` returns a path with a `\` vs `/` separator difference from `testProject.References[i]`, or `OutputRefFilePath` returns `obj/Debug/.../ref/...dll` while `testProject.References` carries the `bin/Debug/.../X.dll` form. | Path-format normalisation in `ScanAssemblyReferences` (`InputFileResolver.cs:604-627`). |

We will commit the fix on a fresh branch (`fix/159-slnx-mutable-assembly-resolution`)
once the diagnostic output identifies which lever to pull. Expected turnaround
after we receive the output: same day for **H2** (one-line filter change), 1-2
days for **H1/H6/H7** (small patch + integration test on a `.slnx`-with-Folders
fixture in our `samples/` tree).

---

## 4. If the patch is too invasive

If maintaining a forked stryker-netx build is not an option for you, the
**second-best** alternative is a standalone Roslyn diagnostic console app
(~50 LoC) that we can supply on request. It opens the `.slnx` directly via
`MSBuildWorkspace.OpenSolutionAsync` and prints the same data points. It does
not exercise the exact stryker-netx code path so it cannot rule out
hypotheses involving the *order* in which projects are loaded, but it covers
H1/H6 well enough.

Just reply "send the standalone tool" and we'll attach it.

---

## 5. Privacy

The `[DIAG]` output contains absolute paths from your dev machine (drive
letters, user folder names). If those need redacting before you send them,
just replace the user/repo prefix with `<REPO>` consistently — the hypothesis
discrimination only depends on **whether** the strings differ and **how**
(case vs separator vs path-form), not on their absolute content.
