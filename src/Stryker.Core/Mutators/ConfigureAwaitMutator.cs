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
/// the boolean literal argument of <c>ConfigureAwait(false/true)</c>. Catches
/// "is the synchronization-context behavior actually tested?" — code that
/// configures one way but is exercised under a context that masks the
/// difference will survive the swap.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Only acts on <c>x.ConfigureAwait(literal)</c> where <c>literal</c>
///         is exactly <c>true</c> or <c>false</c>. Variable / expression
///         arguments are skipped — swapping them makes no semantic sense.</item>
///   <item>Always compiles: both <c>true</c> and <c>false</c> are valid for
///         the same overload set (<c>Task.ConfigureAwait(bool)</c> /
///         <c>Task&lt;T&gt;.ConfigureAwait(bool)</c>).</item>
/// </list>
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class ConfigureAwaitMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
        {
            yield break;
        }

        if (!string.Equals(member.Name.Identifier.Text, "ConfigureAwait", StringComparison.Ordinal))
        {
            yield break;
        }

        var args = node.ArgumentList.Arguments;
        if (args.Count != 1)
        {
            yield break;
        }

        if (args[0].Expression is not LiteralExpressionSyntax literal)
        {
            yield break;
        }

        SyntaxKind originalKind, replacementKind;
        string originalText, replacementText;
        if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            originalKind = SyntaxKind.TrueLiteralExpression;
            replacementKind = SyntaxKind.FalseLiteralExpression;
            originalText = "true";
            replacementText = "false";
        }
        else if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            originalKind = SyntaxKind.FalseLiteralExpression;
            replacementKind = SyntaxKind.TrueLiteralExpression;
            originalText = "false";
            replacementText = "true";
        }
        else
        {
            yield break;
        }

        _ = originalKind;
        var newLiteral = SyntaxFactory.LiteralExpression(replacementKind);
        var newArg = args[0].WithExpression(newLiteral);
        var newArgs = node.ArgumentList.WithArguments(args.Replace(args[0], newArg));
        var replacement = node.WithArgumentList(newArgs);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = $"ConfigureAwait: '{originalText}' → '{replacementText}'",
            Type = Mutator.Boolean,
        };
    }
}
