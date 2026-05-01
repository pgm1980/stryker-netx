using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class IdempotentBooleanFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsIdempotentBoolean()
        => new IdempotentBooleanFilter().FilterId.Should().Be("IdempotentBoolean");

    [Fact]
    public void IsEquivalent_OnDoubleNegationCollapse_ReturnsTrue()
    {
        var original = ParseExpression<PrefixUnaryExpressionSyntax>("!!x");
        var replacement = ParseExpression<IdentifierNameSyntax>("x");
        var mutation = BuildMutation(original, replacement);
        new IdempotentBooleanFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnLogicalIdentityPreserved_ReturnsTrue()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x && true");
        var mutation = BuildMutation(node, node);
        new IdempotentBooleanFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnRealBoolMutation_ReturnsFalse()
    {
        var original = ParseExpression<LiteralExpressionSyntax>("true");
        var replacement = ParseExpression<LiteralExpressionSyntax>("false");
        var mutation = BuildMutation(original, replacement);
        new IdempotentBooleanFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnNonBooleanNodes_ReturnsFalse()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("42");
        var mutation = BuildMutation(node, node);
        new IdempotentBooleanFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }
}
