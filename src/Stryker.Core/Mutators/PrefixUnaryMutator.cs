using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

[MutationProfileMembership(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]

public class PrefixUnaryMutator : MutatorBase<PrefixUnaryExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Standard;

    // Phase 10.4: FrozenDictionary + FrozenSet for hot-path SyntaxKind lookup.
    private static readonly FrozenDictionary<SyntaxKind, SyntaxKind> UnaryWithOpposite =
        new Dictionary<SyntaxKind, SyntaxKind>
        {
            [SyntaxKind.UnaryMinusExpression] = SyntaxKind.UnaryPlusExpression,
            [SyntaxKind.UnaryPlusExpression] = SyntaxKind.UnaryMinusExpression,
            [SyntaxKind.PreIncrementExpression] = SyntaxKind.PreDecrementExpression,
            [SyntaxKind.PreDecrementExpression] = SyntaxKind.PreIncrementExpression,
        }.ToFrozenDictionary();

    private static readonly FrozenSet<SyntaxKind> UnaryToInitial =
        FrozenSet.ToFrozenSet([SyntaxKind.BitwiseNotExpression, SyntaxKind.LogicalNotExpression]);

    public override IEnumerable<Mutation> ApplyMutations(PrefixUnaryExpressionSyntax node, SemanticModel semanticModel)
    {
        var unaryKind = node.Kind();
        if (UnaryWithOpposite.TryGetValue(unaryKind, out var oppositeKind))
        {
            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = SyntaxFactory.PrefixUnaryExpression(oppositeKind, node.Operand.WithCleanTrivia()),
                DisplayName = $"{unaryKind} to {oppositeKind} mutation",
                Type = unaryKind.ToString().StartsWith("Unary", StringComparison.Ordinal) ? Mutator.Unary : Mutator.Update
            };
        }
        else if (UnaryToInitial.Contains(unaryKind))
        {
            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = node.Operand.WithCleanTrivia(),
                DisplayName = $"{unaryKind} to un-{unaryKind} mutation",
                Type = unaryKind.ToString().StartsWith("Logic", StringComparison.Ordinal) ? Mutator.Boolean : Mutator.Unary
            };
        }
    }
}
