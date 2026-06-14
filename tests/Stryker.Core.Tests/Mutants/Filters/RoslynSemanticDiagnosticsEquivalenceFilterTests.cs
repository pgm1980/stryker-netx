using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class RoslynSemanticDiagnosticsEquivalenceFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsRoslynSemanticDiagnostics()
        => new RoslynSemanticDiagnosticsEquivalenceFilter().FilterId.Should().Be("RoslynSemanticDiagnostics");

    [Fact]
    public void IsEquivalent_OnNullSemanticModel_ReturnsFalse()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnNullReplacement_ReturnsFalse()
    {
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int a, int b) { var x = a + b; } }");
        var mutation = new Stryker.Abstractions.Mutation
        {
            OriginalNode = original,
            ReplacementNode = null!,
            Type = Stryker.Abstractions.Mutator.Statement,
            DisplayName = "test",
        };
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, model).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnStatementReplacementAtInvalidPosition_ReturnsFalse()
    {
        // Sprint 155 (ADR-037, v3.2.9): statement-level replacements are now in-scope
        // via TryGetSpeculativeSemanticModel. When the position isn't a valid statement-
        // position (e.g. inside a var-init expression), Roslyn refuses to construct a
        // speculative model — we conservatively keep the mutant (return false).
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int a, int b) { var x = a + b; } }");
        var statement = ParseStatement<ReturnStatementSyntax>("return 0;");
        var mutation = BuildMutation(original, statement);
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, model).Should().BeFalse(
            "Roslyn refuses to bind a return-statement at expression-position; keep mutant conservatively");
    }

    [Fact]
    public void IsEquivalent_OnDeclarationReplacement_ReturnsFalse()
    {
        // Declaration-level replacements (e.g. MethodDeclarationSyntax) remain
        // out-of-scope; the v2.1 parser-only filter handles structural validity.
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int a, int b) { var x = a + b; } }");
        var classDecl = (ClassDeclarationSyntax)original.Ancestors()
            .First(static n => n is ClassDeclarationSyntax);
        var mutation = BuildMutation(original, classDecl);
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, model).Should().BeFalse(
            "declaration-level replacement is out-of-scope for this filter");
    }

    [Fact]
    public void IsEquivalent_OnInstanceMethodGroupReplacement_ReturnsFalse()
    {
        // EQF-001 (external 360 test): StringMethodMutator turns the StartsWith member-access
        // method group into EndsWith. The call parentheses are the parent node, not part of the
        // replacement, so the replacement is a bare method group. Speculatively binding it as an
        // expression yields a null Symbol with CandidateReason OverloadResolutionFailure but a
        // non-empty CandidateSymbols set. A method group with resolvable candidates is valid and
        // killable, not equivalent. The same one-line fix also covers LinqMutator extension-method
        // groups such as the Min to Max swap; that path is import and reference dependent and only
        // reproduces end to end (repro bug01), not in this minimal semantic-model harness, so it is
        // verified via the E2E probe rather than here.
        var (model, original) = BuildSemanticContext<MemberAccessExpressionSyntax>(
            "class C { bool M(string s, string p) => s.StartsWith(p); }");
        var replacement = original.WithName(SyntaxFactory.IdentifierName("EndsWith"));
        var mutation = BuildMutation(original, replacement);
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, model).Should().BeFalse(
            "an instance method group has resolvable overload candidates and is killable, not equivalent");
    }
}
