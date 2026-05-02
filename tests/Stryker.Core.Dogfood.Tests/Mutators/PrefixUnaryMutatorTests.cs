using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 47 (v2.34.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class PrefixUnaryMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new PrefixUnaryMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(SyntaxKind.UnaryMinusExpression, SyntaxKind.UnaryPlusExpression)]
    [InlineData(SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression)]
    public void ShouldMutateUnaryTypes(SyntaxKind original, SyntaxKind expected)
    {
        var target = new PrefixUnaryMutator();
        var originalNode = SyntaxFactory.PrefixUnaryExpression(
            original,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(expected);
        mutation.Type.Should().Be(Mutator.Unary);
    }

    [Theory]
    [InlineData(SyntaxKind.PreIncrementExpression, SyntaxKind.PreDecrementExpression)]
    [InlineData(SyntaxKind.PreDecrementExpression, SyntaxKind.PreIncrementExpression)]
    public void ShouldMutateUpdateTypes(SyntaxKind original, SyntaxKind expected)
    {
        var target = new PrefixUnaryMutator();
        var originalNode = SyntaxFactory.PrefixUnaryExpression(
            original,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(expected);
        mutation.Type.Should().Be(Mutator.Update);
    }

    [Theory]
    [InlineData(SyntaxKind.BitwiseNotExpression)]
    [InlineData(SyntaxKind.LogicalNotExpression)]
    public void ShouldMutateAnRemove(SyntaxKind original)
    {
        var target = new PrefixUnaryMutator();
        var originalNode = SyntaxFactory.PrefixUnaryExpression(
            original,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(SyntaxKind.NumericLiteralExpression);
        mutation.DisplayName.Should().Be($"{original} to un-{original} mutation");

        if (original == SyntaxKind.BitwiseNotExpression)
        {
            mutation.Type.Should().Be(Mutator.Unary);
        }
        else
        {
            mutation.Type.Should().Be(Mutator.Boolean);
        }
    }

    [Theory]
    [InlineData(SyntaxKind.AddressOfExpression)]
    [InlineData(SyntaxKind.PointerIndirectionExpression)]
    public void ShouldNotMutate(SyntaxKind original)
    {
        var target = new PrefixUnaryMutator();

        var originalNode = SyntaxFactory.PrefixUnaryExpression(original, SyntaxFactory.IdentifierName("a"));
        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().BeEmpty();
    }
}
