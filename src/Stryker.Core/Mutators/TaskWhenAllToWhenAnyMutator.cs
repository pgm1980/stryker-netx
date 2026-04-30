using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.4 — greenfield .NET-specific): swaps
/// <c>Task.WhenAll(...) ↔ Task.WhenAny(...)</c>. Catches "is the all/any
/// semantics actually tested?" — a test that passes a single-element task
/// collection won't tell <c>WhenAll</c> from <c>WhenAny</c>; an aggregation
/// test that doesn't observe individual results may also miss the swap.
///
/// Conservative scope: method-name filter only — <c>WhenAll</c> and
/// <c>WhenAny</c> are unambiguous on the BCL <c>Task</c> type. Both
/// directions emitted.
///
/// Compile-safety: not always. <c>WhenAll(IEnumerable&lt;Task&lt;T&gt;&gt;)</c>
/// returns <c>Task&lt;T[]&gt;</c>; <c>WhenAny(IEnumerable&lt;Task&lt;T&gt;&gt;)</c>
/// returns <c>Task&lt;Task&lt;T&gt;&gt;</c>. Call-sites that index the
/// result array won't compile after the swap. The runner classifies
/// non-compiling mutants as killed (precedent: <see cref="GenericConstraintMutator"/>).
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class TaskWhenAllToWhenAnyMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
        {
            yield break;
        }

        var memberName = member.Name.Identifier.Text;
        string? replacementName = null;
        string? label = null;
        if (string.Equals(memberName, "WhenAll", StringComparison.Ordinal))
        {
            replacementName = "WhenAny";
            label = "Task: 'WhenAll(...)' → 'WhenAny(...)'";
        }
        else if (string.Equals(memberName, "WhenAny", StringComparison.Ordinal))
        {
            replacementName = "WhenAll";
            label = "Task: 'WhenAny(...)' → 'WhenAll(...)'";
        }

        if (replacementName is null)
        {
            yield break;
        }

        var newName = SyntaxFactory.IdentifierName(replacementName);
        var newMember = member.WithName(newName);
        var replacement = node.WithExpression(newMember);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = label!,
            Type = Mutator.Statement,
        };
    }
}
