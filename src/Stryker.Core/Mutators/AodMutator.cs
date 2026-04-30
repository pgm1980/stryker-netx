using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 10, comparison.md §4.1 — PIT AOD = Arithmetic Operator
/// Deletion): replace an arithmetic binary expression with one of its operands.
/// <c>a + b</c> -> <c>a</c> AND <c>a + b</c> -> <c>b</c>. Two mutations per
/// arithmetic site. Catches "is the operator necessary" tests cleanly.
///
/// Profile membership: Stronger | All (NOT Defaults). AOD is famously noisy
/// in literature; opt-in only.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class AodMutator : MutatorBase<BinaryExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(BinaryExpressionSyntax node, SemanticModel semanticModel)
    {
        if (!IsArithmetic(node.Kind()))
        {
            yield break;
        }

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = node.Left.WithCleanTriviaFrom(node),
            DisplayName = $"AOD: '{node.OperatorToken.Text}' deletion (keep left operand)",
            Type = Mutator.Arithmetic,
        };
        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = node.Right.WithCleanTriviaFrom(node),
            DisplayName = $"AOD: '{node.OperatorToken.Text}' deletion (keep right operand)",
            Type = Mutator.Arithmetic,
        };
    }

    private static bool IsArithmetic(SyntaxKind kind) => kind is
        SyntaxKind.AddExpression
        or SyntaxKind.SubtractExpression
        or SyntaxKind.MultiplyExpression
        or SyntaxKind.DivideExpression
        or SyntaxKind.ModuloExpression;
}
