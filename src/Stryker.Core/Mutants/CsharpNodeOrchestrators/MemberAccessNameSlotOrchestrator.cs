using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Sprint 143 (ADR-027 Phase 1 — type-position-aware mutation control):
/// orchestrator for <see cref="SimpleNameSyntax"/> nodes that occupy the
/// <c>.Name</c> slot of a <see cref="MemberAccessExpressionSyntax"/>. That
/// slot is strict-typed (Roslyn refuses anything but
/// <see cref="SimpleNameSyntax"/>), so the <see cref="MutantPlacer"/>'s
/// <c>(MutantControl.IsActive(N) ? mutated : original)</c> envelope — a
/// <see cref="ParenthesizedExpressionSyntax"/> — must not be injected here.
/// <para>
/// This orchestrator enters
/// <see cref="MutationControl.MemberAccess"/> regardless of outer control,
/// which causes <see cref="MutationStore.Inject(ExpressionSyntax, ExpressionSyntax)"/>
/// to defer injection. Pending mutations then bubble up to the enclosing MA
/// on <see cref="MutationContext.Leave"/> and inject at the MA's
/// expression-level slot — a slot whose declared type accepts
/// <see cref="ExpressionSyntax"/> and so accepts the envelope.
/// </para>
/// <para>
/// Pairs with the parent-pivot inside
/// <see cref="Stryker.Core.Mutators.UoiMutator"/>: that mutator emits
/// <c>Mutation.OriginalNode = parent MA</c> when targeting an MA <c>.Name</c>
/// slot, keeping <c>(OriginalNode, ReplacementNode)</c> structurally valid
/// for <see cref="Helpers.RoslynHelper.InjectMutation{T}"/>.
/// </para>
/// <para>
/// <b>Phase-1 scope:</b> MA.Name only. The MB.Name twin
/// (<c>data?.Length</c>) still requires the legacy
/// <c>DoNotMutateOrchestrator&lt;SimpleNameSyntax&gt;</c> guard because a
/// pivot to MB lands in <c>ConditionalAccessExpression.WhenNotNull</c>, which
/// the binder rejects when not binding-led (<c>.</c> or <c>[</c>) and would
/// poison the whole-file compile. Phase 2 lifts the pivot to the enclosing
/// CAE.
/// </para>
/// </summary>
internal sealed class MemberAccessNameSlotOrchestrator : NodeSpecificOrchestrator<SimpleNameSyntax, ExpressionSyntax>
{
    protected override bool CanHandle(SimpleNameSyntax? t) => t is not null
        && t.Parent is MemberAccessExpressionSyntax memberAccess
        && memberAccess.Name == t;

    protected override MutationContext PrepareContext(SimpleNameSyntax node, MutationContext context) =>
        // Enter MemberAccess control so MutationStore.Inject (Expression-flavor) bails at this
        // level. The pending mutations propagate to the enclosing MA frame on Leave.
        base.PrepareContext(node, context.Enter(MutationControl.MemberAccess));

    protected override void RestoreContext(MutationContext context) => base.RestoreContext(context.Leave());
}
