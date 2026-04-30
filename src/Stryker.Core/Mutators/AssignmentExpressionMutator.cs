using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

[MutationProfileMembership(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]

public class AssignmentExpressionMutator : MutatorBase<AssignmentExpressionSyntax>
{
    // Phase 10.4: FrozenDictionary for O(1) SyntaxKind lookup; mutator runs per syntax-node so this is hot.
    private static readonly FrozenDictionary<SyntaxKind, IEnumerable<SyntaxKind>> KindsToMutate =
        new Dictionary<SyntaxKind, IEnumerable<SyntaxKind>>
        {
            [SyntaxKind.AddAssignmentExpression] = [SyntaxKind.SubtractAssignmentExpression],
            [SyntaxKind.SubtractAssignmentExpression] = [SyntaxKind.AddAssignmentExpression],
            [SyntaxKind.MultiplyAssignmentExpression] = [SyntaxKind.DivideAssignmentExpression],
            [SyntaxKind.DivideAssignmentExpression] = [SyntaxKind.MultiplyAssignmentExpression],
            [SyntaxKind.ModuloAssignmentExpression] = [SyntaxKind.MultiplyAssignmentExpression],
            [SyntaxKind.AndAssignmentExpression] = [SyntaxKind.OrAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression],
            [SyntaxKind.OrAssignmentExpression] = [SyntaxKind.AndAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression],
            [SyntaxKind.ExclusiveOrAssignmentExpression] = [SyntaxKind.OrAssignmentExpression, SyntaxKind.AndAssignmentExpression],
            [SyntaxKind.LeftShiftAssignmentExpression] = [SyntaxKind.RightShiftAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression],
            [SyntaxKind.RightShiftAssignmentExpression] = [SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression],
            [SyntaxKind.CoalesceAssignmentExpression] = [SyntaxKind.SimpleAssignmentExpression],
            [SyntaxKind.UnsignedRightShiftAssignmentExpression] = [SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.RightShiftAssignmentExpression],
        }.ToFrozenDictionary();

    public override MutationLevel MutationLevel => MutationLevel.Standard;

    public override IEnumerable<Mutation> ApplyMutations(AssignmentExpressionSyntax node, SemanticModel semanticModel)
    {
        var assignmentKind = node.Kind();

        if (assignmentKind == SyntaxKind.AddAssignmentExpression
            && (node.Left.IsAStringExpression() || node.Right.IsAStringExpression()))
        {
            yield break;
        }

        if (!KindsToMutate.TryGetValue(assignmentKind, out var targetAssignmentKinds))
        {
            yield break;
        }


        foreach (var targetAssignmentKind in targetAssignmentKinds)
        {
            var replacementNode =
                SyntaxFactory.AssignmentExpression(targetAssignmentKind, node.Left.WithCleanTrivia(), node.Right.WithCleanTrivia());
            replacementNode = replacementNode.WithOperatorToken(replacementNode.OperatorToken.WithCleanTriviaFrom(node.OperatorToken));
            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = replacementNode,
                DisplayName = $"{assignmentKind} to {targetAssignmentKind} mutation",
                Type = Mutator.Assignment
            };
        }
    }
}
