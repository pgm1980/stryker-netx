using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.4 — greenfield .NET-specific): swaps
/// <c>AsSpan() ↔ AsMemory()</c> (and the read-only variants). Catches "is
/// the chosen view-type tested?" — code that passes through both
/// <c>Span&lt;T&gt;</c> and <c>Memory&lt;T&gt;</c>-typed APIs only by way
/// of one of them will fail when the other is silently substituted, but
/// only if a test exercises the divergent path.
///
/// Compile-safety: not always. <c>Span&lt;T&gt;</c> is a <c>ref struct</c>
/// and cannot be stored on the heap or passed across <c>await</c> /
/// <c>yield</c> boundaries; <c>Memory&lt;T&gt;</c> can. Most call-sites
/// that pass the result to a method with a <c>Span&lt;T&gt;</c> parameter
/// won't compile after the swap to <c>AsMemory()</c>. The runner
/// classifies non-compiling mutants as killed (precedent:
/// <see cref="GenericConstraintMutator"/>).
///
/// Profile membership: All only — high compile-failure rate.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class AsSpanAsMemoryMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    private static readonly ImmutableDictionary<string, string> Swaps =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AsSpan"] = "AsMemory",
            ["AsMemory"] = "AsSpan",
            ["AsReadOnlySpan"] = "AsReadOnlyMemory",
            ["AsReadOnlyMemory"] = "AsReadOnlySpan",
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
        {
            yield break;
        }

        var memberName = member.Name.Identifier.Text;
        if (!Swaps.TryGetValue(memberName, out var replacementName))
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
            DisplayName = $"Span/Memory view: '{memberName}()' → '{replacementName}()'",
            Type = Mutator.Statement,
        };
    }
}
