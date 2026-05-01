using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.3.0 (Sprint 16, comparison.md §4.4 — greenfield .NET-specific):
/// rewrites <c>await x</c> as <c>x.Result</c> — the spec-faithful semantic
/// variant of v2.0.0's <see cref="AsyncAwaitMutator"/>, which emits
/// <c>x.GetAwaiter().GetResult()</c>.
///
/// Both mutators are useful in the catalogue because their failure
/// signatures differ:
/// <list type="bullet">
///   <item><c>x.Result</c> wraps any thrown exception in
///         <see cref="System.AggregateException"/>.</item>
///   <item><c>x.GetAwaiter().GetResult()</c> unwraps and rethrows the
///         original exception.</item>
/// </list>
/// Tests that assert on a specific exception type may pass under one
/// substitution and fail under the other — having both mutators in the
/// catalogue maximises kill-detection sensitivity for sync-over-async
/// anti-patterns.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Skips <c>await foreach</c> / <c>await using</c> (different syntax nodes).</item>
///   <item>Always compiles: <c>Task</c> / <c>Task&lt;T&gt;</c> / <c>ValueTask&lt;T&gt;</c>
///         all expose a <c>.Result</c> property in .NET. <c>Task</c>
///         (non-generic) does not, but <c>await x</c> on a non-generic
///         <c>Task</c> resolves to <c>void</c> — substituting <c>x.Result</c>
///         then yields a parser/semantic mismatch which the runner classifies
///         as killed. Acceptable.</item>
/// </list>
///
/// Profile membership: Stronger | All. Async-correctness probe — opt-in.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class AsyncAwaitResultMutator : MutatorBase<AwaitExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(AwaitExpressionSyntax node, SemanticModel semanticModel)
    {
        var awaited = node.Expression;

        // Build: awaited.Result
        var resultAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            awaited,
            SyntaxFactory.IdentifierName("Result"));

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = ((ExpressionSyntax)resultAccess).WithCleanTriviaFrom(node),
            DisplayName = "Async/await: 'await x' → 'x.Result' (sync-over-async, AggregateException-wrapping variant)",
            Type = Mutator.Statement,
        };
    }
}
