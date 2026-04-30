using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (ADR-017, initial filter set): catches boolean mutations that
/// reduce to the original under standard logical-identity laws. Examples:
/// <list type="bullet">
///   <item><c>!!x</c> mutated to <c>x</c> (or vice versa) — double negation.</item>
///   <item><c>x &amp;&amp; true</c> mutated to <c>x &amp;&amp; true</c> — preserved tautology.</item>
///   <item><c>x || false</c> mutated to <c>x || false</c> — preserved contradiction.</item>
/// </list>
/// Conservative scope as per <see cref="IEquivalentMutantFilter"/> contract:
/// only literal-true/false patterns are matched — variable operands cause the
/// filter to abstain, preserving the false-negative bias.
/// </summary>
public sealed class IdempotentBooleanFilter : IEquivalentMutantFilter
{
    public string FilterId => "IdempotentBoolean";

    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        // Detect double-negation collapse: the original is `!!x`, the replacement is `x`.
        if (IsDoubleNegation(mutation.OriginalNode) && AreSameInnerOperand(mutation.OriginalNode, mutation.ReplacementNode))
        {
            return true;
        }
        if (IsDoubleNegation(mutation.ReplacementNode) && AreSameInnerOperand(mutation.ReplacementNode, mutation.OriginalNode))
        {
            return true;
        }

        // Detect short-circuit tautology/contradiction preservation:
        //   x && true (original) ↔ x && true (replacement) — both kept literal-true RHS.
        //   x || false (original) ↔ x || false (replacement) — both kept literal-false RHS.
        if (mutation.OriginalNode is BinaryExpressionSyntax originalBinary
            && mutation.ReplacementNode is BinaryExpressionSyntax replacementBinary
            && IsLogicalIdentityPattern(originalBinary)
            && IsLogicalIdentityPattern(replacementBinary)
            && originalBinary.IsEquivalentTo(replacementBinary))
        {
            return true;
        }

        return false;
    }

    private static bool IsDoubleNegation(SyntaxNode node) =>
        node is PrefixUnaryExpressionSyntax outer
            && outer.IsKind(SyntaxKind.LogicalNotExpression)
            && outer.Operand is PrefixUnaryExpressionSyntax { } inner
            && inner.IsKind(SyntaxKind.LogicalNotExpression);

    private static bool AreSameInnerOperand(SyntaxNode doubleNegated, SyntaxNode replacement)
    {
        if (doubleNegated is PrefixUnaryExpressionSyntax outer
            && outer.Operand is PrefixUnaryExpressionSyntax inner)
        {
            return inner.Operand.IsEquivalentTo(replacement);
        }
        return false;
    }

    private static bool IsLogicalIdentityPattern(BinaryExpressionSyntax binary)
    {
        // x && true | true && x  → tautology RHS makes it equivalent to x.
        if (binary.IsKind(SyntaxKind.LogicalAndExpression))
        {
            return IsLiteralTrue(binary.Left) || IsLiteralTrue(binary.Right);
        }
        // x || false | false || x → contradiction RHS makes it equivalent to x.
        if (binary.IsKind(SyntaxKind.LogicalOrExpression))
        {
            return IsLiteralFalse(binary.Left) || IsLiteralFalse(binary.Right);
        }
        return false;
    }

    private static bool IsLiteralTrue(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.TrueLiteralExpression);

    private static bool IsLiteralFalse(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.FalseLiteralExpression);
}
