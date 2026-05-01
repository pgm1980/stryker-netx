using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class PostfixUnaryMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<PostfixUnaryMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("a++")]
    [InlineData("a--")]
    public void ApplyMutations_OnPostfixIncrementDecrement_EmitsAtLeastOneMutation(string source)
    {
        var node = ParseExpression<PostfixUnaryExpressionSyntax>(source);
        var mutations = ApplyMutations<PostfixUnaryMutator, PostfixUnaryExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }
}
