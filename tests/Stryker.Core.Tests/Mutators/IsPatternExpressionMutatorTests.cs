using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class IsPatternExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<IsPatternExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnIsPattern_EmitsAtLeastOneMutation()
    {
        var node = ParseExpression<IsPatternExpressionSyntax>("x is int n");
        var mutations = ApplyMutations<IsPatternExpressionMutator, IsPatternExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }
}
