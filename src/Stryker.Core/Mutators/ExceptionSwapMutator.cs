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
/// v2.0.0 (Sprint 12, comparison.md §4.4 — greenfield .NET-specific): swaps
/// the type in <c>throw new ArgumentNullException(...)</c> with a sibling
/// in the same family (e.g. <c>ArgumentException</c>). Catches "does the
/// catch handler / test discriminate exception type?" — many tests assert
/// on a base type and survive a swap to a sibling, which is a real bug
/// category.
///
/// Conservative scope: only mutates a small whitelist of well-known
/// argument-related exceptions, and only inside <c>throw new T(...)</c>.
/// All swap targets share a constructor signature with the original
/// (string message, or string param + string message), so the result
/// always compiles.
///
/// Profile membership: All only — most disruptive of the greenfield batch.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class ExceptionSwapMutator : MutatorBase<ObjectCreationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    private static readonly ImmutableDictionary<string, string> Swaps =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ArgumentNullException"] = "ArgumentException",
            ["ArgumentException"] = "ArgumentNullException",
            ["ArgumentOutOfRangeException"] = "ArgumentException",
            ["InvalidOperationException"] = "NotSupportedException",
            ["NotSupportedException"] = "InvalidOperationException",
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public override IEnumerable<Mutation> ApplyMutations(ObjectCreationExpressionSyntax node, SemanticModel semanticModel)
    {
        // Only inside throw — outside, swapping the type is much riskier.
        if (node.Parent is not (ThrowExpressionSyntax or ThrowStatementSyntax))
        {
            yield break;
        }

        if (node.Type is not IdentifierNameSyntax typeIdent)
        {
            yield break;
        }

        var originalName = typeIdent.Identifier.Text;
        if (!Swaps.TryGetValue(originalName, out var swappedName))
        {
            yield break;
        }

        var newType = SyntaxFactory.IdentifierName(swappedName);
        var newCreation = node.WithType(newType);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = newCreation.WithCleanTriviaFrom(node),
            DisplayName = $"Exception swap: 'throw new {originalName}(...)' → 'throw new {swappedName}(...)'",
            Type = Mutator.Initializer,
        };
    }
}
