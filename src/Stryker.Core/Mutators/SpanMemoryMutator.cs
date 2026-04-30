using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 12, comparison.md §4.4 — greenfield .NET-specific): mutates
/// <c>span.Slice(start, length)</c> by zeroing the start argument
/// (<c>span.Slice(0, length)</c>). Catches off-by-N slicing tests cleanly —
/// if the test only checks the slice length but not its content, the start-
/// dropped mutant survives.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Only acts on member-access invocations named exactly <c>Slice</c>.</item>
///   <item>Only when there are exactly two arguments (the <c>(start, length)</c> overload).</item>
///   <item>Skips when the first argument is already a literal <c>0</c>.</item>
/// </list>
/// The mutant always compiles: <c>0</c> is a valid <see cref="int"/> for
/// every <c>Slice</c> overload that takes a start.
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class SpanMemoryMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
        {
            yield break;
        }

        if (!string.Equals(member.Name.Identifier.Text, "Slice", StringComparison.Ordinal))
        {
            yield break;
        }

        var args = node.ArgumentList.Arguments;
        if (args.Count != 2)
        {
            yield break;
        }

        // Skip if start is already literal zero.
        var firstArg = args[0].Expression;
        if (firstArg is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.NumericLiteralExpression)
            && string.Equals(lit.Token.ValueText, "0", StringComparison.Ordinal))
        {
            yield break;
        }

        var zero = SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(0));

        var newArgs = node.ArgumentList.WithArguments(
            args.Replace(args[0], args[0].WithExpression(zero)));
        var replacement = node.WithArgumentList(newArgs);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = "Span/Memory: 'Slice(start, length)' → 'Slice(0, length)'",
            Type = Mutator.Statement,
        };
    }
}
