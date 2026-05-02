using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 46 (v2.33.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Mutators/PostfixUnaryMutatorTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `ApplyMutations(node, null)` → `ApplyMutations(node, null!)` — production drift, SemanticModel param non-nullable.
/// `.IsKind(SyntaxKind)` → `.Kind() == SyntaxKind` — direct comparison avoids extension-method namespace dependency.
/// </summary>
public class PostfixUnaryMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new PostfixUnaryMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(SyntaxKind.PostIncrementExpression, SyntaxKind.PostDecrementExpression)]
    [InlineData(SyntaxKind.PostDecrementExpression, SyntaxKind.PostIncrementExpression)]
    public void ShouldMutate(SyntaxKind original, SyntaxKind expected)
    {
        var target = new PostfixUnaryMutator();
        var originalNode = SyntaxFactory.PostfixUnaryExpression(
            original,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var result = target.ApplyMutations(originalNode, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Kind().Should().Be(expected);
        mutation.Type.Should().Be(Mutator.Update);
        mutation.DisplayName.Should().Be($"{original} to {expected} mutation");
    }
}
