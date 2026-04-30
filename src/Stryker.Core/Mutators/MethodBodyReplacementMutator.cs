using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.1 (Sprint 13, comparison.md §4.2 — cargo-mutants C1, "function-body
/// replacement genre"): replaces an entire method body with the
/// type-appropriate trivial body. For non-<c>void</c> methods,
/// <c>{ return default; }</c>; for <c>void</c> methods, an empty body
/// <c>{ }</c>. Catches "is this method ever even invoked, or is the body
/// dead code from the test suite's perspective?" — orthogonal to per-
/// statement mutators (<see cref="StatementMutator"/>, <see cref="BlockMutator"/>)
/// and to per-return-expression mutation (<see cref="TypeDrivenReturnMutator"/>).
///
/// Type-aware: needs the <see cref="SemanticModel"/> to look up the return
/// type and decide between the void / non-void shapes.
///
/// Conservative scope:
/// <list type="bullet">
///   <item>Skips abstract / partial / extern declarations (no body).</item>
///   <item>Skips expression-bodied methods — they round-trip via
///         <see cref="TypeDrivenReturnMutator"/>'s expression-shape mutators.</item>
///   <item>Skips async methods — replacing the body with <c>return default;</c>
///         loses the necessary <c>Task</c> wrapping; a future iteration
///         could emit <c>return Task.CompletedTask;</c> / <c>Task.FromResult(default)</c>.</item>
///   <item>Skips methods whose body is already a single trivial
///         <c>return default;</c> or empty body (no net change).</item>
///   <item>One mutation per qualifying method.</item>
/// </list>
///
/// Profile membership: All only — coarse genre, very high impact.
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class MethodBodyReplacementMutator : TypeAwareMutatorBase<MethodDeclarationSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(MethodDeclarationSyntax node, SemanticModel semanticModel)
    {
        if (node.Body is null)
        {
            // Abstract / partial / extern / expression-bodied — skip.
            yield break;
        }

        if (IsAsync(node))
        {
            yield break;
        }

        var returnTypeSymbol = semanticModel.GetTypeInfo(node.ReturnType).Type;
        var isVoid = returnTypeSymbol?.SpecialType == SpecialType.System_Void;

        BlockSyntax newBody;
        string label;
        if (isVoid)
        {
            if (node.Body.Statements.Count == 0)
            {
                yield break;
            }
            newBody = SyntaxFactory.Block();
            label = $"Method body emptied: '{node.Identifier.Text}' → {{ }}";
        }
        else
        {
            if (IsTrivialReturnDefault(node.Body))
            {
                yield break;
            }
            var defaultLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            var returnStatement = SyntaxFactory.ReturnStatement(defaultLiteral);
            newBody = SyntaxFactory.Block(returnStatement);
            label = $"Method body replaced: '{node.Identifier.Text}' → {{ return default; }}";
        }

        var replacement = node.WithBody(newBody);

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = replacement.WithCleanTriviaFrom(node),
            DisplayName = label,
            Type = Mutator.Statement,
        };
    }

    private static bool IsAsync(MethodDeclarationSyntax node) =>
        node.Modifiers.Any(static m => m.IsKind(SyntaxKind.AsyncKeyword));

    private static bool IsTrivialReturnDefault(BlockSyntax body) =>
        body.Statements is [ReturnStatementSyntax { Expression: LiteralExpressionSyntax lit }]
            && lit.IsKind(SyntaxKind.DefaultLiteralExpression);
}
