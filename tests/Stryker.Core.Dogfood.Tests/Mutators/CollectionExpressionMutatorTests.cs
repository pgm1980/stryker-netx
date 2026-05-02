using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>Sprint 109 (v2.95.0) consolidated architectural-deferral. Upstream
/// CollectionExpressionMutatorTests (524 LOC) uses a custom <c>[CollectionExpressionTest]</c>
/// MSTest attribute that drives 4 [TestMethod]s with rich source/expected fixture data.
/// Re-port requires translating that custom attribute to xUnit MemberData and building a
/// fixture-loader helper. Belongs in dedicated CollectionExpressionMutator deep-port sprint.</summary>
public class CollectionExpressionMutatorTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: Custom [CollectionExpressionTest] MSTest attribute drives 524 LOC of fixture data. Re-port = MemberData rewrite + fixture-loader helper. Dedicated deep-port sprint required.")]
    public void CollectionExpressionMutator_ArchitecturalDeferral() { /* permanently skipped */ }
}
