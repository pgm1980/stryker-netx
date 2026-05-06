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
/// <c>InvalidCastException(ParenthesizedExpressionSyntax → TypeSyntax)</c>.</para>
///
/// <para><b>v3.2.0 (Sprint 145, ADR-027 Phase 3 closure):</b>
/// <see cref="MutationProfile.None"/> is now finalized as the architectural
/// design for this mutator, not a temporary mitigation. Maxential cost/benefit
/// (11 Schritte, 3 engine-refactor alternatives evaluated) concluded that
/// re-enabling Span↔ReadOnlySpan / Memory↔ReadOnlyMemory swaps would require
/// either (a) a TypeReplacementInstrumentationEngine plus a pipeline-level
/// separate-compile-per-mutation mode (4+ sprints for one niche mutator), or
/// (b) preprocessor-direktiven envelope (5+ sprints, very high risk). Neither
/// option is justified by the user-value: this mutator targets a single edge
/// case (Span/Memory pairs) which produces compile-pass mutants when the body
/// only reads the span (effectively no-op mutation, useless for test scoring)
/// and compile-fail mutants when the body writes (which the standard runtime
/// pipeline already classifies as killed without needing this engine). The
/// skip is therefore the final design. If a future user need motivates the
/// engine work, it would warrant its own multi-sprint v3.x release with
/// updated ADR.</para>
///
/// Profile membership: <see cref="MutationProfile.None"/> (final).
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
