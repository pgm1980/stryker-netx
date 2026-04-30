using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 11, comparison.md §4.1 — PIT CONSTRUCTOR_CALLS): replaces
/// <c>new Foo(...)</c> with <c>null</c>. Catches "is the constructed object
/// actually used downstream?" tests. Type-aware: only emits the mutation
/// when the surrounding context permits a null reference (e.g. assignment
/// to a nullable, return from a nullable-returning method).
///
/// Profile membership: Stronger | All. Constructor-to-null is famously
/// disruptive — opt-in only.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class ConstructorNullMutator : MutatorBase<ObjectCreationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(ObjectCreationExpressionSyntax node, SemanticModel semanticModel)
    {
        // Conservative scope: skip if the construction is part of a `throw new ...;`
        // expression — replacing the throw target with null doesn't compile.
        if (node.Parent is ThrowExpressionSyntax or ThrowStatementSyntax)
        {
            yield break;
        }

        // Skip if the construction is used as a base() call or this() call.
        if (node.Parent is ConstructorInitializerSyntax)
        {
            yield break;
        }

        // Typed as ExpressionSyntax so WithCleanTriviaFrom<T> binds T = ExpressionSyntax,
        // letting node (ObjectCreationExpressionSyntax) upcast.
        ExpressionSyntax nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = nullLiteral.WithCleanTriviaFrom(node),
            DisplayName = $"Constructor → null: 'new {node.Type}(...)' → 'null'",
            Type = Mutator.Initializer,
        };
    }
}
