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
/// v2.0.1 (Sprint 13, comparison.md §4.4 — greenfield .NET-specific): negates
/// the single argument of <c>DateTime.AddDays(n) → AddDays(-n)</c> (and the
/// rest of the <c>Add*</c> family on <c>DateTime</c>, <c>DateTimeOffset</c>
/// and <c>TimeSpan</c>). Catches "is the addition direction tested?" — many
/// schedulers, retries, and date-pickers conflate +N and -N when the test
/// only checks magnitude.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Method-name-only filter — the names <c>AddDays</c>, <c>AddHours</c>,
///         <c>AddMinutes</c>, etc. are well-known on the BCL types and
///         unlikely to collide with custom APIs in a way that would break
///         the negation (any custom <c>AddDays(int)</c> still accepts a
///         negated <see cref="int"/>).</item>
///   <item>Only mutates single-argument calls (the BCL signatures).</item>
///   <item>If the argument is already <c>-x</c>, drops the minus
///         (<c>AddDays(-1) → AddDays(1)</c>) instead of double-negating.</item>
/// </list>
/// Always compiles.
///
/// Profile membership: Stronger | All.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class DateTimeAddSignMutator : MutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    private static readonly ImmutableHashSet<string> AddMembers =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "AddDays", "AddHours", "AddMinutes", "AddSeconds",
            "AddMilliseconds", "AddMicroseconds", "AddTicks",
            "AddMonths", "AddYears");

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
        {
            yield break;
        }

        var memberName = member.Name.Identifier.Text;
        if (!AddMembers.Contains(memberName))
        {
            yield break;
        }

        var args = node.ArgumentList.Arguments;
        if (args.Count != 1)
        {
            yield break;
        }

        var argExpr = args[0].Expression;
        ExpressionSyntax negated;
        string label;
        if (argExpr is PrefixUnaryExpressionSyntax pre && pre.IsKind(SyntaxKind.UnaryMinusExpression))
        {
            // Drop the unary minus: -x -> x.
            negated = pre.Operand;
            label = $"DateTime: '{memberName}(-x)' → '{memberName}(x)'";
        }
        else
        {
            // Wrap with unary minus: x -> -x.
            negated = SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.UnaryMinusExpression, argExpr);
            label = $"DateTime: '{memberName}(x)' → '{memberName}(-x)'";
        }

        var newArg = args[0].WithExpression(negated);
        var newArgs = node.ArgumentList.WithArguments(args.Replace(args[0], newArg));
        var replacement = node.WithArgumentList(newArgs);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = label,
            Type = Mutator.Statement,
        };
    }
}
