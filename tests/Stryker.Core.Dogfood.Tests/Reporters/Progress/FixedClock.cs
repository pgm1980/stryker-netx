using Stryker.Core.Reporters.Progress;
using Stryker.TestHelpers;

namespace Stryker.Core.Dogfood.Tests.Reporters.Progress;

/// <summary>Sprint 86 (v2.72.0). Fixed clock returning 10ms — used by ProgressBarReporterTests.</summary>
internal sealed class FixedClock : TestBase, IStopWatchProvider
{
    public void Start() { /* fixed clock — no-op */ }

    public void Stop() { /* fixed clock — no-op */ }

    public long GetElapsedMillisecond() => 10L;
}
