using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html.RealTime;

/// <summary>Sprint 109 (v2.95.0) consolidated architectural-deferral. Upstream SseServerTest
/// spawns a real HttpListener with concurrent client connections (164 LOC). Re-port requires
/// either a TestServer pattern (in-memory HttpListener replacement) or careful port-allocation
/// + cleanup harness. Belongs in dedicated HTTP-server harness sprint.</summary>
public class SseServerTest
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: spawns real HttpListener with concurrent client connections. Re-port = TestServer pattern OR port-allocation harness. Dedicated HTTP-server harness sprint required.")]
    public void SseServer_ArchitecturalDeferral() { /* permanently skipped */ }
}
