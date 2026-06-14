using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
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

    // EQF-003 (external 360 test): for an unsigned operand compared to the literal zero, only two of the
    // eight equality-to-ordered mutations are genuinely equivalent, and which two depends on operand
    // order (because zero-at-or-below the variable is always true, but the variable at-or-below zero
    // holds only when it equals zero). With the variable on the left, equals-zero to less-or-equal-zero
    // and not-equals-zero to greater-than-zero are equivalent. With the variable on the right the pair
    // flips: equals to greater-or-equal and not-equals to less-than. Every other combination is killable
    // and must NOT be flagged as equivalent (that is the EQF-003 bug: the old filter flagged all eight).
    [Theory]
    // variable on the left, the two equivalent forms
    [InlineData("x == 0", SyntaxKind.LessThanOrEqualExpression, true)]
    [InlineData("x != 0", SyntaxKind.GreaterThanExpression, true)]
    // variable on the left, killable forms
    [InlineData("x == 0", SyntaxKind.LessThanExpression, false)]
    [InlineData("x == 0", SyntaxKind.GreaterThanExpression, false)]
    [InlineData("x == 0", SyntaxKind.GreaterThanOrEqualExpression, false)]
    [InlineData("x != 0", SyntaxKind.LessThanExpression, false)]
    [InlineData("x != 0", SyntaxKind.LessThanOrEqualExpression, false)]
    [InlineData("x != 0", SyntaxKind.GreaterThanOrEqualExpression, false)]
    // variable on the right, the two equivalent forms (flipped direction)
    [InlineData("0 == x", SyntaxKind.GreaterThanOrEqualExpression, true)]
    [InlineData("0 != x", SyntaxKind.LessThanExpression, true)]
    // variable on the right, killable forms
    [InlineData("0 == x", SyntaxKind.LessThanOrEqualExpression, false)]
    [InlineData("0 != x", SyntaxKind.GreaterThanExpression, false)]
    public void IsEquivalent_UnsignedZeroComparison_FlagsOnlyTheTwoEquivalentForms(
        string originalExpr, SyntaxKind replacementKind, bool expected)
    {
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            $"class C {{ void M(uint x) {{ var b = {originalExpr}; }} }}");
        var replacement = SyntaxFactory.BinaryExpression(replacementKind, original.Left, original.Right);
        var mutation = BuildMutation(original, replacement);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, model).Should().Be(expected);
    }

    [Fact]
    public void IsEquivalent_OnSignedEqualsZeroMutated_ReturnsFalse()
    {
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int x) { var b = x == 0; } }");
        var replacement = SyntaxFactory.BinaryExpression(
            SyntaxKind.LessThanOrEqualExpression, original.Left, original.Right);
        var mutation = BuildMutation(original, replacement);
        new ConservativeDefaultsEqualityFilter().IsEquivalent(mutation, model).Should().BeFalse(
            "the filter is unsigned-only; a signed operand never yields an always-true ordered comparison");
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
