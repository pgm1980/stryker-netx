using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 10, comparison.md §4.1 — PIT UOI = Unary Operator
/// Insertion): wraps an identifier in increment/decrement so a use-of-x
/// becomes a use-and-mutate-x. <c>x</c> -> <c>x++</c>, <c>x</c> -> <c>++x</c>,
/// <c>x</c> -> <c>x--</c>, <c>x</c> -> <c>--x</c>. Targets bare identifier
/// references in expression position; conservatively skips contexts where
/// the assignment-side change would be syntactically invalid (lvalue
/// requirements, ref-passing, etc.).
///
/// Profile membership: All only — UOI is famously aggressive (every
/// identifier use becomes 4 mutations); opt-in via --profile All.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class UoiMutator : MutatorBase<IdentifierNameSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(IdentifierNameSyntax node, SemanticModel semanticModel)
    {
        // Conservative scope: only mutate when the identifier appears in a
        // simple expression position. Skip when the node is the target of
        // assignment, a member-access head, an invocation, or already inside
        // an increment expression (avoid double-wrapping).
        if (!IsSafeToWrap(node))
        {
            yield break;
        }

        yield return WrapIn(node, SyntaxKind.PostIncrementExpression, "x++");
        yield return WrapIn(node, SyntaxKind.PreIncrementExpression, "++x");
        yield return WrapIn(node, SyntaxKind.PostDecrementExpression, "x--");
        yield return WrapIn(node, SyntaxKind.PreDecrementExpression, "--x");
    }

    private static Mutation WrapIn(IdentifierNameSyntax original, SyntaxKind kind, string label)
    {
        ExpressionSyntax replacement = kind is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
            ? SyntaxFactory.PostfixUnaryExpression(kind, original)
            : SyntaxFactory.PrefixUnaryExpression(kind, original);
        return new Mutation
        {
            OriginalNode = original,
            ReplacementNode = replacement.WithCleanTriviaFrom(original),
            DisplayName = $"UOI: '{original.Identifier.Text}' -> '{label}'",
            Type = Mutator.Update,
        };
    }

    private static bool IsSafeToWrap(IdentifierNameSyntax node)
    {
        var parent = node.Parent;

        // Skip if the identifier is itself the operand of an existing increment.
        if (parent is PrefixUnaryExpressionSyntax pre &&
            pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
        {
            return false;
        }
        if (parent is PostfixUnaryExpressionSyntax post &&
            post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression)
        {
            return false;
        }

        // Skip if the identifier is the LHS of an assignment.
        if (parent is AssignmentExpressionSyntax assign && assign.Left == node)
        {
            return false;
        }

        // Skip member-access heads / invocation targets — wrapping there changes call semantics.
        if (parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == node)
        {
            return false;
        }
        if (parent is InvocationExpressionSyntax inv && inv.Expression == node)
        {
            return false;
        }

        // Skip when used as a ref/out argument — would not compile.
        if (parent is ArgumentSyntax arg && (arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) ||
                                              arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)))
        {
            return false;
        }

        return true;
    }
}
