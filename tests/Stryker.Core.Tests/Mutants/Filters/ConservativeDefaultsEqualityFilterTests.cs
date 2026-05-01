using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class ConservativeDefaultsEqualityFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsConservativeDefaultsEquality()
        => new ConservativeDefaultsEqualityFilter().FilterId.Should().Be("ConservativeDefaultsEquality");

    [Fact]
    public void IsEquivalent_OnNullSemanticModel_ReturnsFalse()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x == 0");
        var mutation = BuildMutation(node, node);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnUnsignedEqualsZeroMutatedToLessThan_ReturnsTrue()
    {
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(uint x) { var b = x == 0; } }");
        // Build a synthetic replacement: x < 0
        var replacement = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.BinaryExpression(
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanExpression,
            original.Left, original.Right);
        var mutation = BuildMutation(original, replacement);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, model).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnSignedEqualsZeroMutated_ReturnsFalse()
    {
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int x) { var b = x == 0; } }");
        var replacement = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.BinaryExpression(
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanExpression,
            original.Left, original.Right);
        var mutation = BuildMutation(original, replacement);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, model).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnNonBinaryNodes_ReturnsFalse()
    {
        var (model, expr) = BuildSemanticContext<LiteralExpressionSyntax>(
            "class C { void M() { var x = 42; } }");
        var mutation = BuildMutation(expr, expr);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, model).Should().BeFalse();
    }
}
