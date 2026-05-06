using System;
using System.Linq;
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
/// <para>
/// v3.2.9 (Sprint 155 / ADR-037): coverage extended from
/// <see cref="ExpressionSyntax"/> to <see cref="StatementSyntax"/> via
/// <c>SemanticModel.TryGetSpeculativeSemanticModel(int, StatementSyntax, out SemanticModel)</c>.
/// Statement-level mutations (e.g. <c>StatementMutator</c>'s return-statement
/// rewrites, <c>BlockMutator</c>'s while-true → while-false) now get semantic
/// pre-filtering instead of relying solely on the parser-only v2.1 filter.
/// Closes the Sprint-16-deferred extension item.
/// </para>
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

        return mutation.ReplacementNode switch
        {
            ExpressionSyntax expression => IsEquivalentExpression(mutation, semanticModel, expression),
            StatementSyntax statement => IsEquivalentStatement(mutation, semanticModel, statement),
            // Declaration-level replacements (e.g. MethodBodyReplacement) would need
            // a third speculative path; conservatively defer to v2.1 parser-only filter.
            _ => false,
        };
    }

    /// <summary>
    /// Sprint 17 (v2.4.0) expression path — speculative symbol-binding. Symbol == null
    /// with CandidateReason != None means "binder tried but failed".
    /// </summary>
    private static bool IsEquivalentExpression(Mutation mutation, SemanticModel semanticModel, ExpressionSyntax replacementExpression)
    {
        // Sprint 137 (v3.0.24): Roslyn's GetSpeculativeSymbolInfo crashes with NRE when binding
        // a MemberBindingExpression (the `.X` part of `obj?.X`) in isolation — the binder calls
        // FindConditionalAccessNodeForBinding which returns null and dereferences. Pre-check skips
        // these; treat as non-equivalent (conservative).
        if (replacementExpression is MemberBindingExpressionSyntax)
        {
            return false;
        }

        var position = mutation.OriginalNode!.SpanStart;
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

    /// <summary>
    /// Sprint 155 (v3.2.9, ADR-037) statement path — uses
    /// <c>SemanticModel.TryGetSpeculativeSemanticModel(int, StatementSyntax, out SemanticModel)</c>
    /// to construct a speculative semantic model rooted at the replacement statement,
    /// then walks the statement's <see cref="ExpressionSyntax"/> descendants and queries
    /// <c>GetSymbolInfo</c> on each. Speculative models do not support
    /// <c>GetDiagnostics()</c> (it throws <see cref="NotSupportedException"/>), but
    /// per-descendant <c>GetSymbolInfo</c> works and surfaces unbound references the
    /// same way as the expression-path. Catches e.g. <c>BlockMutator</c> wrapping a
    /// return-bearing block in <c>if (false) {…}</c> where the body still references
    /// names that are out-of-scope in the speculative position.
    /// </summary>
    private static bool IsEquivalentStatement(Mutation mutation, SemanticModel semanticModel, StatementSyntax replacementStatement)
    {
        var position = mutation.OriginalNode!.SpanStart;
        SemanticModel? speculativeModel;
        try
        {
            if (!semanticModel.TryGetSpeculativeSemanticModel(position, replacementStatement, out speculativeModel) || speculativeModel is null)
            {
                // Roslyn refused to construct a speculative model for this position +
                // statement combination. Conservative: keep the mutant.
                return false;
            }
        }
#pragma warning disable S1696, CA1031 // see expression-path comment
        catch (Exception)
        {
            return false;
        }
#pragma warning restore S1696, CA1031

        // Speculative models throw NotSupportedException on GetDiagnostics(); use
        // per-descendant GetSymbolInfo instead. Same signal as the expression-path:
        // Symbol == null + CandidateReason != None means "binder tried but failed".
        foreach (var expr in replacementStatement.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
        {
            // Sprint 137 known crash class — same pre-check as the expression-path.
            if (expr is MemberBindingExpressionSyntax)
            {
                continue;
            }
            try
            {
                var info = speculativeModel.GetSymbolInfo(expr);
                if (info.Symbol is null && info.CandidateReason != CandidateReason.None)
                {
                    return true; // semantic invalid → filter as equivalent
                }
            }
#pragma warning disable S1696, CA1031, RCS1075 // best-effort — Roslyn binder edge cases; empty catch is intentional skip
            catch (Exception)
            {
                // Skip this descendant; continue walk. Conservative if we end with no errors.
            }
#pragma warning restore S1696, CA1031, RCS1075
        }

        return false;
    }
}
