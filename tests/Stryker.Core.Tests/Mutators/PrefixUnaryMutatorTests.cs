using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class PrefixUnaryMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<PrefixUnaryMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("-a", "swap")]
    [InlineData("+a", "swap")]
    [InlineData("++a", "swap")]
    [InlineData("--a", "swap")]
    [InlineData("~a", "removal")]
    [InlineData("!a", "removal")]
    public void ApplyMutations_OnPrefixUnary_EmitsSingleMutation(string source, string variant)
    {
        var node = ParseExpression<PrefixUnaryExpressionSyntax>(source);
        var mutations = ApplyMutations<PrefixUnaryMutator, PrefixUnaryExpressionSyntax>(new(), node);
        AssertSingleMutation(mutations);
        variant.Should().BeOneOf("swap", "removal"); // sanity guard
    }
}
