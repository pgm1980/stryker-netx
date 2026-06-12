using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Orchestrate mutations for expressions (and sub expressions).
/// </summary>
/// <typeparam name="T">Node specific type, must inherit <see cref="ExpressionSyntax"/>.</typeparam>
internal sealed class ExpressionSpecificOrchestrator<T> : NodeSpecificOrchestrator<T, ExpressionSyntax> where T : ExpressionSyntax
{
    /// <inheritdoc/>
    /// <remarks>Inject all pending mutations controlled with conditional operator(s).</remarks>
    protected override ExpressionSyntax InjectMutations(T sourceNode, ExpressionSyntax targetNode, SemanticModel semanticModel, MutationContext context) => context.InjectMutations(targetNode, sourceNode);

    protected override MutationContext StoreMutations(T node,
        IEnumerable<Mutant> mutations,
        MutationContext context) =>
         // if the expression contains a declaration, it must be controlled at the block level.
         // Sprint 180 (issue #284a): the same applies to expressions nested inside the pattern
         // of a variable-binding is-expression (e.g. the constant in 'o is { Length: 2 } s') —
         // an expression-level wrap would surface at the is-expression and duplicate the
         // designation in the same statement scope (CS0128).
         context.AddMutations(mutations,
             node.ContainsDeclarations() || node.IsInsideVariableBindingIsPattern()
                 ? MutationControl.Block
                 : MutationControl.Expression);

    protected override MutationContext PrepareContext(T node, MutationContext context) => base.PrepareContext(node, context.Enter(MutationControl.Expression));

    protected override void RestoreContext(MutationContext context) => base.RestoreContext(context.Leave());
}
