using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Mock counterpart for the actual <c>MutantControl</c> class that the production
/// build injects into the mutated assembly. <see cref="Stryker.DataCollector.CoverageCollector"/>
/// talks to this type by name via reflection, so the public-state surface MUST
/// mirror upstream's MutantControl: public static fields (not properties).
/// Without this binding the collector's coverage and mutant-selection paths
/// fall back to no-op behaviour and several tests in
/// <see cref="CoverageCollectorTests"/> would silently change semantics.
///
/// Sprint 27 (v2.14.0) port of upstream stryker-net 4.14.0 inline class from
/// src/Stryker.TestRunner.VsTest.UnitTest/CoverageCollectorTests.cs.
/// </summary>
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields",
    Justification = "Reflection target shape must mirror upstream MutantControl public-field surface; properties would break the binding.")]
[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible",
    Justification = "Reflection target shape must mirror upstream MutantControl public-field surface.")]
[SuppressMessage("Design", "MA0069:Non-constant static fields should not be visible",
    Justification = "Reflection target shape must mirror upstream MutantControl public-field surface.")]
public static class MutantControl
{
    public static bool CaptureCoverage;
    public static int ActiveMutant = -1;
    private static List<int>[] coverageData = [[], []];

    public static IList<int>[] GetCoverageData()
    {
        var result = coverageData;
        ClearCoverageInfo();
        return result;
    }

    public static void ClearCoverageInfo() => coverageData = [[], []];

    public static void HitNormal(int mutation) => coverageData[0].Add(mutation);

    public static void HitStatic(int mutation) => coverageData[1].Add(mutation);
}
