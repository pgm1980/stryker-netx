using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>Sprint 94 (v2.80.0) defer-doc placeholder. CollectionExpressionMutatorTests upstream
/// uses a custom <c>[CollectionExpressionTest]</c> MSTest attribute providing rich source/expected
/// fixture data (524 LOC). Port requires translating that custom attribute to xUnit MemberData
/// and building a fixture-loader helper. Defer to dedicated CollectionExpressionMutator deep-port sprint.</summary>
public class CollectionExpressionMutatorTests
{
    [Fact(Skip = "Custom [CollectionExpressionTest] attribute drives 524 LOC of fixtures — defer to dedicated deep-port sprint with MemberData rewrite.")]
    public void ShouldBeMutationLevelAdvanced() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldAddValueToEmptyCollectionExpression() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldRemoveValuesFromCollectionExpression() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ShouldNotMutateCollectionExpressionsWithExplicitCast() { /* placeholder */ }
}
