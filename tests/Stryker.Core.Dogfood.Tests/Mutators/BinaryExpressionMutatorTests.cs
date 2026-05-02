using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 49 (v2.36.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class BinaryExpressionMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelBasic()
    {
        var target = new BinaryExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Basic);
    }

    [Theory]
    [InlineData(Mutator.Arithmetic, SyntaxKind.AddExpression, new[] { SyntaxKind.SubtractExpression })]
    [InlineData(Mutator.Arithmetic, SyntaxKind.SubtractExpression, new[] { SyntaxKind.AddExpression })]
    [InlineData(Mutator.Arithmetic, SyntaxKind.MultiplyExpression, new[] { SyntaxKind.DivideExpression })]
    [InlineData(Mutator.Arithmetic, SyntaxKind.DivideExpression, new[] { SyntaxKind.MultiplyExpression })]
    [InlineData(Mutator.Arithmetic, SyntaxKind.ModuloExpression, new[] { SyntaxKind.MultiplyExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.GreaterThanExpression, new[] { SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanOrEqualExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.LessThanExpression, new[] { SyntaxKind.GreaterThanExpression, SyntaxKind.LessThanOrEqualExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.GreaterThanOrEqualExpression, new[] { SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.LessThanOrEqualExpression, new[] { SyntaxKind.GreaterThanExpression, SyntaxKind.LessThanExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.EqualsExpression, new[] { SyntaxKind.NotEqualsExpression })]
    [InlineData(Mutator.Equality, SyntaxKind.NotEqualsExpression, new[] { SyntaxKind.EqualsExpression })]
    [InlineData(Mutator.Logical, SyntaxKind.LogicalAndExpression, new[] { SyntaxKind.LogicalOrExpression })]
    [InlineData(Mutator.Logical, SyntaxKind.LogicalOrExpression, new[] { SyntaxKind.LogicalAndExpression })]
    [InlineData(Mutator.Bitwise, SyntaxKind.BitwiseAndExpression, new[] { SyntaxKind.BitwiseOrExpression })]
    [InlineData(Mutator.Bitwise, SyntaxKind.BitwiseOrExpression, new[] { SyntaxKind.BitwiseAndExpression })]
    [InlineData(Mutator.Bitwise, SyntaxKind.RightShiftExpression, new[] { SyntaxKind.LeftShiftExpression, SyntaxKind.UnsignedRightShiftExpression })]
    [InlineData(Mutator.Bitwise, SyntaxKind.LeftShiftExpression, new[] { SyntaxKind.RightShiftExpression, SyntaxKind.UnsignedRightShiftExpression })]
    [InlineData(Mutator.Bitwise, SyntaxKind.UnsignedRightShiftExpression, new[] { SyntaxKind.LeftShiftExpression, SyntaxKind.RightShiftExpression })]
    public void ShouldMutate(Mutator expectedKind, SyntaxKind input, SyntaxKind[] expectedOutput)
    {
        var target = new BinaryExpressionMutator();
        var originalNode = SyntaxFactory.BinaryExpression(
            input,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        if (expectedOutput.Length == 1)
        {
            result.Should().ContainSingle();
        }
        else
        {
            result.Count.Should().Be(2, "Two mutations should have been made");
        }

        var index = 0;
        foreach (var mutation in result)
        {
            mutation.ReplacementNode.Kind().Should().Be(expectedOutput[index]);
            mutation.Type.Should().Be(expectedKind);
            mutation.DisplayName.Should().Be($"{mutation.Type} mutation");
            index++;
        }
    }

    [Fact]
    public void ShouldMutate_ExclusiveOr()
    {
        const SyntaxKind Kind = SyntaxKind.ExclusiveOrExpression;
        var target = new BinaryExpressionMutator();
        var originalNode = SyntaxFactory.BinaryExpression(
            Kind,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(4)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Count.Should().Be(2, "There should be two mutations");
        var logicalMutation = result.SingleOrDefault(x => x.Type == Mutator.Logical);
        logicalMutation.Should().NotBeNull();
        logicalMutation!.ReplacementNode.Should().NotBeNull();
        logicalMutation.ReplacementNode.Kind().Should().Be(SyntaxKind.EqualsExpression);
        logicalMutation.DisplayName.Should().Be("Logical mutation");

        var integralMutation = result.SingleOrDefault(x => x.Type == Mutator.Bitwise);
        integralMutation.Should().NotBeNull();
        integralMutation!.ReplacementNode.Should().NotBeNull();
        integralMutation.ReplacementNode.Kind().Should().Be(SyntaxKind.BitwiseNotExpression);
        integralMutation.DisplayName.Should().Be("Bitwise mutation");

        var parenthesizedExpression = integralMutation.ReplacementNode.ChildNodes().SingleOrDefault();
        parenthesizedExpression.Should().NotBeNull();
        parenthesizedExpression!.Kind().Should().Be(SyntaxKind.ParenthesizedExpression);

        var exclusiveOrExpression = parenthesizedExpression.ChildNodes().SingleOrDefault();
        exclusiveOrExpression.Should().NotBeNull();
        exclusiveOrExpression!.Kind().Should().Be(SyntaxKind.ExclusiveOrExpression);
    }

    [Fact]
    public void ShouldNotMutate_StringsLeft()
    {
        var target = new BinaryExpressionMutator();
        var originalNode = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutate_StringsRight()
    {
        var target = new BinaryExpressionMutator();
        var originalNode = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(8)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }
}
