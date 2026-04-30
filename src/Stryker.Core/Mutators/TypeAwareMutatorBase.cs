using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (ADR-015): base class for mutators that REQUIRE a working
/// <see cref="SemanticModel"/> to produce their substitutions. Unlike
/// <see cref="MutatorBase{T}"/>, this base implements
/// <see cref="ITypeAwareMutator"/> so the orchestrator (or test runner)
/// can opt-out when no semantic model is available — typically because
/// the project failed to compile, in which case running type-aware
/// mutators would throw rather than silently produce nothing.
///
/// Sub-classes get convenience helpers <see cref="GetExpressionType"/>
/// and <see cref="GetReturnType"/> for the most common SemanticModel
/// look-ups in mutation logic.
/// </summary>
public abstract class TypeAwareMutatorBase<T> : ITypeAwareMutator
    where T : SyntaxNode
{
    /// <summary>The mutation-level threshold under which this mutator is active.</summary>
    public abstract MutationLevel MutationLevel { get; }

    /// <summary>Type-aware mutation entry point — receives a non-null semantic model.</summary>
    public abstract IEnumerable<Mutation> ApplyMutations(T node, SemanticModel semanticModel);

    /// <inheritdoc />
    public IEnumerable<Mutation> Mutate(SyntaxNode node, SemanticModel semanticModel, IStrykerOptions options)
    {
        if (semanticModel is null)
        {
            // No semantic model => bail out silently. Type-aware mutators are
            // intentionally a no-op when type information is unavailable rather
            // than producing potentially-wrong substitutions from syntax alone.
            return [];
        }
        if (MutationLevel > options.MutationLevel || node is not T tNode)
        {
            return [];
        }
        return ApplyMutations(tNode, semanticModel);
    }

    /// <summary>
    /// Resolves the type of an expression via the semantic model.
    /// Returns <c>null</c> when the expression has no resolvable type
    /// (e.g. dynamic, target-typed-new at incomplete site).
    /// </summary>
    protected static ITypeSymbol? GetExpressionType(ExpressionSyntax expression, SemanticModel semanticModel) =>
        semanticModel.GetTypeInfo(expression).Type;

    /// <summary>
    /// Resolves the return type of the enclosing method/property/lambda for the
    /// given node via the semantic model. Returns <c>null</c> when the node is
    /// not inside a return-bearing context.
    /// </summary>
    protected static ITypeSymbol? GetReturnType(SyntaxNode node, SemanticModel semanticModel)
    {
        var current = node;
        while (current is not null)
        {
            switch (current)
            {
                case MethodDeclarationSyntax method:
                    return semanticModel.GetTypeInfo(method.ReturnType).Type;
                case LocalFunctionStatementSyntax localFunction:
                    return semanticModel.GetTypeInfo(localFunction.ReturnType).Type;
                case PropertyDeclarationSyntax property:
                    return semanticModel.GetTypeInfo(property.Type).Type;
                case AccessorDeclarationSyntax accessor when accessor.Parent?.Parent is PropertyDeclarationSyntax accProp:
                    return semanticModel.GetTypeInfo(accProp.Type).Type;
                case ParenthesizedLambdaExpressionSyntax pLambda when pLambda.ReturnType is not null:
                    return semanticModel.GetTypeInfo(pLambda.ReturnType).Type;
                case ConversionOperatorDeclarationSyntax convOp:
                    return semanticModel.GetTypeInfo(convOp.Type).Type;
                case OperatorDeclarationSyntax op:
                    return semanticModel.GetTypeInfo(op.ReturnType).Type;
            }
            current = current.Parent;
        }
        return null;
    }
}
