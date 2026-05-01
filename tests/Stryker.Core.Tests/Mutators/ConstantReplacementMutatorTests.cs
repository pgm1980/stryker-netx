using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ConstantReplacementMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<ConstantReplacementMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("42")]
    [InlineData("100L")]
    [InlineData("3.14")]
    [InlineData("2.5f")]
    public void ApplyMutations_OnNumericLiteral_EmitsMultipleMutations(string source)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void ApplyMutations_OnZeroLiteral_SkipsZeroAxis()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("0");
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
