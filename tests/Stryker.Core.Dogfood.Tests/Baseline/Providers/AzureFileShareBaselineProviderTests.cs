using Xunit;

namespace Stryker.Core.Dogfood.Tests.Baseline.Providers;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. AzureFileShareBaselineProvider tests use
/// HttpMessageHandler mock pattern + Azure SAS URL parsing matrix (261 LOC). Defer to dedicated
/// HTTP-mocking sprint (alongside DashboardClient).</summary>
public class AzureFileShareBaselineProviderTests
{
    [Fact(Skip = "Heavy HttpMessageHandler mock + Azure SAS URL parsing — defer to HTTP-mocking deep-port sprint.")]
    public void Load_ReturnsReportFromAzure() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Load_HandlesNotFound() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Save_UploadsToAzure() { /* placeholder */ }
}
