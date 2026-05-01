using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class CollectionExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<CollectionExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnNonEmptyCollectionExpression_EmitsMutation()
    {
        var node = ParseExpression<CollectionExpressionSyntax>("[1, 2, 3]");
        var mutations = ApplyMutations<CollectionExpressionMutator, CollectionExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }
}
