using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 10, comparison.md §4.1 — PIT ROR full matrix): for each
/// relational operator, emit ALL 5 alternative replacements (vs Stryker.NET
/// v1.x's single boundary swap + single negation). Catches subtle off-by-one
/// and predicate-direction bugs that the v1.x boundary mutator misses.
///
/// ROR matrix (per OPL standard, see PIT ROR docs):
///   &lt;  -> &lt;=, &gt;, &gt;=, ==, !=
///   &lt;= -> &lt;, &gt;, &gt;=, ==, !=
///   &gt;  -> &lt;, &lt;=, &gt;=, ==, !=
///   &gt;= -> &lt;, &lt;=, &gt;, ==, !=
///   == -> &lt;, &lt;=, &gt;, &gt;=, !=
///   != -> &lt;, &lt;=, &gt;, &gt;=, ==
///
/// 5 mutations per relational site is HUGE on real code; profile is
/// Stronger | All only — opt-in. The existing single-swap mutators
/// (BinaryExpressionMutator's equality + boundary substitutions) cover
/// the most-likely cases at Defaults.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class RorMatrixMutator : MutatorBase<BinaryExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    private static readonly Dictionary<SyntaxKind, (string Op, SyntaxKind Token)[]> RorMatrix = new()
    {
        [SyntaxKind.LessThanExpression] =
        [
            ("<=", SyntaxKind.LessThanEqualsToken),
            (">", SyntaxKind.GreaterThanToken),
            (">=", SyntaxKind.GreaterThanEqualsToken),
            ("==", SyntaxKind.EqualsEqualsToken),
            ("!=", SyntaxKind.ExclamationEqualsToken),
        ],
        [SyntaxKind.LessThanOrEqualExpression] =
        [
            ("<", SyntaxKind.LessThanToken),
            (">", SyntaxKind.GreaterThanToken),
            (">=", SyntaxKind.GreaterThanEqualsToken),
            ("==", SyntaxKind.EqualsEqualsToken),
            ("!=", SyntaxKind.ExclamationEqualsToken),
        ],
        [SyntaxKind.GreaterThanExpression] =
        [
            ("<", SyntaxKind.LessThanToken),
            ("<=", SyntaxKind.LessThanEqualsToken),
            (">=", SyntaxKind.GreaterThanEqualsToken),
            ("==", SyntaxKind.EqualsEqualsToken),
            ("!=", SyntaxKind.ExclamationEqualsToken),
        ],
        [SyntaxKind.GreaterThanOrEqualExpression] =
        [
            ("<", SyntaxKind.LessThanToken),
            ("<=", SyntaxKind.LessThanEqualsToken),
            (">", SyntaxKind.GreaterThanToken),
            ("==", SyntaxKind.EqualsEqualsToken),
            ("!=", SyntaxKind.ExclamationEqualsToken),
        ],
        [SyntaxKind.EqualsExpression] =
        [
            ("<", SyntaxKind.LessThanToken),
            ("<=", SyntaxKind.LessThanEqualsToken),
            (">", SyntaxKind.GreaterThanToken),
            (">=", SyntaxKind.GreaterThanEqualsToken),
            ("!=", SyntaxKind.ExclamationEqualsToken),
        ],
        [SyntaxKind.NotEqualsExpression] =
        [
            ("<", SyntaxKind.LessThanToken),
            ("<=", SyntaxKind.LessThanEqualsToken),
            (">", SyntaxKind.GreaterThanToken),
            (">=", SyntaxKind.GreaterThanEqualsToken),
            ("==", SyntaxKind.EqualsEqualsToken),
        ],
    };

    public override IEnumerable<Mutation> ApplyMutations(BinaryExpressionSyntax node, SemanticModel semanticModel)
    {
        if (!RorMatrix.TryGetValue(node.Kind(), out var alternatives))
        {
            yield break;
        }

        foreach (var (opText, tokenKind) in alternatives)
        {
            var newKind = MapTokenToBinaryKind(tokenKind);
            // Sprint 184 on issue 279, finding F-06: ordering replacements on operands
            // without an ordering — reference types and bool — are guaranteed CS0019; the
            // probe showed the full matrix firing on every null check. Equality swaps stay
            // for all types, and unknown types keep the historical behaviour.
            if (IsOrderingKind(newKind) && OperandsKnownNotOrdered(node, semanticModel))
            {
                continue;
            }

            var newToken = SyntaxFactory.Token(tokenKind);
            var replacement = SyntaxFactory.BinaryExpression(newKind, node.Left, newToken, node.Right);
            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = replacement.WithCleanTriviaFrom(node),
                DisplayName = $"ROR matrix: '{node.OperatorToken.Text}' -> '{opText}'",
                Type = Mutator.Equality,
            };
        }
    }

private static bool IsOrderingKind(SyntaxKind kind) =>
        kind is SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression
            or SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression;

    private static bool OperandsKnownNotOrdered(BinaryExpressionSyntax node, SemanticModel? semanticModel)
    {
        if (semanticModel is null)
        {
            return false;
        }

        ITypeSymbol? type;
        try
        {
            type = semanticModel.GetTypeInfo(node.Left).Type ?? semanticModel.GetTypeInfo(node.Right).Type;
        }
        catch (ArgumentException)
        {
            // node does not belong to this model's tree — abstain, mutate as before
            return false;
        }

        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return false;
        }

        // lifted operators: int? < int? is legal — judge the underlying type
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && type is INamedTypeSymbol { TypeArguments.Length: 1 } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        var ordered = type.TypeKind is TypeKind.Enum or TypeKind.Pointer
            || type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Decimal or SpecialType.System_Char
            || !type.GetMembers("op_LessThan").IsEmpty;
        return !ordered;
    }

    private static SyntaxKind MapTokenToBinaryKind(SyntaxKind tokenKind) => tokenKind switch
    {
        SyntaxKind.LessThanToken => SyntaxKind.LessThanExpression,
        SyntaxKind.LessThanEqualsToken => SyntaxKind.LessThanOrEqualExpression,
        SyntaxKind.GreaterThanToken => SyntaxKind.GreaterThanExpression,
        SyntaxKind.GreaterThanEqualsToken => SyntaxKind.GreaterThanOrEqualExpression,
        SyntaxKind.EqualsEqualsToken => SyntaxKind.EqualsExpression,
        SyntaxKind.ExclamationEqualsToken => SyntaxKind.NotEqualsExpression,
        _ => SyntaxKind.None,
    };
}
