using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.1.0 (Sprint 14, comparison.md §4.1 — PIT CRCR Constant Replacement
/// Composite): replaces a numeric literal <c>c</c> with each of
/// <c>0, 1, -1, -c</c> (the four axes not covered by v2.0.0's
/// <see cref="InlineConstantsMutator"/>, which already emits <c>c+1</c>
/// and <c>c-1</c>). Catches "is the exact constant value tested?"
/// substantially more aggressively than the boundary-only inline
/// constants pass — useful for cases where <c>+1</c>/<c>-1</c> happens
/// to land on a value the test still passes against.
///
/// Conservative scope per axis:
/// <list type="bullet">
///   <item><c>→ 0</c>: skipped if literal already <c>0</c>.</item>
///   <item><c>→ 1</c>: skipped if literal already <c>1</c>.</item>
///   <item><c>→ -1</c>: skipped if literal already <c>-1</c> (rare in
///         literal form; usually written as <c>-1</c> via unary).</item>
///   <item><c>→ -c</c>: skipped if literal already <c>0</c> (no-op).</item>
/// </list>
/// Always compiles — every numeric type has 0/1/-1 representations.
///
/// Profile membership: Stronger | All (matches <see cref="InlineConstantsMutator"/>).
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class ConstantReplacementMutator : MutatorBase<LiteralExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(LiteralExpressionSyntax node, SemanticModel semanticModel)
    {
        if (!node.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            yield break;
        }

        var token = node.Token;

        if (token.Value is int intValue)
        {
            foreach (var m in IntMutations(node, intValue)) yield return m;
            yield break;
        }
        if (token.Value is long longValue)
        {
            foreach (var m in LongMutations(node, longValue)) yield return m;
            yield break;
        }
        if (token.Value is double doubleValue)
        {
            foreach (var m in DoubleMutations(node, doubleValue)) yield return m;
            yield break;
        }
        if (token.Value is float floatValue)
        {
            foreach (var m in FloatMutations(node, floatValue)) yield return m;
        }
    }

    private static IEnumerable<Mutation> IntMutations(LiteralExpressionSyntax node, int v)
    {
        if (v != 0) yield return MakeNumericMutation(node, 0, "→0");
        if (v != 1) yield return MakeNumericMutation(node, 1, "→1");
        if (v != -1) yield return MakeNumericMutation(node, -1, "→-1");
        if (v != 0) yield return MakeNumericMutation(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> LongMutations(LiteralExpressionSyntax node, long v)
    {
        if (v != 0L) yield return MakeNumericMutation(node, 0L, "→0");
        if (v != 1L) yield return MakeNumericMutation(node, 1L, "→1");
        if (v != -1L) yield return MakeNumericMutation(node, -1L, "→-1");
        if (v != 0L) yield return MakeNumericMutation(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> DoubleMutations(LiteralExpressionSyntax node, double v)
    {
        // S1244 noted: skip-on-equality optimization disabled for floats.
        // Worst case: 0.0 → 0.0 emits a no-op mutation (very rare in practice
        // and harmless — runner detects unchanged behavior).
        yield return MakeNumericMutation(node, 0.0, "→0");
        yield return MakeNumericMutation(node, 1.0, "→1");
        yield return MakeNumericMutation(node, -1.0, "→-1");
        yield return MakeNumericMutation(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> FloatMutations(LiteralExpressionSyntax node, float v)
    {
        yield return MakeNumericMutation(node, 0f, "→0");
        yield return MakeNumericMutation(node, 1f, "→1");
        yield return MakeNumericMutation(node, -1f, "→-1");
        yield return MakeNumericMutation(node, -v, "→-c");
    }

    private static Mutation MakeNumericMutation(LiteralExpressionSyntax original, IConvertible newValue, string axis)
    {
        var asString = Convert.ToString(newValue, CultureInfo.InvariantCulture)!;
        var asDouble = Convert.ToDouble(newValue, CultureInfo.InvariantCulture);
        var literal = SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(asString, asDouble));
        return new Mutation
        {
            OriginalNode = original,
            ReplacementNode = literal.WithCleanTriviaFrom(original),
            DisplayName = $"CRCR ({original.Token.Text} {axis} → {newValue})",
            Type = Mutator.Linq,
        };
    }
}
