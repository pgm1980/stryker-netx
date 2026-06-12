using System;
using System.Collections.Generic;
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
///         literal form; usually written as <c>-1</c> via unary). Skipped
///         entirely for unsigned types.</item>
///   <item><c>→ -c</c>: skipped if literal already <c>0</c> (no-op).
///         Skipped entirely for unsigned types. Silent-skipped via try/catch
///         when <c>-c</c> overflows on <c>decimal.MaxValue</c>/<c>MinValue</c>.</item>
/// </list>
///
/// Profile membership: Stronger | All (matches <see cref="InlineConstantsMutator"/>).
///
/// v3.3.1 (Sprint 169, ADR-049 P0 — BUG_REPORT_9_FOLLOWUP_2): type-aware literal
/// emission. Previously emitted via <c>SyntaxFactory.Literal(string, double)</c>
/// which forced every emitted token to <c>Token.Value : double</c>. The bug was
/// invisible on standalone mutator output (a single mutation in an int slot is
/// only one leaf in the ConditionalInstrumentationEngine ternary wrap), but when
/// the typed-correct leaves from <see cref="InlineConstantsMutator"/> (fixed in
/// ADR-047) joined this mutator's double leaves under the wrap, the common-type
/// inference picked double → CS0029/CS1503/CS0266 cascade on int/long/uint/ulong/
/// decimal slots. Reporter's v3.3.0 validation found 184 / 196 residual Safe Mode!
/// warnings traced here. The new <c>Make()</c> helper switch-dispatches on the
/// boxed value type to the correct <c>SyntaxFactory.Literal(T)</c> overload,
/// preserving the numeric type end-to-end. Coverage extended from {int, long,
/// float, double} to {int, uint, long, ulong, float, double, decimal} — matches
/// ADR-047 catalogue.
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

        switch (node.Token.Value)
        {
            case int v:
                foreach (var m in IntMutations(node, v)) yield return m;
                yield break;
            case uint v:
                foreach (var m in UIntMutations(node, v)) yield return m;
                yield break;
            case long v:
                foreach (var m in LongMutations(node, v)) yield return m;
                yield break;
            case ulong v:
                foreach (var m in ULongMutations(node, v)) yield return m;
                yield break;
            case float v:
                foreach (var m in FloatMutations(node, v)) yield return m;
                yield break;
            case double v:
                foreach (var m in DoubleMutations(node, v)) yield return m;
                yield break;
            case decimal v:
                foreach (var m in DecimalMutations(node, v)) yield return m;
                yield break;
        }
    }

    private static IEnumerable<Mutation> IntMutations(LiteralExpressionSyntax node, int v)
    {
        if (v != 0) yield return Make(node, 0, "→0");
        if (v != 1) yield return Make(node, 1, "→1");
        if (v != -1) yield return Make(node, -1, "→-1");
        if (v != 0) yield return Make(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> UIntMutations(LiteralExpressionSyntax node, uint v)
    {
        // Unsigned types have no negative representation; the →-1 / →-c axes are skipped entirely.
        if (v != 0u) yield return Make(node, 0u, "→0");
        if (v != 1u) yield return Make(node, 1u, "→1");
    }

    private static IEnumerable<Mutation> LongMutations(LiteralExpressionSyntax node, long v)
    {
        if (v != 0L) yield return Make(node, 0L, "→0");
        if (v != 1L) yield return Make(node, 1L, "→1");
        if (v != -1L) yield return Make(node, -1L, "→-1");
        if (v != 0L) yield return Make(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> ULongMutations(LiteralExpressionSyntax node, ulong v)
    {
        if (v != 0ul) yield return Make(node, 0ul, "→0");
        if (v != 1ul) yield return Make(node, 1ul, "→1");
    }

    private static IEnumerable<Mutation> FloatMutations(LiteralExpressionSyntax node, float v)
    {
        // S1244 noted: skip-on-equality optimization disabled for floats.
        // Worst case: 0.0 → 0.0 emits a no-op mutation (rare in practice
        // and harmless — runner detects unchanged behavior).
        yield return Make(node, 0f, "→0");
        yield return Make(node, 1f, "→1");
        yield return Make(node, -1f, "→-1");
        yield return Make(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> DoubleMutations(LiteralExpressionSyntax node, double v)
    {
        yield return Make(node, 0.0, "→0");
        yield return Make(node, 1.0, "→1");
        yield return Make(node, -1.0, "→-1");
        yield return Make(node, -v, "→-c");
    }

    private static IEnumerable<Mutation> DecimalMutations(LiteralExpressionSyntax node, decimal v)
    {
        // Decimal arithmetic is checked — decimal.MaxValue + 1m and -(decimal.MaxValue) overflow.
        // The skip-on-equality rule handles the no-op case; try-helpers handle the overflow case.
        var mut0 = v != 0m ? Make(node, 0m, "→0") : null;
        var mut1 = v != 1m ? Make(node, 1m, "→1") : null;
        var mutMinus1 = v != -1m ? Make(node, -1m, "→-1") : null;
        var mutMinusC = v != 0m ? TryMakeDecimalNegate(node, v) : null;
        if (mut0 is not null) yield return mut0;
        if (mut1 is not null) yield return mut1;
        if (mutMinus1 is not null) yield return mutMinus1;
        if (mutMinusC is not null) yield return mutMinusC;
    }

    private static Mutation? TryMakeDecimalNegate(LiteralExpressionSyntax original, decimal v)
    {
        try
        {
            return Make(original, -v, "→-c");
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sprint 169 (v3.3.1) — mirrors <see cref="InlineConstantsMutator"/>'s ADR-047
    /// Make() helper: switch-expression dispatches on the boxed value type to the
    /// correct <see cref="SyntaxFactory.Literal(int)"/> / Literal(long) / etc. overload,
    /// preserving the numeric type end-to-end. Boxing cost is negligible (AST-walk
    /// time, not per-mutant test-execution hot path).
    /// </summary>
    private static Mutation Make(LiteralExpressionSyntax original, object newValue, string axis)
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
                $"ConstantReplacementMutator.Make does not support numeric type {newValue.GetType()}; " +
                "extend the switch in ApplyMutations and Make together."),
        };
        var literal = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken);
        return new Mutation
        {
            OriginalNode = original,
            ReplacementNode = literal.WithCleanTriviaFrom(original),
            DisplayName = $"CRCR ({original.Token.Text} {axis} → {newValue})",
            Type = Mutator.Number,
        };
    }
}
