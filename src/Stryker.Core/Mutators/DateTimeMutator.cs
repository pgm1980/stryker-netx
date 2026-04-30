using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 12, comparison.md §4.4 — greenfield .NET-specific): swaps
/// <c>DateTime.Now</c> ↔ <c>DateTime.UtcNow</c> and <c>DateTimeOffset.Now</c> ↔
/// <c>DateTimeOffset.UtcNow</c>. Catches "is the time-source actually mocked
/// or the right one?" tests — code that mixes Now/UtcNow without a clock
/// abstraction is a classic source of timezone-dependent bugs.
///
/// Conservative scope: only acts on the four exact <c>Type.Member</c> patterns
/// above. Does not touch other DateTime members, instance accesses, or
/// <c>DateTime.Today</c> etc. — a focused, always-compiles probe.
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class DateTimeMutator : MutatorBase<MemberAccessExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(MemberAccessExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not IdentifierNameSyntax typeIdent)
        {
            yield break;
        }

        var typeName = typeIdent.Identifier.Text;
        if (typeName is not ("DateTime" or "DateTimeOffset"))
        {
            yield break;
        }

        var memberName = node.Name.Identifier.Text;
        var (replacement, label) = memberName switch
        {
            "Now" => ("UtcNow", $"DateTime: '{typeName}.Now' → '{typeName}.UtcNow'"),
            "UtcNow" => ("Now", $"DateTime: '{typeName}.UtcNow' → '{typeName}.Now'"),
            _ => (null, null),
        };

        if (replacement is null)
        {
            yield break;
        }

        var newAccess = node.WithName(SyntaxFactory.IdentifierName(replacement));

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = newAccess.WithCleanTriviaFrom(node),
            DisplayName = label!,
            Type = Mutator.Statement,
        };
    }
}
