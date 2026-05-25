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
///
/// v3.3.0 (Sprint 168, ADR-047 P0 — Bug Report #9 Anomaly #4):
/// type-aware literal emission. Previously the helper emitted via
/// <c>SyntaxFactory.Literal(string, double)</c> which forced every
/// emitted token to <c>Token.Value : double</c>, causing CS0266 /
/// CS1503 / CS0029 cascades on int / long / uint / ulong / decimal
/// slots (~39% CompileError rate on real codebases). The new
/// <c>Make()</c> helper switch-dispatches on the original value type to
/// the correct <c>SyntaxFactory.Literal(T)</c> overload, preserving the
/// numeric type end-to-end. Coverage extended from {int, long, float,
/// double} to {int, uint, long, ulong, float, double, decimal} — the
/// seven numeric types Roslyn supports as literal tokens.
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

        switch (node.Token.Value)
        {
            case int v:
                yield return Make(node, v + 1, "+1");
                yield return Make(node, v - 1, "-1");
                yield break;
            case uint v:
                yield return Make(node, v + 1u, "+1");
                yield return Make(node, v - 1u, "-1");
                yield break;
            case long v:
                yield return Make(node, v + 1L, "+1");
                yield return Make(node, v - 1L, "-1");
                yield break;
            case ulong v:
                yield return Make(node, v + 1ul, "+1");
                yield return Make(node, v - 1ul, "-1");
                yield break;
            case float v:
                yield return Make(node, v + 1f, "+1");
                yield return Make(node, v - 1f, "-1");
                yield break;
            case double v:
                yield return Make(node, v + 1d, "+1");
                yield return Make(node, v - 1d, "-1");
                yield break;
            case decimal v:
                foreach (var mut in MakeDecimalPair(node, v))
                {
                    yield return mut;
                }
                yield break;
        }
    }

    /// <summary>
    /// Sprint 168 (v3.3.0): emits a typed-correct numeric literal via Roslyn's
    /// per-type <c>SyntaxFactory.Literal(T)</c> overloads. The switch-expression
    /// dispatches on the boxed value type. Boxing-cost is negligible because
    /// the mutator runs at AST-walk time, not in the per-mutant test-execution
    /// hot path.
    /// </summary>
    private static Mutation Make(LiteralExpressionSyntax original, object newValue, string suffix)
    {
        SyntaxToken literalToken = newValue switch
        {
            int i => SyntaxFactory.Literal(i),
            uint u => SyntaxFactory.Literal(u),
            long l => SyntaxFactory.Literal(l),
            ulong ul => SyntaxFactory.Literal(ul),
            float f => SyntaxFactory.Literal(f),
            double d => SyntaxFactory.Literal(d),
            decimal m => SyntaxFactory.Literal(m),
            _ => throw new InvalidOperationException(
                $"InlineConstantsMutator.Make does not support numeric type {newValue.GetType()}; " +
                "extend the switch in ApplyMutations and Make together."),
        };
        var literal = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken);
        return new Mutation
        {
            OriginalNode = original,
            ReplacementNode = literal.WithCleanTriviaFrom(original),
            DisplayName = $"Inline-constants ({original.Token.Text} -> {newValue} [{suffix}])",
            Type = Mutator.Linq,
        };
    }

    /// <summary>
    /// Sprint 168 (v3.3.0): decimal arithmetic is checked by default and overflows
    /// at <c>decimal.MaxValue + 1m</c> / <c>decimal.MinValue - 1m</c>. Wrap each
    /// candidate in a try/catch so the overflowing variant is silently skipped
    /// while the non-overflowing one still ships.
    /// </summary>
    private static IEnumerable<Mutation> MakeDecimalPair(LiteralExpressionSyntax original, decimal baseValue)
    {
        var plus = TryMakeDecimal(original, baseValue, +1m, "+1");
        var minus = TryMakeDecimal(original, baseValue, -1m, "-1");
        if (plus is not null)
        {
            yield return plus;
        }
        if (minus is not null)
        {
            yield return minus;
        }
    }

    private static Mutation? TryMakeDecimal(LiteralExpressionSyntax original, decimal baseValue, decimal delta, string suffix)
    {
        try
        {
            return Make(original, baseValue + delta, suffix);
        }
        catch (OverflowException)
        {
            return null;
        }
    }
}
