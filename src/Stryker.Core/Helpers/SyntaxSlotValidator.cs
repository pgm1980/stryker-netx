using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stryker.Core.Helpers;

/// <summary>
/// Sprint 147 (ADR-028, Bug #9 architectural fix from Calculator-Tester Bug-Report 4):
/// central validation layer that detects syntax-tree-shape incompatibilities BEFORE the
/// mutated tree is handed to subsequent Roslyn typed visitors. The historical Bug #9
/// crash class — <c>ParenthesizedExpressionSyntax</c> envelope landing in a strict-typed
/// <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax"/>,
/// <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.SimpleNameSyntax"/> or
/// <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax"/> slot — manifests at
/// arbitrary downstream typed-visitor sites (manifesting as either
/// <see cref="InvalidCastException"/> or <see cref="NullReferenceException"/> depending on
/// which property the typed visitor accesses).
///
/// <para>
/// User-Forderung c) (Bug-Report 4): "Validierungs-Layer vor der Mutation. Eine zentrale
/// Stelle in der Pipeline, die jede beabsichtigte Mutation auf Syntax-Konsistenz prüft,
/// bevor sie auf den Syntax-Tree angewandt wird." This class implements that layer.
/// </para>
///
/// <para>
/// The strategy is a <b>force-traverse safety-net</b>: after applying the candidate
/// replacement, we walk the descendants of the result via Roslyn's
/// <c>DescendantNodesAndSelf()</c> (which exercises the typed visitor cascade).
/// If a slot-type mismatch exists, the visitor throws — we catch and signal
/// the validation failure to the caller, which then drops the mutation cleanly instead
/// of crashing the whole pipeline. This is the defense-in-depth complement to the
/// per-mutator skip lists (e.g.,
/// <see cref="Stryker.Core.Mutators.UoiMutator.IsInTypeSyntaxPosition"/>): the skip
/// lists handle known patterns at the source, the validator catches the unknown.
/// </para>
///
/// <para>
/// The performance cost is one descendant-walk per mutation injection. This is acceptable
/// because (a) most mutations are at expression-level where the tree-walk is shallow, and
/// (b) the alternative (uncaught crashes) is a complete-pipeline-abort which is far
/// worse for the Calculator-Tester's All-Profile test runs.
/// </para>
/// </summary>
internal static class SyntaxSlotValidator
{
    /// <summary>
    /// Attempts a node replacement with downstream-traversal validation. Returns
    /// <see langword="true"/> if the replacement produces a structurally valid tree
    /// (no <see cref="InvalidCastException"/> / <see cref="NullReferenceException"/>
    /// during the descendant walk); <see langword="false"/> otherwise.
    /// </summary>
    /// <typeparam name="T">Type of the host node.</typeparam>
    /// <param name="sourceNode">The host node. Must contain <paramref name="originalNode"/>.</param>
    /// <param name="originalNode">The node within <paramref name="sourceNode"/> to be replaced.</param>
    /// <param name="replacementNode">The replacement node.</param>
    /// <param name="result">When validation succeeds, the replaced tree. Otherwise, the original
    /// <paramref name="sourceNode"/> unchanged.</param>
    /// <param name="validationError">When validation fails, a diagnostic describing the slot mismatch.</param>
    /// <returns><see langword="true"/> when the replacement is structurally valid; otherwise <see langword="false"/>.</returns>
    public static bool TryReplaceWithValidation<T>(
        T sourceNode,
        SyntaxNode originalNode,
        SyntaxNode replacementNode,
        out T result,
        out string? validationError)
        where T : SyntaxNode
    {
        result = sourceNode;
        validationError = null;

        T candidate;
        try
        {
            candidate = sourceNode.ReplaceNode(originalNode, replacementNode);
        }
        catch (Exception ex) when (ex is InvalidCastException or NullReferenceException or InvalidOperationException)
        {
            validationError = FormatValidationError("ReplaceNode failed", originalNode, replacementNode, ex);
            return false;
        }

        // Force-traverse the candidate via Roslyn's typed visitor cascade. If the slot
        // shape is broken, the visitor throws — we catch and report the failure. The
        // ToList() materialises the IEnumerable so the visitor exceptions surface
        // here rather than in some downstream consumer.
        try
        {
            _ = candidate.DescendantNodesAndSelf().ToList();
        }
        catch (Exception ex) when (ex is InvalidCastException or NullReferenceException)
        {
            validationError = FormatValidationError("Downstream visitor traversal failed", originalNode, replacementNode, ex);
            return false;
        }

        result = candidate;
        return true;
    }

    private static string FormatValidationError(
        string stage,
        SyntaxNode originalNode,
        SyntaxNode replacementNode,
        Exception ex)
    {
        var parentKind = originalNode.Parent?.Kind().ToString() ?? "(no parent)";
        return $"Slot-incompatible mutation rejected ({stage}): "
            + $"replacing {originalNode.Kind()} with {replacementNode.GetType().Name}({replacementNode.Kind()}) "
            + $"under parent {parentKind} caused {ex.GetType().Name}: {ex.Message}";
    }
}
