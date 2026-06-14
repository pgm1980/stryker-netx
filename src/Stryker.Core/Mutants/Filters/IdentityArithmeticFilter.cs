using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (ADR-017, initial filter set): catches arithmetic substitutions whose replacement is the
/// algebraic identity of the original — the value is unchanged, only the operator flips. Examples
/// (EQF-002 / ADR-059):
/// <list type="bullet">
///   <item>`x + 0` mutated to `x - 0` — both equal `x`.</item>
///   <item>`x * 1` mutated to `x / 1` — both equal `x`.</item>
/// </list>
/// Equivalence requires the identity literal (zero for additive, one for multiplicative) to be the
/// RIGHT operand of both expressions and the left operand to be unchanged. Left-literal forms are
/// excluded because they are not identities (`0 - x` is a negation, `1 / x` is a reciprocal).
/// Conservative scope: only literal-zero/-one patterns match; anything else abstains so the mutant
/// is tested as usual. False-negative bias preserved.
/// </summary>
public sealed class IdentityArithmeticFilter : IEquivalentMutantFilter
{
    public string FilterId => "IdentityArithmetic";

    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        // Only inspect binary mutations on literal-zero/-one operands.
        if (mutation.OriginalNode is not BinaryExpressionSyntax originalBinary
            || mutation.ReplacementNode is not BinaryExpressionSyntax replacementBinary)
        {
            return false;
        }

        // EQF-002 (ADR-059): a real arithmetic-identity equivalent flips the operator while leaving the
        // value untouched (plus-zero becomes minus-zero, or times-one becomes over-one, both still equal
        // the original value). The previous IsEquivalentTo guard was syntactic and only matched a no-op,
        // so the filter never fired and these equivalents survived, lowering the score. Match by
        // structure instead: same left operand, the identity literal as the RIGHT operand of both, and
        // both operators drawn from the same identity group (add and subtract for zero, multiply and
        // divide for one).
        return IsRightIdentityEquivalent(originalBinary, replacementBinary, additive: true)
            || IsRightIdentityEquivalent(originalBinary, replacementBinary, additive: false);
    }

    private static bool IsRightIdentityEquivalent(BinaryExpressionSyntax original, BinaryExpressionSyntax replacement, bool additive) =>
        IsInIdentityGroup(original, additive)
            && IsInIdentityGroup(replacement, additive)
            && IsIdentityLiteral(original.Right, additive)
            && IsIdentityLiteral(replacement.Right, additive)
            && original.Left.IsEquivalentTo(replacement.Left);

    private static bool IsInIdentityGroup(BinaryExpressionSyntax binary, bool additive) => additive
        ? binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.SubtractExpression)
        : binary.IsKind(SyntaxKind.MultiplyExpression) || binary.IsKind(SyntaxKind.DivideExpression);

    private static bool IsIdentityLiteral(ExpressionSyntax expression, bool additive) => additive
        ? IsLiteralZero(expression)
        : IsLiteralOne(expression);

    private static bool IsLiteralZero(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 0 } or LiteralExpressionSyntax { Token.Value: 0L }
            or LiteralExpressionSyntax { Token.Value: 0.0 } or LiteralExpressionSyntax { Token.Value: 0.0f }
            or LiteralExpressionSyntax { Token.Value: 0m };

    private static bool IsLiteralOne(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 1 } or LiteralExpressionSyntax { Token.Value: 1L }
            or LiteralExpressionSyntax { Token.Value: 1.0 } or LiteralExpressionSyntax { Token.Value: 1.0f }
            or LiteralExpressionSyntax { Token.Value: 1m };
}
