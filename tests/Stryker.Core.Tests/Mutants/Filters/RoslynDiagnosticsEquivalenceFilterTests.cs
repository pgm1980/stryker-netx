using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class RoslynDiagnosticsEquivalenceFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsRoslynDiagnostics()
        => new RoslynDiagnosticsEquivalenceFilter().FilterId.Should().Be("RoslynDiagnostics");

    [Fact]
    public void IsEquivalent_OnNullReplacementNode_ReturnsFalse()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = new Stryker.Abstractions.Mutation
        {
            OriginalNode = original,
            ReplacementNode = null!,
            Type = Stryker.Abstractions.Mutator.Statement,
            DisplayName = "test",
        };
        new RoslynDiagnosticsEquivalenceFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnValidReplacementNode_ReturnsFalse()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("a + b");
        var replacement = ParseExpression<BinaryExpressionSyntax>("a - b");
        var mutation = BuildMutation(original, replacement);
        // Valid syntax → no parse-error → not flagged as equivalent.
        new RoslynDiagnosticsEquivalenceFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnReplacementWithParseErrors_ReturnsTrue()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("a + b");
        // Construct a syntactically broken expression by parsing invalid code.
        var brokenTree = CSharpSyntaxTree.ParseText("class C { void M() { var x = ( ; } }");
        var brokenNode = brokenTree.GetRoot();
        var mutation = BuildMutation(original, brokenNode);
        new RoslynDiagnosticsEquivalenceFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }
}
