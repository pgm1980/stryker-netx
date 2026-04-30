using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.1 — PIT EXP_ARGUMENT_PROPAGATION):
/// rewrites <c>foo.Bar(a, b) → a</c> (or <c>→ b</c>) when one of the
/// arguments has a static type assignable to the call's return type.
/// Catches "is the method's transformation of its inputs actually
/// observed?" — if every test path happens to produce arguments equal to
/// the expected output, the propagation mutant survives.
///
/// Type-aware: needs the <see cref="SemanticModel"/> to look up the
/// invocation's return type and each argument's static type, then the
/// implicit-conversion classification between them. Without the semantic
/// model the mutator is silent (cannot make a type-safe substitution).
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Skips when the invocation has zero arguments.</item>
///   <item>Skips <c>void</c> calls (no return type to substitute for).</item>
///   <item>Skips arguments whose conversion to the return type is not
///         implicit (would require an explicit cast and may fail at
///         runtime).</item>
///   <item>Emits one mutation per type-compatible argument.</item>
/// </list>
///
/// Profile membership: All only — every multi-arg invocation × N mutants
/// makes this the highest-volume operator in the catalogue.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class ArgumentPropagationMutator : TypeAwareMutatorBase<InvocationExpressionSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(InvocationExpressionSyntax node, SemanticModel semanticModel)
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            yield break;
        }

        if (semanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
        {
            yield break;
        }

        var returnType = methodSymbol.ReturnType;
        if (returnType is null || returnType.SpecialType == SpecialType.System_Void)
        {
            yield break;
        }

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            var argType = semanticModel.GetTypeInfo(arg.Expression).Type;
            if (argType is null)
            {
                continue;
            }

            if (semanticModel.Compilation is not CSharpCompilation csharpCompilation)
            {
                yield break;
            }

            var conversion = csharpCompilation.ClassifyConversion(argType, returnType);
            if (!conversion.IsImplicit || conversion.IsUserDefined)
            {
                continue;
            }

            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = arg.Expression.WithCleanTriviaFrom(node),
                DisplayName = $"Argument propagation: '{node.Expression}(…)' → arg #{i} ('{arg.Expression}')",
                Type = Mutator.Statement,
            };
        }
    }
}
