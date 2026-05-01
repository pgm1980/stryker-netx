using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class InlineConstantsMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<InlineConstantsMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("42")]
    [InlineData("100L")]
    [InlineData("3.14")]
    [InlineData("2.5f")]
    public void ApplyMutations_OnNumericLiteral_EmitsTwoMutations(string source)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
    }

    [Fact]
    public void ApplyMutations_OnNonNumericLiteral_ReturnsNoMutation()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("\"hello\"");
        AssertNoMutations(ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node));
    }
}
