using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

internal sealed class InvocationExpressionOrchestrator : MemberAccessExpressionOrchestrator<InvocationExpressionSyntax>
{

    protected override MutationContext StoreMutations(InvocationExpressionSyntax node,
        IEnumerable<Mutant> mutations,
        MutationContext context) =>
         // if the invocation contains a declaration, it must be controlled at the block level.
         context.AddMutations(mutations, node.ArgumentList.ContainsDeclarations() ? MutationControl.Block : MutationControl.Expression);

    // Sprint 151 (ADR-032): routed through OrchestrationHelpers.ReplaceChildrenValidated
    // for per-child slot-validation. The InvocationExpression branch with member-access
    // chains was a frequent crash source per Bug-Report 5 (Calculator.Infrastructure
    // ParenthesizedExpressionSyntax → IdentifierNameSyntax cast).
    protected override ExpressionSyntax OrchestrateChildrenMutation(InvocationExpressionSyntax node, SemanticModel semanticModel,
        MutationContext context) =>
        OrchestrationHelpers.ReplaceChildrenValidated(node, node.ChildNodes(),
            original =>
            {
                if (original == node.Expression)
                {
                    // we cannot mutate only the invoked method name, mutations must be controlled at the expression level
                    var subContext = context.Enter(MutationControl.MemberAccess);
                    var result = subContext.Mutate(original, semanticModel);
                    subContext.Leave();
                    return result;
                }
                else
                {
                    //The argument list can be freely mutated,
                    var subContext = context.Enter(MutationControl.Member);
                    var result = subContext.Mutate(original, semanticModel);
                    subContext.Leave();
                    return result;
                }
            });
}
