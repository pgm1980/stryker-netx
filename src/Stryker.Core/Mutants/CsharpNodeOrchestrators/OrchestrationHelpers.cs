using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Stryker.Core.Helpers;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Sprint 151 (ADR-032, Bug #9 systemic audit from Calculator-Tester Bug-Report 5):
/// extends the Sprint-147 <see cref="SyntaxSlotValidator"/> coverage from the
/// injection-pipeline (where it already protects <c>MutationStore.Inject</c> overloads)
/// into the <b>orchestration</b> phase. The historical assumption — that the
/// injection-time validator was a complete safety net — was wrong: the actual
/// crash site for the Bug-Report-5 failure pattern (<c>ParenthesizedExpressionSyntax
/// → IdentifierNameSyntax</c>) is <c>NodeSpecificOrchestrator.OrchestrateChildrenMutation</c>'s
/// call to Roslyn's <c>ReplaceNodes</c>, which runs BEFORE injection.
///
/// <para>
/// User-Forderung (Bug-Report 5, Punkt e — verschärft): "Eine projektweite Suche
/// nach allen impliziten oder expliziten Casts in Mutator-Code-Pfaden, die einen
/// Syntax-Knoten in einen spezifischeren Subtyp casten." The audit (see ADR-032)
/// found that the unsafe casts are not in <i>our</i> mutator code but inside Roslyn's
/// own <c>ReplaceNodes</c>-driven typed-visitor cascade — exactly the surface
/// covered by <see cref="SyntaxSlotValidator.TryReplaceWithValidation{T}"/>. This helper
/// applies that validation per-child before the bulk <c>ReplaceNodes</c> call, so a
/// single slot-incompatible child mutation does not abort the entire orchestration.
/// </para>
///
/// <para>
/// Coverage strategy:
/// <list type="number">
///   <item><description><b>Per-child validation</b>: each candidate child mutation is
///   tested against the parent via <see cref="SyntaxSlotValidator.TryReplaceWithValidation{T}"/>.
///   On failure, the candidate is dropped and the original child is preserved.</description></item>
///   <item><description><b>Final safety-net</b>: even after per-child validation, a try/catch
///   around the bulk <c>ReplaceNodes</c> handles the rare case where individually-valid
///   replacements interact in unforeseen ways. On crash, all child mutations are dropped
///   and the orchestration continues with the unmodified parent.</description></item>
/// </list>
/// Both layers log a diagnostic so reporters can classify the dropped mutations as
/// <see cref="Stryker.Abstractions.MutantStatus.CompileError"/> or equivalent.
/// </para>
/// </summary>
internal static partial class OrchestrationHelpers
{
    private static readonly ILogger Logger = ApplicationLogging.LoggerFactory.CreateLogger("Stryker.Core.Mutants.CsharpNodeOrchestrators.OrchestrationHelpers");

    /// <summary>
    /// Per-child slot-validated equivalent of <c>node.ReplaceNodes(children, computeReplacementNode)</c>.
    /// Each candidate replacement is validated against the parent via the
    /// <see cref="SyntaxSlotValidator"/>. Slot-incompatible candidates are silently dropped.
    /// </summary>
    /// <typeparam name="TParent">Roslyn parent-node type.</typeparam>
    /// <param name="node">The parent node whose children are being mutated.</param>
    /// <param name="children">Children to consider for replacement (typically <c>node.ChildNodes()</c>).</param>
    /// <param name="computeReplacementNode">Mutation function — receives the original child, returns the
    /// (possibly mutated) replacement. Returning the same instance signals "no mutation".</param>
    /// <returns>The parent with all valid child mutations applied; or <paramref name="node"/> unchanged
    /// if no valid mutations are produced (or the bulk replacement fails as a final safety net).</returns>
    public static TParent ReplaceChildrenValidated<TParent>(
        TParent node,
        IEnumerable<SyntaxNode> children,
        Func<SyntaxNode, SyntaxNode> computeReplacementNode)
        where TParent : SyntaxNode
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(computeReplacementNode);

        var validated = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
        foreach (var child in children)
        {
            var mutated = computeReplacementNode(child);
            if (ReferenceEquals(mutated, child))
            {
                continue; // no mutation produced for this child
            }
            if (SyntaxSlotValidator.TryReplaceWithValidation<TParent>(node, child, mutated, out _, out var error))
            {
                validated[child] = mutated;
            }
            else if (Logger.IsEnabled(LogLevel.Debug))
            {
                // CA1873 false-positive: the IsEnabled(Debug) guard above ensures Kind() is
                // only invoked when Debug logging is actually consumed. The analyzer can't
                // see across the guard / source-gen boundary.
#pragma warning disable CA1873
                LogPerChildRejected(Logger, child.Kind(), mutated.Kind(), error ?? "(no diagnostic)");
#pragma warning restore CA1873
            }
        }

        if (validated.Count == 0)
        {
            return node;
        }

        try
        {
            return node.ReplaceNodes(validated.Keys, (orig, _) => validated[orig]);
        }
        catch (Exception ex) when (ex is InvalidCastException or NullReferenceException or InvalidOperationException)
        {
            LogBulkReplaceCrashed(Logger, ex);
            return node;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Orchestration validator rejected child mutation: {OriginalKind} -> {ReplacementKind}: {ValidationError}")]
    private static partial void LogPerChildRejected(ILogger logger, SyntaxKind originalKind, SyntaxKind replacementKind, string validationError);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Orchestration bulk-replace fell back to unmutated parent.")]
    private static partial void LogBulkReplaceCrashed(ILogger logger, Exception ex);
}
