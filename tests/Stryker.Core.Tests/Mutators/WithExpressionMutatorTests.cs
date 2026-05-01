using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class WithExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<WithExpressionMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnWithExpression_EmitsOneMutationPerInitializer()
    {
        var node = ParseExpression<WithExpressionSyntax>("rec with { A = 1, B = 2 }");
        var mutations = ApplyMutations<WithExpressionMutator, WithExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
    }

    [Fact]
    public void ApplyMutations_OnEmptyInitializer_ReturnsNoMutation()
    {
        var node = ParseExpression<WithExpressionSyntax>("rec with { }");
        AssertNoMutations(ApplyMutations<WithExpressionMutator, WithExpressionSyntax>(new(), node));
    }
}
