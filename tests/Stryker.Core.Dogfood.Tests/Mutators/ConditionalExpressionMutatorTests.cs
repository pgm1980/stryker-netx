using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 47 (v2.34.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class ConditionalExpressionMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new ConditionalExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Fact]
    public void ShouldMutate_TwoMutations()
    {
        var target = new ConditionalExpressionMutator();
        const string Source = "251 == 73 ? 1 : 0";
        var tree = CSharpSyntaxTree.ParseText(Source);
        var originalNode = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Count.Should().Be(2, "Two mutations should have been made");
        result[0].ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>()
            .Which.Expression.Should().BeOfType<ConditionalExpressionSyntax>()
            .Which.Condition.Kind().Should().Be(SyntaxKind.TrueLiteralExpression);
        result[1].ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>()
            .Which.Expression.Should().BeOfType<ConditionalExpressionSyntax>()
            .Which.Condition.Kind().Should().Be(SyntaxKind.FalseLiteralExpression);
    }

    [Fact]
    public void ShouldMutate_DoNotTouchBranches()
    {
        var target = new ConditionalExpressionMutator();
        const string Source = "251 == 73 ? 1 : 0";
        var tree = CSharpSyntaxTree.ParseText(Source);
        var originalNode = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();

        var result = target.ApplyMutations(originalNode, null!).ToList();

        foreach (var mutation in result)
        {
            var pes = mutation.ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>().Which;
            var ces = pes.Expression.Should().BeOfType<ConditionalExpressionSyntax>().Which;
            ces.WhenTrue.IsEquivalentTo(originalNode.WhenTrue).Should().BeTrue();
            ces.WhenFalse.IsEquivalentTo(originalNode.WhenFalse).Should().BeTrue();
        }
    }

    [Fact]
    public void ShouldNotMutateDeclarationPatterns()
    {
        var target = new ConditionalExpressionMutator();
        const string Source = "var y = x is object result ? result.ToString() : null;";
        var tree = CSharpSyntaxTree.ParseText(Source);

        var expressionSyntax = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().BeEmpty();
    }
}
