using System.Collections.Generic;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;

namespace Stryker.Core.Dogfood.Tests.Reporters.Json;

/// <summary>Sprint 100 (v2.86.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Reporters/Json/MockJsonReport.cs.
/// Test-only stub used by DashboardClientsTest to construct a JsonReport
/// without invoking the full JsonReport.Build pipeline.</summary>
public class MockJsonReport : JsonReport
{
    public MockJsonReport(
        IDictionary<string, int>? thresholds,
        IDictionary<string, ISourceFile>? files)
    {
        if (thresholds is not null)
        {
            Thresholds = thresholds;
        }
        if (files is not null)
        {
            Files = files;
        }
    }
}
