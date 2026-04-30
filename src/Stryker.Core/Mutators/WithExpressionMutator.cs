using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 11, comparison.md §4.2 — cargo-mutants C5): mutates
/// C# 9+ <c>record with { ... }</c> expressions by removing one initializer
/// at a time. Catches "is this field actually being updated by the with-
/// expression?" tests — i.e. tests that don't observe the post-with state
/// of a particular property.
///
/// For an expression with N initializers, emits N mutations (each missing
/// one of the initializers). This pattern matches cargo-mutants's
/// struct-literal-field-deletion mutator, adapted to C# records.
///
/// Profile membership: Stronger | All. Records-specific — opt-in.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class WithExpressionMutator : MutatorBase<WithExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(WithExpressionSyntax node, SemanticModel semanticModel)
    {
        var initializer = node.Initializer;
        var expressions = initializer.Expressions;
        if (expressions.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < expressions.Count; i++)
        {
            var droppedExpression = expressions[i];
            var remaining = SyntaxFactory.SeparatedList<ExpressionSyntax>(
                expressions.Where((_, idx) => idx != i));
            var newInitializer = initializer.WithExpressions(remaining);
            var replacement = node.WithInitializer(newInitializer);

            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = replacement.WithCleanTriviaFrom(node),
                DisplayName = $"With-expression: drop '{droppedExpression.ToString().Trim()}'",
                Type = Mutator.Initializer,
            };
        }
    }
}
