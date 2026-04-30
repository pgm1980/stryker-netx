using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 12, comparison.md §4.4 — greenfield .NET-specific):
/// rewrites <c>await x</c> as <c>x.GetAwaiter().GetResult()</c> — a sync-over-async
/// substitution. Catches "is the awaiter actually awaited (not just blocked on)?"
/// tests, and tests that pass on async libraries but would deadlock under a
/// SynchronizationContext.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Skips <c>await foreach</c> / <c>await using</c> (different syntax nodes).</item>
///   <item>Always compiles: every awaitable exposes <c>GetAwaiter().GetResult()</c>.</item>
/// </list>
///
/// Profile membership: Stronger | All. Async-correctness probe — opt-in.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class AsyncAwaitMutator : MutatorBase<AwaitExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(AwaitExpressionSyntax node, SemanticModel semanticModel)
    {
        var awaited = node.Expression;

        // Build: awaited.GetAwaiter().GetResult()
        var getAwaiter = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                awaited,
                SyntaxFactory.IdentifierName("GetAwaiter")));

        var getResult = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                getAwaiter,
                SyntaxFactory.IdentifierName("GetResult")));

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = ((ExpressionSyntax)getResult).WithCleanTriviaFrom(node),
            DisplayName = "Async/await: 'await x' → 'x.GetAwaiter().GetResult()' (sync-over-async)",
            Type = Mutator.Statement,
        };
    }
}
