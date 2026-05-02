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
public class BooleanMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new BooleanMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression)]
    [InlineData(SyntaxKind.FalseLiteralExpression, SyntaxKind.TrueLiteralExpression)]
    public void ShouldMutate(SyntaxKind original, SyntaxKind expected)
    {
        var target = new BooleanMutator();

        var result = target.ApplyMutations(SyntaxFactory.LiteralExpression(original), null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(expected);
        mutation.DisplayName.Should().Be("Boolean mutation");
    }

    [Theory]
    [InlineData(SyntaxKind.NumericLiteralExpression)]
    [InlineData(SyntaxKind.StringLiteralExpression)]
    [InlineData(SyntaxKind.CharacterLiteralExpression)]
    [InlineData(SyntaxKind.NullLiteralExpression)]
    [InlineData(SyntaxKind.DefaultLiteralExpression)]
    public void ShouldNotMutate(SyntaxKind original)
    {
        var target = new BooleanMutator();

        var result = target.ApplyMutations(SyntaxFactory.LiteralExpression(original), null!).ToList();

        result.Should().BeEmpty();
    }
}
