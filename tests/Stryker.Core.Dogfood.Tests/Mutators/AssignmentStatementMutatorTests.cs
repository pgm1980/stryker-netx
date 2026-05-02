using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 50 (v2.37.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class AssignmentStatementMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new AssignmentExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(SyntaxKind.AddAssignmentExpression, SyntaxKind.SubtractAssignmentExpression, null)]
    [InlineData(SyntaxKind.SubtractAssignmentExpression, SyntaxKind.AddAssignmentExpression, null)]
    [InlineData(SyntaxKind.MultiplyAssignmentExpression, SyntaxKind.DivideAssignmentExpression, null)]
    [InlineData(SyntaxKind.DivideAssignmentExpression, SyntaxKind.MultiplyAssignmentExpression, null)]
    [InlineData(SyntaxKind.ModuloAssignmentExpression, SyntaxKind.MultiplyAssignmentExpression, null)]
    [InlineData(SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.RightShiftAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression)]
    [InlineData(SyntaxKind.RightShiftAssignmentExpression, SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression)]
    [InlineData(SyntaxKind.AndAssignmentExpression, SyntaxKind.OrAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression)]
    [InlineData(SyntaxKind.OrAssignmentExpression, SyntaxKind.AndAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression)]
    [InlineData(SyntaxKind.ExclusiveOrAssignmentExpression, SyntaxKind.OrAssignmentExpression, SyntaxKind.AndAssignmentExpression)]
    [InlineData(SyntaxKind.CoalesceAssignmentExpression, SyntaxKind.SimpleAssignmentExpression, null)]
    [InlineData(SyntaxKind.UnsignedRightShiftAssignmentExpression, SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.RightShiftAssignmentExpression)]
    public void AssignmentMutator_ShouldMutate(SyntaxKind input, SyntaxKind expectedOutput, SyntaxKind? additionalOutput)
    {
        var target = new AssignmentExpressionMutator();
        var originalNode = SyntaxFactory.AssignmentExpression(
            input,
            SyntaxFactory.IdentifierName("a"),
            SyntaxFactory.IdentifierName("b"));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        if (additionalOutput.HasValue)
        {
            result.Count.Should().Be(2);
            result[0].ReplacementNode.Kind().Should().Be(expectedOutput);
            result[1].ReplacementNode.Kind().Should().Be(additionalOutput.Value);
        }
        else
        {
            result.Should().ContainSingle();
            result[0].ReplacementNode.Kind().Should().Be(expectedOutput);
        }

        foreach (var mutation in result)
        {
            mutation.Type.Should().Be(Mutator.Assignment);
            mutation.DisplayName.Should().Be($"{input} to {mutation.ReplacementNode.Kind()} mutation");
        }
    }

    [Theory]
    [InlineData("a += b", "a -= b")]
    [InlineData("a +=  b", "a -=  b")]
    [InlineData("a  += b", "a  -= b")]
    [InlineData("a +=\nb", "a -=\nb")]
    [InlineData("a\n+= b", "a\n-= b")]
    public void ShouldKeepTrivia(string originalExpressionString, string expectedExpressionString)
    {
        var target = new AssignmentExpressionMutator();
        var originalExpression = SyntaxFactory.ParseExpression(originalExpressionString);

        var result = target.ApplyMutations((originalExpression as AssignmentExpressionSyntax)!, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.ToString().Should().Be(expectedExpressionString);
    }

    [Fact]
    public void ShouldNotMutateSimpleAssignment()
    {
        var target = new AssignmentExpressionMutator();
        var originalNode = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName("a"),
            SyntaxFactory.IdentifierName("b"));
        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStringLiteralsLeft()
    {
        var target = new AssignmentExpressionMutator();
        var originalNode = SyntaxFactory.AssignmentExpression(
            SyntaxKind.AddAssignmentExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression),
            SyntaxFactory.IdentifierName("b"));
        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStringLiteralsRight()
    {
        var target = new AssignmentExpressionMutator();
        var originalNode = SyntaxFactory.AssignmentExpression(
            SyntaxKind.AddAssignmentExpression,
            SyntaxFactory.IdentifierName("b"),
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression));
        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStringLiteralsBoth()
    {
        var target = new AssignmentExpressionMutator();
        var originalNode = SyntaxFactory.AssignmentExpression(
            SyntaxKind.AddAssignmentExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression),
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression));
        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }
}
