using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class IdentityArithmeticFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsIdentityArithmetic()
        => new IdentityArithmeticFilter().FilterId.Should().Be("IdentityArithmetic");

    [Fact]
    public void IsEquivalent_OnAdditiveIdentityPreserved_ReturnsTrue()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x + 0");
        var mutation = BuildMutation(node, node);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnMultiplicativeIdentityPreserved_ReturnsTrue()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x * 1");
        var mutation = BuildMutation(node, node);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnRealMutation_ReturnsFalse()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("x + 0");
        var replacement = ParseExpression<BinaryExpressionSyntax>("x - 0");
        var mutation = BuildMutation(original, replacement);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnNonBinaryNodes_ReturnsFalse()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("42");
        var mutation = BuildMutation(node, node);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnNonIdentityArithmetic_ReturnsFalse()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("x + y");
        var replacement = ParseExpression<BinaryExpressionSyntax>("x - y");
        var mutation = BuildMutation(original, replacement);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }
}
