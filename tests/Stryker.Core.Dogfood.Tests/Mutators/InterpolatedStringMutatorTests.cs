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
public class InterpolatedStringMutatorTests
{
    private static InterpolatedStringExpressionSyntax GetInterpolatedString(string expression) =>
        (SyntaxFactory.ParseExpression(expression) as InterpolatedStringExpressionSyntax)!;

    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new InterpolatedStringMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData("$\"foo\"")]
    [InlineData("$@\"foo\"")]
    [InlineData("$\"foo {42}\"")]
    public void ShouldMutate(string expression)
    {
        var node = GetInterpolatedString(expression);
        var mutator = new InterpolatedStringMutator();

        var result = mutator.ApplyMutations(node, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Should().BeOfType<InterpolatedStringExpressionSyntax>()
            .Which.Contents.Should().BeEmpty();
        mutation.DisplayName.Should().Be("String mutation");
    }

    [Theory]
    [InlineData("$\"\"")]
    [InlineData("$@\"\"")]
    public void ShouldNotMutateEmptyInterpolatedString(string expression)
    {
        var node = GetInterpolatedString(expression);
        var mutator = new InterpolatedStringMutator();

        var result = mutator.ApplyMutations(node, null!).ToList();

        result.Should().BeEmpty();
    }
}
