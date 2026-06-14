using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (Sprint 9, ADR-017 + cargo-mutants §4.2 "Konservative Defaults"):
/// catches relational mutations that would obviously evaluate-to-the-same-result
/// for unsigned numeric types. Concretely: a mutation from <c>x == 0</c> to
/// <c>x &lt; 0</c> on a <c>uint</c>/<c>byte</c>/<c>ushort</c>/<c>ulong</c>
/// expression is equivalent because unsigned types cannot be less than zero —
/// the mutated comparison has the same semantic as <c>false</c> + the original
/// has the same semantic for <c>x != 0</c>, but the test outcome doesn't move.
///
/// cargo-mutants enforces this conservative default by simply NOT mutating
/// <c>==</c>/<c>!=</c> to <c>&lt;</c>/<c>&gt;</c> on unsigned operands. Stryker.NET
/// applies the mutation regardless and produces survivors. We close the gap
/// by treating those mutants as equivalent at filter time — they're truly
/// untestable and the false-survivor lowers the mutation score artificially.
///
/// Conservative scope: only the literal-zero comparison case is matched
/// (<c>uint x; x == 0</c> mutated to <c>x &lt; 0</c>). Variable comparisons
/// (<c>x &lt; y</c>) cause the filter to abstain — they MAY be testable
/// depending on runtime values.
/// </summary>
public sealed class ConservativeDefaultsEqualityFilter : IEquivalentMutantFilter
{
    public string FilterId => "ConservativeDefaultsEquality";

    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        if (semanticModel is null)
        {
            return false;
        }

        // Only relevant when mutating from equality/inequality to ordered comparison.
        if (mutation.OriginalNode is not BinaryExpressionSyntax original
            || mutation.ReplacementNode is not BinaryExpressionSyntax replacement)
        {
            return false;
        }

        var originalKind = original.Kind();
        var replacementKind = replacement.Kind();

        var isEqualityOriginal = originalKind is SyntaxKind.EqualsExpression
            or SyntaxKind.NotEqualsExpression;
        var isOrderedReplacement = replacementKind is SyntaxKind.LessThanExpression
            or SyntaxKind.GreaterThanExpression
            or SyntaxKind.LessThanOrEqualExpression
            or SyntaxKind.GreaterThanOrEqualExpression;

        if (!isEqualityOriginal || !isOrderedReplacement)
        {
            return false;
        }

        // Match the literal-zero case — `unsigned X 0` or `0 X unsigned`.
        var (variableSide, otherSide) = OperandsOrderedByVariable(original);
        if (variableSide is null || !IsLiteralZero(otherSide))
        {
            return false;
        }

        var variableType = semanticModel.GetTypeInfo(variableSide).Type;
        if (!IsUnsignedNumeric(variableType))
        {
            return false;
        }

        // EQF-003 (ADR-059): of the eight equality-to-ordered replacements for an unsigned operand
        // versus literal zero, only two are genuinely equivalent, and which two depends on operand
        // order. Normalise so the variable is conceptually on the left (flip the replacement direction
        // when the literal is on the left), then only equals-to-less-or-equal and not-equals-to-greater
        // are equivalent; the other six are killable. The old filter flagged all eight unconditionally,
        // silently dropping killable mutants (the same hidden-test-gap harm class as EQF-001).
        var variableIsLeft = original.Left is not LiteralExpressionSyntax;
        var normalizedReplacementKind = variableIsLeft ? replacementKind : FlipComparison(replacementKind);
        return (originalKind, normalizedReplacementKind) switch
        {
            (SyntaxKind.EqualsExpression, SyntaxKind.LessThanOrEqualExpression) => true,
            (SyntaxKind.NotEqualsExpression, SyntaxKind.GreaterThanExpression) => true,
            _ => false,
        };
    }

    private static (ExpressionSyntax? Variable, ExpressionSyntax Other) OperandsOrderedByVariable(BinaryExpressionSyntax binary)
    {
        if (binary.Left is LiteralExpressionSyntax)
        {
            return (binary.Right, binary.Left);
        }
        if (binary.Right is LiteralExpressionSyntax)
        {
            return (binary.Left, binary.Right);
        }
        return (null, binary.Right);
    }

    private static bool IsLiteralZero(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 0 } or LiteralExpressionSyntax { Token.Value: 0L }
            or LiteralExpressionSyntax { Token.Value: (uint)0 } or LiteralExpressionSyntax { Token.Value: (ulong)0 }
            or LiteralExpressionSyntax { Token.Value: (byte)0 } or LiteralExpressionSyntax { Token.Value: (ushort)0 };

    private static bool IsUnsignedNumeric(ITypeSymbol? type) =>
        type?.SpecialType is SpecialType.System_Byte
            or SpecialType.System_UInt16
            or SpecialType.System_UInt32
            or SpecialType.System_UInt64;

    // Mirrors an ordered comparison operator for the case where the literal is the left operand,
    // so the equivalence check can reason as if the variable were always on the left.
    private static SyntaxKind FlipComparison(SyntaxKind kind) => kind switch
    {
        SyntaxKind.LessThanExpression => SyntaxKind.GreaterThanExpression,
        SyntaxKind.GreaterThanExpression => SyntaxKind.LessThanExpression,
        SyntaxKind.LessThanOrEqualExpression => SyntaxKind.GreaterThanOrEqualExpression,
        SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanOrEqualExpression,
        _ => kind,
    };
}
