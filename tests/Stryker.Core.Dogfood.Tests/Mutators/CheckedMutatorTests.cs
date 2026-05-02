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
public class CheckedMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelComplete()
    {
        var target = new CheckedMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(SyntaxKind.CheckedExpression, "4 + 2", SyntaxKind.AddExpression)]
    public void ShouldMutate(SyntaxKind original, string expression, SyntaxKind expected)
    {
        var target = new CheckedMutator();

        var es = SyntaxFactory.ParseExpression(expression);
        var result = target.ApplyMutations(SyntaxFactory.CheckedExpression(original, es), null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(expected);
        mutation.DisplayName.Should().Be("Remove checked expression");
    }

    [Theory]
    [InlineData(SyntaxKind.UncheckedExpression)]
    public void ShouldNotMutate(SyntaxKind original)
    {
        var target = new CheckedMutator();

        var es = SyntaxFactory.ParseExpression("4 + 2");
        var result = target.ApplyMutations(SyntaxFactory.CheckedExpression(original, es), null!).ToList();

        result.Should().BeEmpty();
    }
}
