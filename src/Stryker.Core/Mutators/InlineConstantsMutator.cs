using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 10, comparison.md §4.1 — PIT INLINE_CONSTS):
/// numeric-literal mutation. Stryker.NET v1.x has NO mutator for numeric
/// literals (only string + boolean literals); PIT and mutmut both do.
/// Closes that gap by emitting two variants per integer/float literal:
/// <c>n + 1</c> and <c>n - 1</c>. Detects "off-by-one" tests directly.
///
/// Profile membership: Stronger | All (NOT Defaults). Numeric mutations
/// are noisy on under-tested code — opt-in only.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class InlineConstantsMutator : MutatorBase<LiteralExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(LiteralExpressionSyntax node, SemanticModel semanticModel)
    {
        if (!node.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            yield break;
        }

        var token = node.Token;

        // int / long path
        if (token.Value is int intValue)
        {
            yield return MakeNumericMutation(node, intValue + 1, "+1");
            yield return MakeNumericMutation(node, intValue - 1, "-1");
            yield break;
        }
        if (token.Value is long longValue)
        {
            yield return MakeNumericMutation(node, longValue + 1L, "+1");
            yield return MakeNumericMutation(node, longValue - 1L, "-1");
            yield break;
        }

        // double / float / decimal path — emit ±1.0 to keep the shape numeric
        if (token.Value is double doubleValue)
        {
            yield return MakeNumericMutation(node, doubleValue + 1.0, "+1");
            yield return MakeNumericMutation(node, doubleValue - 1.0, "-1");
            yield break;
        }
        if (token.Value is float floatValue)
        {
            yield return MakeNumericMutation(node, floatValue + 1.0f, "+1");
            yield return MakeNumericMutation(node, floatValue - 1.0f, "-1");
        }
    }

    private static Mutation MakeNumericMutation(LiteralExpressionSyntax original, IConvertible newValue, string suffix)
    {
        var literal = SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(Convert.ToString(newValue, System.Globalization.CultureInfo.InvariantCulture)!,
                                  Convert.ToDouble(newValue, System.Globalization.CultureInfo.InvariantCulture)));
        return new Mutation
        {
            OriginalNode = original,
            ReplacementNode = literal.WithCleanTriviaFrom(original),
            DisplayName = $"Inline-constants ({original.Token.Text} -> {newValue} [{suffix}])",
            Type = Mutator.Linq, // closest existing Mutator enum kind for inline constants
        };
    }
}
