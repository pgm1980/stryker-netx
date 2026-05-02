using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.4.0 (Sprint 17): semantic-level pre-filter for the equivalent-mutant
/// pipeline. Uses Roslyn's <b>speculative-binding</b> API
/// (<c>SemanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression)</c>)
/// to ask "would the replacement bind successfully in the original semantic
/// context?" without rebuilding the <see cref="Compilation"/>. Catches
/// semantic errors that the v2.1.0 <see cref="RoslynDiagnosticsEquivalenceFilter"/>
/// (parser-only) misses — e.g. <c>ArgumentPropagationMutator</c> emitting an
/// arg-typed expression that fails type-checking against the call's
/// receiver type.
///
/// <para><b>Cost model.</b> Speculative-binding is O(1) per mutation in the
/// existing semantic context — Roslyn was designed for this exact use case
/// (IDE refactoring previews). Compare to the originally-scoped
/// SyntaxTree-substitution + <c>Compilation.AddSyntaxTrees</c> approach,
/// which would have been O(parse + bind) per mutation. The MVP here is
/// what made v2.4.0 inclusion viable.</para>
///
/// <para><b>Conservative scope.</b> Only acts on
/// <see cref="ExpressionSyntax"/>-typed replacements; statement-level and
/// declaration-level replacements would need the
/// <see cref="SemanticModel"/>'s <c>TryGetSpeculativeSemanticModel(...)</c>
/// overloads which are bulkier per call. Statement-level replacements
/// continue to be filtered by the v2.1 parser-only filter.</para>
///
/// <para>Always-on in the pipeline (no profile gate).</para>
/// </summary>
public sealed class RoslynSemanticDiagnosticsEquivalenceFilter : IEquivalentMutantFilter
{
    /// <inheritdoc />
    public string FilterId => "RoslynSemanticDiagnostics";

    /// <inheritdoc />
    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        if (semanticModel is null)
        {
            return false;
        }
        if (mutation.OriginalNode is null || mutation.ReplacementNode is null)
        {
            return false;
        }
        if (mutation.ReplacementNode is not ExpressionSyntax replacementExpression)
        {
            // Statement / declaration replacements need a different speculative API
            // (TryGetSpeculativeSemanticModel) which is bulkier per call. Stay conservative
            // and let the v2.1 parser-only filter handle structural validity for those.
            return false;
        }

        var position = mutation.OriginalNode.SpanStart;

        // Sprint 137 (v3.0.24): Roslyn's GetSpeculativeSymbolInfo crashes with NRE when binding
        // a MemberBindingExpression (the `.X` part of `obj?.X`) in isolation — the binder calls
        // FindConditionalAccessNodeForBinding which returns null and dereferences. Wrap in
        // try/catch to stay conservative (treat as non-equivalent — keep mutant).
        // Sprint 137: pre-check to skip MemberBindingExpression (`.X` part of `obj?.X`) — these
        // cannot be speculatively bound in isolation; Roslyn's binder dereferences a null in
        // FindConditionalAccessNodeForBinding. Treat as non-equivalent (conservative).
        if (replacementExpression is MemberBindingExpressionSyntax)
        {
            return false;
        }

        Microsoft.CodeAnalysis.SymbolInfo info;
        try
        {
            info = semanticModel.GetSpeculativeSymbolInfo(
                position,
                replacementExpression,
                SpeculativeBindingOption.BindAsExpression);
        }
#pragma warning disable S1696, CA1031 // Roslyn speculative-binding throws across many exception types — best-effort conservative fallback
        catch (Exception)
        {
            // Roslyn binder crashed on speculative binding (NRE in FindConditionalAccessNodeForBinding,
            // IOE for invalid context, etc) — can't determine equivalence, conservatively keep the mutant.
            return false;
        }
#pragma warning restore S1696, CA1031

        // CandidateReason != None means "binder tried but failed" — the most reliable
        // signal that the replacement is semantically invalid in this context. Symbol
        // == null with CandidateReason == None means "successfully bound to nothing
        // resolvable" (e.g. a literal expression), which is fine.
        return info.Symbol is null && info.CandidateReason != CandidateReason.None;
    }
}
