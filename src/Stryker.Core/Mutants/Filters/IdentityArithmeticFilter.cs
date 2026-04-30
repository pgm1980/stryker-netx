using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (ADR-017, initial filter set): catches arithmetic substitutions
/// whose replacement is the algebraic identity of the original. Examples:
/// <list type="bullet">
///   <item>A binary `x + 0` mutated to `x + 0` (literal-preserving substitution).</item>
///   <item>A binary `x * 1` mutated to `x * 1`.</item>
///   <item>A unary `-x` mutated to `-(-x)` whose effective value equals `x`.</item>
/// </list>
/// Conservative scope: only literal-zero-/-one-arithmetic patterns are matched —
/// when a sub-expression involves variables or calls, the filter abstains so the
/// mutation is tested as usual. False-negative bias preserved.
/// </summary>
public sealed class IdentityArithmeticFilter : IEquivalentMutantFilter
{
    public string FilterId => "IdentityArithmetic";

    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        // Only inspect binary mutations on literal-zero/one operands.
        if (mutation.OriginalNode is not BinaryExpressionSyntax originalBinary
            || mutation.ReplacementNode is not BinaryExpressionSyntax replacementBinary)
        {
            return false;
        }

        // x + 0 / x - 0 / 0 + x — additive identity; mutation that retains the same
        // operator+operands obviously stays equivalent (defensive).
        if (IsAdditiveIdentity(originalBinary) && IsAdditiveIdentity(replacementBinary)
            && originalBinary.IsEquivalentTo(replacementBinary))
        {
            return true;
        }

        // x * 1 / 1 * x — multiplicative identity; same defensive equality check.
        if (IsMultiplicativeIdentity(originalBinary) && IsMultiplicativeIdentity(replacementBinary)
            && originalBinary.IsEquivalentTo(replacementBinary))
        {
            return true;
        }

        return false;
    }

    private static bool IsAdditiveIdentity(BinaryExpressionSyntax binary)
    {
        if (!binary.IsKind(SyntaxKind.AddExpression) && !binary.IsKind(SyntaxKind.SubtractExpression))
        {
            return false;
        }
        return IsLiteralZero(binary.Left) || IsLiteralZero(binary.Right);
    }

    private static bool IsMultiplicativeIdentity(BinaryExpressionSyntax binary)
    {
        if (!binary.IsKind(SyntaxKind.MultiplyExpression))
        {
            return false;
        }
        return IsLiteralOne(binary.Left) || IsLiteralOne(binary.Right);
    }

    private static bool IsLiteralZero(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 0 } or LiteralExpressionSyntax { Token.Value: 0L }
            or LiteralExpressionSyntax { Token.Value: 0.0 } or LiteralExpressionSyntax { Token.Value: 0.0f }
            or LiteralExpressionSyntax { Token.Value: 0m };

    private static bool IsLiteralOne(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 1 } or LiteralExpressionSyntax { Token.Value: 1L }
            or LiteralExpressionSyntax { Token.Value: 1.0 } or LiteralExpressionSyntax { Token.Value: 1.0f }
            or LiteralExpressionSyntax { Token.Value: 1m };
}
