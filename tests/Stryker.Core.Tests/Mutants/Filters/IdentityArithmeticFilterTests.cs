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

    // EQF-002 (external 360 test): a genuine arithmetic-identity equivalent changes the operator while
    // the value stays the same. Going from plus-zero to minus-zero, or from times-one to over-one, both
    // still equal the original value, so they must be flagged as equivalent. The old IsEquivalentTo
    // guard was purely syntactic and only matched a no-op, so the filter never fired and these
    // equivalents survived (artificially lowering the score). Equivalence needs the identity literal as
    // the RIGHT operand and the same left operand; the left-literal forms (zero minus the variable is a
    // negation, one over the variable is a reciprocal) are NOT identities and must stay killable.
    [Theory]
    [InlineData("x + 0", "x - 0", true)]
    [InlineData("x - 0", "x + 0", true)]
    [InlineData("x * 1", "x / 1", true)]
    [InlineData("x / 1", "x * 1", true)]
    [InlineData("x + 0", "x + 0", true)]
    [InlineData("x * 1", "x * 1", true)]
    [InlineData("0 + x", "0 - x", false)]
    [InlineData("1 * x", "1 / x", false)]
    [InlineData("x + y", "x - y", false)]
    [InlineData("x + 0", "x * 0", false)]
    public void IsEquivalent_FlagsOnlyRightIdentityOperatorFlips(string originalExpr, string replacementExpr, bool expected)
    {
        var original = ParseExpression<BinaryExpressionSyntax>(originalExpr);
        var replacement = ParseExpression<BinaryExpressionSyntax>(replacementExpr);
        var mutation = BuildMutation(original, replacement);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().Be(expected);
    }

    [Fact]
    public void IsEquivalent_OnNonBinaryNodes_ReturnsFalse()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("42");
        var mutation = BuildMutation(node, node);
        new IdentityArithmeticFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }
}
