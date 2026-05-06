using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 10, comparison.md §4.1 — PIT UOI = Unary Operator
/// Insertion): wraps an identifier in increment/decrement so a use-of-x
/// becomes a use-and-mutate-x. <c>x</c> -> <c>x++</c>, <c>x</c> -> <c>++x</c>,
/// <c>x</c> -> <c>x--</c>, <c>x</c> -> <c>--x</c>. Targets bare identifier
/// references in expression position; conservatively skips contexts where
/// the assignment-side change would be syntactically invalid (lvalue
/// requirements, ref-passing, etc.).
///
/// Profile membership: All only — UOI is famously aggressive (every
/// identifier use becomes 4 mutations); opt-in via --profile All.
///
/// <para><b>v3.2.0-dev (Sprint 143, ADR-027 Phase 1 — type-position-aware
/// pivot for MemberAccess.Name):</b> when the visited identifier lives in a
/// MemberAccess.Name slot (typed <see cref="SimpleNameSyntax"/> in Roslyn),
/// a <c>PostfixUnary(IdentifierName)</c> replacement is structurally invalid
/// in that slot — the typed visitor refuses anything but a SimpleName, and the
/// <c>ConditionalInstrumentationEngine</c> wraps every mutation in a
/// <c>ParenthesizedExpression</c> envelope, which inherits the same strict
/// slot and crashes (Bug-9). Phase 1 pivots the mutation up to the enclosing
/// <see cref="MemberAccessExpressionSyntax"/> so the post-/pre-fix wraps the
/// full member-access expression: <c>data.Length</c> -> <c>data.Length++</c>.
/// This replaces the Sprint 142 hotfix (skip + global
/// <c>DoNotMutateOrchestrator&lt;SimpleNameSyntax&gt;</c>) for the MA.Name
/// case while restoring UOI coverage there.</para>
///
/// <para><b>Out of Phase-1 scope (deferred to ADR-027 Phase 2 — CAE-aware
/// lifting):</b> the symmetric case for MemberBinding.Name (<c>data?.Length</c>)
/// is still skipped via <see cref="IsSafeToWrap"/>. Pivoting to the enclosing
/// MB and emitting <c>PostfixUnary(MB)</c> structurally produces a
/// <c>ConditionalAccessExpression.WhenNotNull</c> that no longer starts with
/// a binding operator (<c>.</c> or <c>[</c>), which Roslyn's binder rejects
/// (yields a compile error on the entire mutated file, taking out unrelated
/// mutations). Phase 2 will lift these all the way to the enclosing CAE so
/// the post-/pre-fix wraps the full conditional access. Until then, MB.Name
/// stays skipped to keep the file-compile clean.</para>
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class UoiMutator : MutatorBase<IdentifierNameSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(IdentifierNameSyntax node, SemanticModel semanticModel)
    {
        // Conservative scope: only mutate when the identifier appears in a
        // simple expression position. Skip when the node is the target of
        // assignment, an invocation, or already inside an increment expression
        // (avoid double-wrapping). MA.Name is NOT skipped — Sprint 143
        // (ADR-027 Phase 1) handles it via parent-pivot below.
        if (!IsSafeToWrap(node))
        {
            yield break;
        }

        // Sprint 143 (ADR-027 Phase 1): MA.Name pivot. Pivoting up to the
        // enclosing MA puts (OriginalNode, ReplacementNode) in a slot whose
        // declared type accepts ExpressionSyntax (and so accepts the engine's
        // ParenthesizedExpression envelope). MB.Name (conditional access) is
        // not pivoted here — see Phase 2 in the type doc-comment.
        ExpressionSyntax pivot = node;
        if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node)
        {
            pivot = memberAccess;
        }

        yield return WrapIn(pivot, node, SyntaxKind.PostIncrementExpression, "x++");
        yield return WrapIn(pivot, node, SyntaxKind.PreIncrementExpression, "++x");
        yield return WrapIn(pivot, node, SyntaxKind.PostDecrementExpression, "x--");
        yield return WrapIn(pivot, node, SyntaxKind.PreDecrementExpression, "--x");
    }

    private static Mutation WrapIn(ExpressionSyntax pivot, IdentifierNameSyntax labelSource, SyntaxKind kind, string label)
    {
        ExpressionSyntax replacement = kind is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
            ? SyntaxFactory.PostfixUnaryExpression(kind, pivot)
            : SyntaxFactory.PrefixUnaryExpression(kind, pivot);
        return new Mutation
        {
            OriginalNode = pivot,
            ReplacementNode = replacement.WithCleanTriviaFrom(pivot),
            DisplayName = $"UOI: '{labelSource.Identifier.Text}' -> '{label}'",
            Type = Mutator.Update,
        };
    }

    private static bool IsSafeToWrap(IdentifierNameSyntax node)
    {
        var parent = node.Parent;

        // Skip if the identifier is itself the operand of an existing increment.
        if (parent is PrefixUnaryExpressionSyntax pre &&
            pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
        {
            return false;
        }
        if (parent is PostfixUnaryExpressionSyntax post &&
            post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression)
        {
            return false;
        }

        // Skip if the identifier is the LHS of an assignment.
        if (parent is AssignmentExpressionSyntax assign && assign.Left == node)
        {
            return false;
        }

        // Skip member-access heads (Expression-side) / invocation targets — wrapping
        // changes call semantics. The right-hand MA.Name is NOT skipped here:
        // Sprint 143 pivots the mutation up to the parent MA. MB.Name IS still
        // skipped — Phase 2 will lift it to the enclosing CAE (see type doc).
        if (parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == node)
        {
            return false;
        }
        if (parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == node)
        {
            return false;
        }
        if (parent is InvocationExpressionSyntax inv && inv.Expression == node)
        {
            return false;
        }

        // Skip when used as a ref/out argument — would not compile.
        if (parent is ArgumentSyntax arg && (arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) ||
                                              arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)))
        {
            return false;
        }

        // Sprint 23: skip identifiers that live in a NameSyntax-typed slot
        // (QualifiedName like `Sample.Library` in namespace decls / using directives /
        // type references; AliasQualifiedName like `global::Foo`). The conditional
        // placer wraps every mutation in `(MutantControl.IsActive(N) ? mutated : original)`
        // — a ParenthesizedExpressionSyntax. Roslyn's QualifiedNameSyntax visitor
        // casts both children to NameSyntax and crashes on the parens.
        // (MemberAccess.Name used to be in this list, but Sprint 143 handles it
        // via parent-pivot — see ApplyMutations above. MB.Name remains skipped.)
        if (parent is QualifiedNameSyntax or AliasQualifiedNameSyntax)
        {
            return false;
        }

        return true;
    }
}
