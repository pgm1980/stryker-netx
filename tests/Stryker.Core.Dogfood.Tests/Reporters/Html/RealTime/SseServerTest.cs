using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html.RealTime;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. SseServer tests spawn an actual HttpListener
/// with concurrent client connections (164 LOC). Defer to dedicated HTTP-server harness sprint
/// that mocks HttpListener or uses TestServer pattern.</summary>
public class SseServerTest
{
    [Fact(Skip = "Spawns real HttpListener with concurrent client connections — defer to HTTP-server harness sprint.")]
    public void SseServer_StartsAndAcceptsConnection() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void SseServer_BroadcastsEvents() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void SseServer_HandlesClientDisconnect() { /* placeholder */ }
}
