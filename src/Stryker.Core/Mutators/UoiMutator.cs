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
/// <para><b>v3.2.0-dev (ADR-027 — type-position-aware mutation control,
/// multi-sprint refactor superseding the Sprint 142 hotfix). Phase 1 (Sprint
/// 143):</b> when the visited identifier lives in a <see cref="MemberAccessExpressionSyntax"/>
/// <c>.Name</c> slot (typed <see cref="SimpleNameSyntax"/>), a
/// <c>PostfixUnary(IdentifierName)</c> replacement is structurally invalid
/// for Roslyn's typed visitor. Pivot to the enclosing MA so the post-/pre-fix
/// wraps the full member-access expression — <c>data.Length</c> -> <c>data.Length++</c>.</para>
///
/// <para><b>Phase 2 (Sprint 144):</b> CAE-aware extension of the pivot.
/// (a) <see cref="MemberBindingExpressionSyntax"/> <c>.Name</c> slots
/// (<c>data?.Length</c>) and MA-targets that themselves sit in a
/// <see cref="ConditionalAccessExpressionSyntax"/>'s <c>WhenNotNull</c>
/// subtree (<c>data?.Foo.Length</c>) cannot pivot to MB or to the inner MA —
/// the resulting tree puts a non-binding-led expression at <c>WhenNotNull</c>,
/// which Roslyn's binder rejects (whole-file compile poisoning, observed in
/// the local Sprint-143 bisect). The pivot now <i>walks up</i> through every
/// enclosing <c>CAE.WhenNotNull</c> to land on the outermost CAE in the
/// <c>?.</c>-chain — <c>data?.Length</c> -> <c>data?.Length++</c> as
/// <c>PostfixUnary(CAE)</c>; <c>a?.b?.c.d</c> on <c>d</c> -> outermost CAE.
/// (b) <see cref="IdentifierNameSyntax"/> in TypeSyntax-typed slots (parameter
/// type, return type, property type, field type, generic argument, array
/// element, nullable target, ref-type target, base-type, cast target) is
/// skipped — the same crash class that ADR-026 mitigated for
/// <see cref="SpanReadOnlySpanDeclarationMutator"/> (ParenthesizedExpression
/// envelope into TypeSyntax slot). Re-enable tracked in Phase 3 alongside the
/// <c>SpanReadOnly</c> work.</para>
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class UoiMutator : MutatorBase<IdentifierNameSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(IdentifierNameSyntax node, SemanticModel semanticModel)
    {
        // Conservative scope: only mutate when the identifier appears in a
        // simple expression position. Skip on assignment LHS, invocation targets,
        // existing increments, ref/out args, NameSyntax-typed slots
        // (QualifiedName / AliasQualifiedName), and TypeSyntax-typed slots.
        if (!IsSafeToWrap(node))
        {
            yield break;
        }

        // Sprint 143 + 144 (ADR-027 Phase 1 + 2): determine the pivot target.
        //   Phase 1: MA.Name -> parent MA (Length in data.Length -> data.Length).
        //   Phase 2 (a): MB.Name -> parent MB, then walk up out of CAE.WhenNotNull
        //   to the outermost CAE in the ?.-chain.
        // Pivoting puts (OriginalNode, ReplacementNode) into a slot whose declared
        // type accepts ExpressionSyntax (so the engine's ParenthesizedExpression
        // envelope fits) AND whose binder accepts an arbitrary expression
        // (so we don't poison the whole-file compile via a non-binding-led
        // CAE.WhenNotNull).
        ExpressionSyntax pivot = node;
        if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node)
        {
            pivot = memberAccess;
        }
        else if (node.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == node)
        {
            pivot = memberBinding;
        }

        pivot = LiftPastConditionalAccess(pivot);

        yield return WrapIn(pivot, node, SyntaxKind.PostIncrementExpression, "x++");
        yield return WrapIn(pivot, node, SyntaxKind.PreIncrementExpression, "++x");
        yield return WrapIn(pivot, node, SyntaxKind.PostDecrementExpression, "x--");
        yield return WrapIn(pivot, node, SyntaxKind.PreDecrementExpression, "--x");
    }

    /// <summary>
    /// Sprint 144 (ADR-027 Phase 2): lift the pivot up while it still sits
    /// inside a <see cref="ConditionalAccessExpressionSyntax.WhenNotNull"/>
    /// subtree. CAE.WhenNotNull demands a binding-led expression
    /// (start with <c>.</c> or <c>[</c>), which neither
    /// <c>PostfixUnary(MB)</c> nor <c>PostfixUnary(MA)</c> satisfies. Walking
    /// up to the enclosing CAE makes the post-/pre-fix wrap the full
    /// conditional access — <c>(data?.Length)++</c> as the tree shape,
    /// rendered as <c>data?.Length++</c> — which compiles and binds.
    /// Repeats for nested CAE chains (<c>a?.b?.c.d</c>) until the outermost.
    /// </summary>
    private static ExpressionSyntax LiftPastConditionalAccess(ExpressionSyntax pivot)
    {
        while (true)
        {
            var enclosingCae = FindEnclosingCaeViaWhenNotNull(pivot);
            if (enclosingCae is null)
            {
                return pivot;
            }
            pivot = enclosingCae;
        }
    }

    private static ConditionalAccessExpressionSyntax? FindEnclosingCaeViaWhenNotNull(ExpressionSyntax node)
    {
        // Walk up the ancestor chain. Return the first CAE we cross from its
        // WhenNotNull side. CAEs we cross from the Expression side are
        // transparent — they bound a sibling subtree, not ours, so we keep
        // walking. Practical: in `matrix?.GetType().Name?.Length`, the
        // MB(.GetType) sits inside MA.Expression -> CAE2.Expression -> CAE1.WhenNotNull,
        // so the first WhenNotNull-side crossing is at CAE1, not CAE2.
        SyntaxNode walker = node;
        while (walker.Parent is not null)
        {
            if (walker.Parent is ConditionalAccessExpressionSyntax cae && cae.WhenNotNull == walker)
            {
                return cae;
            }
            walker = walker.Parent;
        }
        return null;
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
        // Each helper covers a class of unsafe context. Returning false from
        // any of them disables the mutation. Order is not significant — the
        // checks are independent.
        return !IsExistingIncrementOperand(node)
            && !IsAssignmentLhs(node)
            && !IsMemberAccessHeadOrInvocationTarget(node)
            && !IsRefOrOutArgument(node)
            && !IsInNameSyntaxSlot(node)
            && !IsInTypeSyntaxPosition(node);
    }

    private static bool IsExistingIncrementOperand(IdentifierNameSyntax node) =>
        (node.Parent is PrefixUnaryExpressionSyntax pre &&
         pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
        || (node.Parent is PostfixUnaryExpressionSyntax post &&
            post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression);

    private static bool IsAssignmentLhs(IdentifierNameSyntax node) =>
        node.Parent is AssignmentExpressionSyntax assign && assign.Left == node;

    /// <summary>
    /// Member-access head (Expression-side) and invocation target — wrapping
    /// would change call semantics. MA.Name and MB.Name are NOT covered here;
    /// Sprint 143 + 144 pivot those targets up (MA -> MA, MB -> enclosing CAE).
    /// </summary>
    private static bool IsMemberAccessHeadOrInvocationTarget(IdentifierNameSyntax node) =>
        (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == node)
        || (node.Parent is InvocationExpressionSyntax inv && inv.Expression == node);

    private static bool IsRefOrOutArgument(IdentifierNameSyntax node) =>
        node.Parent is ArgumentSyntax arg
        && (arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
            || arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

    /// <summary>
    /// Sprint 23: NameSyntax-typed slots in <see cref="QualifiedNameSyntax"/> /
    /// <see cref="AliasQualifiedNameSyntax"/> (namespace declarations, using
    /// directives, type references). Roslyn's typed visitor refuses
    /// <see cref="ParenthesizedExpressionSyntax"/> in those slots.
    /// </summary>
    private static bool IsInNameSyntaxSlot(IdentifierNameSyntax node) =>
        node.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax;

    private static bool IsInTypeSyntaxPosition(IdentifierNameSyntax node)
    {
        // Walk up while parent is itself a TypeSyntax wrapper (ArrayType,
        // NullableType, RefType, GenericName-as-base etc.). The topmost
        // TypeSyntax ancestor is what occupies the consuming slot.
        SyntaxNode current = node;
        while (current.Parent is TypeSyntax)
        {
            current = current.Parent;
        }
        return current.Parent switch
        {
            ParameterSyntax p => p.Type == current,
            VariableDeclarationSyntax v => v.Type == current,
            PropertyDeclarationSyntax pd => pd.Type == current,
            MethodDeclarationSyntax md => md.ReturnType == current,
            LocalFunctionStatementSyntax lf => lf.ReturnType == current,
            TypeArgumentListSyntax => true,
            ArrayTypeSyntax at => at.ElementType == current,
            NullableTypeSyntax nt => nt.ElementType == current,
            RefTypeSyntax rt => rt.Type == current,
            BaseTypeSyntax bt => bt.Type == current,
            CastExpressionSyntax ce => ce.Type == current,
            TypeOfExpressionSyntax to => to.Type == current,
            DefaultExpressionSyntax de => de.Type == current,
            SizeOfExpressionSyntax so => so.Type == current,
            ObjectCreationExpressionSyntax oc => oc.Type == current,
            _ => false,
        };
    }
}
