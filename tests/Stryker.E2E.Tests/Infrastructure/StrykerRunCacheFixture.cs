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
/// Sprint 21 design note (D5): the original plan had per-MutationProfile
/// caching, but v2.7.0 does not expose <c>--mutation-profile</c> on the CLI
/// or in the FileBasedInput JSON config — only the in-process StrykerOptions
/// carries it. Profile-comparison E2E tests are therefore deferred to v2.9.0+
/// (after the CLI/config surface for MutationProfile lands). Today the cache
/// keys runs by the <c>--reporter</c> flag set the test cares about.
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
}
