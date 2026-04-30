using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 11, comparison.md §4.1 — PIT EXP_NAKED_RECEIVER): replaces
/// a method invocation <c>a.Method(...)</c> with the bare receiver <c>a</c>.
/// Catches "is the method's transformation actually observed?" tests — if
/// the test only inspects identity-shape properties of the receiver, the
/// naked-receiver mutant survives.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Only acts on instance method calls (member-access invocation).</item>
///   <item>Skips when the call's parent context expects a specific shape
///         that the receiver wouldn't satisfy (await, throw, foreach in).</item>
/// </list>
///
/// Profile membership: All only — most aggressive method-call mutator.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class NakedReceiverMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        // Only consider receiver-bearing invocations: receiver.Method(args).
        if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            yield break;
        }

        // Skip await: await x.Foo() -> await x is rarely valid (x must be awaitable).
        if (node.Parent is AwaitExpressionSyntax)
        {
            yield break;
        }

        // Skip throw expressions: same reason.
        if (node.Parent is ThrowExpressionSyntax or ThrowStatementSyntax)
        {
            yield break;
        }

        var receiver = memberAccess.Expression;

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = receiver.WithCleanTriviaFrom(node),
            DisplayName = $"Naked receiver: '{memberAccess.Name}(...)' dropped, keep receiver",
            Type = Mutator.Statement,
        };
    }
}
