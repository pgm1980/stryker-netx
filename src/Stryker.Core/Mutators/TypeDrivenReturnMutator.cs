using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 9, ADR-015): the cargo-mutants-style typed-default-return
/// mutator. Replaces the value of an existing <c>return</c> statement with a
/// type-appropriate default — the most aggressive "did the test actually
/// observe the result?" mutator known.
///
/// Substitutions per return-type symbol:
/// <list type="bullet">
///   <item><c>Task&lt;T&gt;</c>          → <c>Task.FromResult(default(T))</c></item>
///   <item><c>ValueTask&lt;T&gt;</c>     → <c>new ValueTask&lt;T&gt;(default(T))</c></item>
///   <item><c>IEnumerable&lt;T&gt;</c>   → <c>Enumerable.Empty&lt;T&gt;()</c></item>
///   <item><c>List&lt;T&gt;</c>          → <c>new List&lt;T&gt;()</c></item>
///   <item><c>Dictionary&lt;K,V&gt;</c>  → <c>new Dictionary&lt;K,V&gt;()</c></item>
///   <item><c>string</c>                  → <c>string.Empty</c> (and <c>null</c> when nullable-annotated)</item>
///   <item><c>int</c>, <c>long</c>, etc. → <c>0</c></item>
///   <item><c>bool</c>                    → <c>false</c> AND <c>true</c></item>
/// </list>
/// Type-aware via <see cref="TypeAwareMutatorBase{T}"/>; bails when no
/// SemanticModel is available (matches ADR-015 contract).
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class TypeDrivenReturnMutator : TypeAwareMutatorBase<ReturnStatementSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(ReturnStatementSyntax node, SemanticModel semanticModel)
    {
        // No expression to replace (`return;` in a void method) — nothing to mutate.
        if (node.Expression is null)
        {
            yield break;
        }

        var returnType = GetReturnType(node, semanticModel);
        if (returnType is null)
        {
            yield break;
        }

        foreach (var replacement in GenerateReplacementsForType(returnType))
        {
            yield return new Mutation
            {
                OriginalNode = node,
                ReplacementNode = SyntaxFactory.ReturnStatement(replacement).WithCleanTriviaFrom(node),
                DisplayName = $"Type-driven return: {returnType.ToDisplayString()} → {replacement.ToFullString()}",
                Type = Mutator.Statement,
            };
        }
    }

    private static IEnumerable<ExpressionSyntax> GenerateReplacementsForType(ITypeSymbol returnType)
    {
        var displayName = returnType.OriginalDefinition.ToDisplayString();

        switch (displayName)
        {
            case "System.Threading.Tasks.Task<TResult>":
                yield return ParseExpression("System.Threading.Tasks.Task.FromResult(default(" + GetSingleTypeArgument(returnType) + "))");
                yield break;

            case "System.Threading.Tasks.ValueTask<TResult>":
                yield return ParseExpression("new System.Threading.Tasks.ValueTask<" + GetSingleTypeArgument(returnType) + ">(default(" + GetSingleTypeArgument(returnType) + "))");
                yield break;

            case "System.Collections.Generic.IEnumerable<T>":
                yield return ParseExpression("System.Linq.Enumerable.Empty<" + GetSingleTypeArgument(returnType) + ">()");
                yield break;

            case "System.Collections.Generic.List<T>":
                yield return ParseExpression("new System.Collections.Generic.List<" + GetSingleTypeArgument(returnType) + ">()");
                yield break;

            case "System.Collections.Generic.Dictionary<TKey, TValue>":
                yield return ParseExpression("new System.Collections.Generic.Dictionary<" + GetTwoTypeArguments(returnType) + ">()");
                yield break;
        }

        switch (returnType.SpecialType)
        {
            case SpecialType.System_String:
                yield return ParseExpression("string.Empty");
                if (returnType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    yield return ParseExpression("null");
                }
                yield break;

            case SpecialType.System_Boolean:
                yield return ParseExpression("false");
                yield return ParseExpression("true");
                yield break;

            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                yield return ParseExpression("0");
                yield break;
        }
    }

    private static ExpressionSyntax ParseExpression(string text) => SyntaxFactory.ParseExpression(text);

    private static string GetSingleTypeArgument(ITypeSymbol type) =>
        type is INamedTypeSymbol named && named.TypeArguments.Length == 1
            ? named.TypeArguments[0].ToDisplayString()
            : "object";

    private static string GetTwoTypeArguments(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.TypeArguments.Length == 2)
        {
            return named.TypeArguments[0].ToDisplayString() + ", " + named.TypeArguments[1].ToDisplayString();
        }
        return "object, object";
    }
}
