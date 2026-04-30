using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.2 — cargo-mutants C3): drops one
/// non-default arm at a time from a <c>switch</c> expression that ends in
/// a discard pattern (<c>_ =&gt; ...</c>). The discard catches the
/// formerly-routed cases, so the deletion is exhaustiveness-preserving and
/// always compiles. Catches "is the specific arm value actually verified?"
/// — tests that only assert membership in the value-set the discard returns
/// will survive.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Skips switch-expressions without a trailing discard arm
///         (deletion would change exhaustiveness).</item>
///   <item>Skips switch-expressions with fewer than 2 arms.</item>
///   <item>Never drops the discard arm itself.</item>
///   <item>Emits one mutation per non-discard arm.</item>
/// </list>
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class SwitchArmDeletionMutator : MutatorBase<SwitchExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(SwitchExpressionSyntax node, SemanticModel semanticModel)
    {
        var arms = node.Arms;
        if (arms.Count < 2)
        {
            yield break;
        }

        var lastArm = arms[arms.Count - 1];
        if (lastArm.Pattern is not DiscardPatternSyntax)
        {
            yield break;
        }

        // For every arm before the discard, emit a mutation that drops it.
        for (var i = 0; i < arms.Count - 1; i++)
        {
            var droppedArm = arms[i];
            var remaining = SyntaxFactory.SeparatedList(
                arms.Where((_, idx) => idx != i));
            var replacement = node.WithArms(remaining);

            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = replacement.WithCleanTriviaFrom(node),
                DisplayName = $"Switch arm dropped: '{droppedArm.Pattern.ToString().Trim()}' (discard catches)",
                Type = Mutator.Statement,
            };
        }
    }
}
