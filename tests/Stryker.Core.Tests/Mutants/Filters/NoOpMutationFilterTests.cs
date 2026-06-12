using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

// Sprint 181 (360°-Analyse G-17): kein bestehender Filter fing generische No-op-Emissionen
// (Replacement ≡ Original, z. B. TypeDrivenReturn auf bereits-Zielwert) — solche Mutanten
// überleben jeden Test und drücken den Score als False Survivors. Filter #0 vergleicht
// die Bäume strukturell (Trivia-insensitiv) und fängt ALLE Shapes zentral.
public class NoOpMutationFilterTests : MutatorTestBase
{
    [Fact]
    public void FilterId_IsNoOpMutation()
        => new NoOpMutationFilter().FilterId.Should().Be("NoOpMutation");

    [Fact]
    public void IsEquivalent_OnIdenticalExpressionReplacement_ReturnsTrue()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x + y");
        var mutation = BuildMutation(node, node);
        new NoOpMutationFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnIdenticalStatementReplacement_ReturnsTrue()
    {
        var original = ParseStatement<ReturnStatementSyntax>("return 0;");
        var replacement = ParseStatement<ReturnStatementSyntax>("return 0;");
        var mutation = BuildMutation(original, replacement);
        new NoOpMutationFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_OnTriviaOnlyDifference_ReturnsTrue()
    {
        var original = ParseExpression<BinaryExpressionSyntax>("x + 1");
        var replacement = ParseExpression<BinaryExpressionSyntax>("x  +  1");
        var mutation = BuildMutation(original, replacement);
        new NoOpMutationFilter().IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Theory]
    [InlineData("x + 1", "x - 1")]
    [InlineData("x", "y")]
    [InlineData("true", "false")]
    public void IsEquivalent_OnRealMutation_ReturnsFalse(string originalSource, string replacementSource)
    {
        var original = SyntaxFactory.ParseExpression(originalSource);
        var replacement = SyntaxFactory.ParseExpression(replacementSource);
        var mutation = BuildMutation(original, replacement);
        new NoOpMutationFilter().IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }
}
