using System;
using System.Collections.Generic;
using System.Threading;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Caches Stryker runs by argument-tuple key so multiple <c>[Fact]</c>s can
/// assert different aspects of the same run without each paying a fresh ~25 s
/// wall-clock cost. Used as a shared collection-level fixture via
/// <see cref="E2ETestCollection"/>.
///
/// Sprint 22 (v2.9.0): per-profile caching enabled — Sprint 21's deferred D5
/// design note is now closed. <c>--mutation-profile</c> is wired through the
/// CLI and JSON config. Profile comparisons are exercised at
/// <c>--mutation-level Advanced</c> because the additional Stronger/All-only
/// operators added in Sprints 9–14 are predominantly <c>Advanced</c> or
/// <c>Complete</c> level — at the default <c>Standard</c> level the three
/// profiles are indistinguishable on Sample.Library, so the comparison would
/// be vacuous. <c>Complete</c>-level + <c>All</c>-profile currently triggers a
/// pre-existing <c>InvalidCastException</c> in one of the v2.x operators
/// (<c>VisitQualifiedName</c> rewriter); that bug is unrelated to Sprint 22
/// and is roadmapped for a follow-up sprint.
/// </summary>
public sealed class StrykerRunCacheFixture
{
    private readonly Dictionary<string, StrykerRunResult> _runs = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <summary>
    /// Retrieves a cached Stryker run for the supplied argument set. The
    /// argument tuple becomes the cache key — callers must pass the same
    /// arguments to get the same cached run.
    /// </summary>
    public StrykerRunResult GetRun(params string[] extraArgs)
    {
        var key = string.Join(' ', extraArgs);
        lock (_gate)
        {
            if (_runs.TryGetValue(key, out var existing))
            {
                return existing;
            }
            var result = ProcessSpawnHelper.RunStrykerAgainstSample(extraArgs);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Stryker run with args '{key}' exited {result.ExitCode}.\n"
                    + $"StdOut:\n{result.StdOut}\n\nStdErr:\n{result.StdErr}");
            }
            _runs[key] = result;
            return result;
        }
    }

    /// <summary>The canonical default-profile run: --reporter json so the JsonReporter is asserted on.</summary>
    public StrykerRunResult GetDefaultsRunWithJsonReporter() => GetRun("--reporter", "json");

    /// <summary>A multi-reporter run that asserts both HTML and JSON outputs land in the same StrykerOutput dir.</summary>
    public StrykerRunResult GetDefaultsRunWithJsonAndHtmlReporters() => GetRun("--reporter", "json", "--reporter", "html");

    /// <summary>
    /// Sprint 22: Defaults profile at Advanced level — the comparison baseline
    /// for the three profile-comparison Facts in <c>SampleE2EProfileTests</c>.
    /// Level=Advanced is required because at the default Standard level all
    /// three profiles produce the same 5 mutants on Sample.Library.
    /// </summary>
    public StrykerRunResult GetDefaultsRunAtAdvancedLevel()
        => GetRun("--mutation-profile", "Defaults", "--mutation-level", "Advanced", "--reporter", "json");

    /// <summary>Sprint 22: Stronger profile at Advanced level. Includes Stronger-only operators (AOD, RorMatrix, InlineConstants, …).</summary>
    public StrykerRunResult GetStrongerRunAtAdvancedLevel()
        => GetRun("--mutation-profile", "Stronger", "--mutation-level", "Advanced", "--reporter", "json");

    /// <summary>Sprint 22: All profile at Advanced level. Includes Stronger-only and All-only operators (provided they are Advanced or below).</summary>
    public StrykerRunResult GetAllRunAtAdvancedLevel()
        => GetRun("--mutation-profile", "All", "--mutation-level", "Advanced", "--reporter", "json");
}
