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
/// v2.1.0 (Sprint 14, comparison.md §4.4 — declaration-site
/// <c>Span&lt;T&gt; ↔ ReadOnlySpan&lt;T&gt;</c> swap, the
/// declaration-site complement to v2.0.1's <see cref="AsSpanAsMemoryMutator"/>
/// which swaps invocation-site <c>AsSpan() ↔ AsMemory()</c>). Targets
/// <see cref="GenericNameSyntax"/> instances appearing in TypeSyntax
/// positions (parameter type, return type, variable declaration, field
/// type) for the four-member set: <c>Span ↔ ReadOnlySpan</c>,
/// <c>Memory ↔ ReadOnlyMemory</c>.
///
/// Compile-safety: not always. There is an implicit conversion from
/// <c>Span&lt;T&gt;</c> to <c>ReadOnlySpan&lt;T&gt;</c> (one-way), so
/// <c>Span&lt;T&gt; → ReadOnlySpan&lt;T&gt;</c> at a parameter position
/// often compiles but breaks if the body assigns into the span;
/// <c>ReadOnlySpan&lt;T&gt; → Span&lt;T&gt;</c> typically requires an
/// explicit cast at the call site that won't be present. The runner
/// classifies non-compiling mutants as killed.
///
/// <para><b>v3.1.2 (Sprint 142, Bug #9 from Calculator-tester report):</b>
/// disabled from all profiles via <see cref="MutationProfile.None"/>. The
/// mutator targets GenericNameSyntax exclusively in TypeSyntax positions
/// (parameter type, field type, etc.). The Conditional-Instrumentation
/// engine wraps every emitted mutation in a <c>ParenthesizedExpressionSyntax</c>
/// (the <c>(MutantControl.IsActive(N) ? mutated : original)</c> envelope),
/// which Roslyn's typed visitor refuses to accept in a TypeSyntax slot →
/// <c>InvalidCastException(ParenthesizedExpressionSyntax → TypeSyntax)</c>.
/// Re-enabling this mutator requires either (a) a type-position-aware
/// instrumentation variant in the engine, or (b) emitting the mutation
/// directly without conditional-control. Tracked in ADR-026.</para>
///
/// Profile membership: <see cref="MutationProfile.None"/> (was: All only).
/// </summary>
[MutationProfileMembership(MutationProfile.None)]
public sealed class SpanReadOnlySpanDeclarationMutator : MutatorBase<GenericNameSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    private static readonly ImmutableDictionary<string, string> Swaps =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Span"] = "ReadOnlySpan",
            ["ReadOnlySpan"] = "Span",
            ["Memory"] = "ReadOnlyMemory",
            ["ReadOnlyMemory"] = "Memory",
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public override IEnumerable<Mutation> ApplyMutations(GenericNameSyntax node, SemanticModel semanticModel)
    {
        var name = node.Identifier.Text;
        if (!Swaps.TryGetValue(name, out var swappedName))
        {
            yield break;
        }

        // Skip if the GenericName is in an invocation/expression position rather
        // than a TypeSyntax position — those are handled by other mutators
        // (AsSpanAsMemoryMutator). The discriminator: parent must be a
        // TypeSyntax-bearing slot (parameter, variable, return type, etc.).
        if (!IsInTypeSyntaxPosition(node))
        {
            yield break;
        }

        if (node.TypeArgumentList.Arguments.Count != 1)
        {
            yield break;
        }

        var newName = node.WithIdentifier(SyntaxFactory.Identifier(swappedName));

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = newName.WithCleanTriviaFrom(node),
            DisplayName = $"Span/Memory declaration: '{name}<T>' → '{swappedName}<T>'",
            Type = Mutator.Statement,
        };
    }

    private static bool IsInTypeSyntaxPosition(GenericNameSyntax node) =>
        node.Parent is ParameterSyntax
            or VariableDeclarationSyntax
            or PropertyDeclarationSyntax
            or MethodDeclarationSyntax
            or FieldDeclarationSyntax
            or LocalFunctionStatementSyntax
            or TypeArgumentListSyntax
            or ArrayTypeSyntax
            or NullableTypeSyntax
            or RefTypeSyntax;
}
