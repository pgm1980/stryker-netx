using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 11, comparison.md §4.2 — cargo-mutants C4): for every
/// pattern-matching guard clause (<c>case X when expr</c>), emit two
/// mutations: replace <c>expr</c> with <c>true</c> AND with <c>false</c>.
/// Catches "is the guard condition actually checked?" tests cleanly.
///
/// C# 8+ pattern-matching syntax — Stryker.NET v1.x doesn't mutate
/// <c>when</c>-clauses directly; this closes the cargo-mutants-flagged gap.
///
/// Profile membership: Stronger | All. Targeted catalogue — opt-in.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class MatchGuardMutator : MutatorBase<WhenClauseSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(WhenClauseSyntax node, SemanticModel semanticModel)
    {
        // Mutate the guard expression to literal-true (clause always matches).
        var trueClause = node.WithCondition(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = trueClause.WithCleanTriviaFrom(node),
            DisplayName = "Match guard: 'when expr' → 'when true'",
            Type = Mutator.Boolean,
        };

        // Mutate the guard expression to literal-false (clause never matches).
        var falseClause = node.WithCondition(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = falseClause.WithCleanTriviaFrom(node),
            DisplayName = "Match guard: 'when expr' → 'when false'",
            Type = Mutator.Boolean,
        };
    }
}
