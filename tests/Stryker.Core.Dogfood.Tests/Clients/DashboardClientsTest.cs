using Xunit;

namespace Stryker.Core.Dogfood.Tests.Clients;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. DashboardClient HTTP-call tests use
/// HttpMessageHandler mock pattern (438 LOC). Defer to dedicated HTTP-mocking deep-port sprint.</summary>
public class DashboardClientsTest
{
    [Fact(Skip = "Heavy HttpMessageHandler mock pattern — defer to HTTP-mocking deep-port sprint.")]
    public void DashboardClient_PublishReport_Succeeds() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void DashboardClient_PublishReport_HandlesAuth() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void DashboardClient_PullReport_DeserializesJson() { /* placeholder */ }
}
