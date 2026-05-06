using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// ADR-027 (multi-sprint type-position-aware mutation control) orchestrator
/// for <see cref="SimpleNameSyntax"/> nodes that occupy a strict-typed
/// <c>.Name</c> slot — either <see cref="MemberAccessExpressionSyntax"/>
/// (Sprint 143 Phase 1) or <see cref="MemberBindingExpressionSyntax"/>
/// (Sprint 144 Phase 2). Both slots refuse anything but
/// <see cref="SimpleNameSyntax"/>, so the <see cref="MutantPlacer"/>'s
/// <c>(MutantControl.IsActive(N) ? mutated : original)</c> envelope — a
/// <see cref="ParenthesizedExpressionSyntax"/> — must not be injected here.
/// <para>
/// This orchestrator enters <see cref="MutationControl.MemberAccess"/>
/// regardless of outer control, which causes
/// <see cref="MutationStore.Inject(ExpressionSyntax, ExpressionSyntax)"/>
/// to defer injection. Pending mutations bubble up to the enclosing MA / MB /
/// CAE on <see cref="MutationContext.Leave"/> and inject at a level whose
/// declared slot type accepts <see cref="ExpressionSyntax"/> (so the envelope
/// fits structurally) and whose binder accepts an arbitrary expression
/// (so the post-/pre-fix wraps the full conditional access without breaking
/// the <c>WhenNotNull</c> binding-led requirement).
/// </para>
/// <para>
/// Pairs with the parent-pivot inside
/// <see cref="Stryker.Core.Mutators.UoiMutator"/>:
/// </para>
/// <list type="bullet">
///   <item>MA.Name target -> <c>OriginalNode = parent MA</c>;</item>
///   <item>MB.Name target -> <c>OriginalNode = parent MB</c>, then walked up
///   out of every enclosing <c>CAE.WhenNotNull</c> to the outermost CAE in
///   the <c>?.</c>-chain;</item>
///   <item>MA target whose own ancestors land it in <c>CAE.WhenNotNull</c>
///   -> walked up the same way.</item>
/// </list>
/// <para>
/// All three paths converge on a pivot whose containing slot is loose
/// (Expression-typed) and whose binder is happy with an arbitrary expression.
/// </para>
/// </summary>
internal sealed class MemberAccessNameSlotOrchestrator : NodeSpecificOrchestrator<SimpleNameSyntax, ExpressionSyntax>
{
    protected override bool CanHandle(SimpleNameSyntax? t) => t is not null
        && ((t.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == t)
            || (t.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == t));

    protected override MutationContext PrepareContext(SimpleNameSyntax node, MutationContext context) =>
        // Enter MemberAccess control so MutationStore.Inject (Expression-flavor) bails at this
        // level. The pending mutations propagate to the enclosing MA / MB / CAE frame on Leave.
        base.PrepareContext(node, context.Enter(MutationControl.MemberAccess));

    protected override void RestoreContext(MutationContext context) => base.RestoreContext(context.Leave());
}
