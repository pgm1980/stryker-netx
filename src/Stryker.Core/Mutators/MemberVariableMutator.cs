using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.1 — PIT EXP_MEMBER_VARIABLE):
/// rewrites <c>this.field = expr;</c> as <c>this.field = default;</c>
/// (and same for properties / instance-member assignments). Catches "is
/// the assigned value actually meaningful?" — code that always reads the
/// field via a path that doesn't depend on its content survives.
///
/// Type-aware: requires the LHS to resolve to an instance field or
/// property symbol (locals, parameters, statics are skipped). Always
/// compiles because <c>default</c> is assignable to any field type.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Only acts on <see cref="SyntaxKind.SimpleAssignmentExpression"/>
///         (<c>=</c>) — compound assignments (<c>+=</c> etc.) are skipped
///         since their semantics are read-modify-write, not pure assign.</item>
///   <item>Skips when the RHS is already a <c>default</c> literal (no
///         net change).</item>
///   <item>Static fields/properties skipped — <c>default</c> reset on a
///         static would have process-global impact.</item>
/// </list>
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class MemberVariableMutator : TypeAwareMutatorBase<AssignmentExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(AssignmentExpressionSyntax node, SemanticModel semanticModel)
    {
        if (!node.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            yield break;
        }

        // Skip if RHS is already `default`.
        if (node.Right is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            yield break;
        }

        var symbol = semanticModel.GetSymbolInfo(node.Left).Symbol;
        var isInstanceFieldOrProperty = symbol switch
        {
            IFieldSymbol field => !field.IsStatic,
            IPropertySymbol property => !property.IsStatic,
            _ => false,
        };
        if (!isInstanceFieldOrProperty)
        {
            yield break;
        }

        var defaultLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
        var replacement = node.WithRight(defaultLiteral);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = $"Member assignment reset: '{node.Left} = …' → '{node.Left} = default'",
            Type = Mutator.Initializer,
        };
    }
}
