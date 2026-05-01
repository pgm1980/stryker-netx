using FluentAssertions;
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
    public void IsEquivalent_OnNonExpressionReplacement_ReturnsFalse()
    {
        // Statement-level replacement is intentionally out-of-scope for this filter.
        var (model, original) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { void M(int a, int b) { var x = a + b; } }");
        var statement = ParseStatement<ReturnStatementSyntax>("return 0;");
        var mutation = BuildMutation(original, statement);
        new RoslynSemanticDiagnosticsEquivalenceFilter().IsEquivalent(mutation, model).Should().BeFalse();
    }
}
