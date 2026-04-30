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
