#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>Sprint 123 (v3.0.10) partial port (replaces Sprint 109 architectural-deferral).
/// First 3 upstream tests use simple [DataRow]/[InlineData] patterns and port directly to xUnit.
/// 4th upstream test uses custom [CollectionExpressionTest] MSTest attribute with multi-line C#
/// fixture data — that one defers to dedicated MemberData rewrite sprint.</summary>
public class CollectionExpressionMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelAdvanced()
    {
        var target = new CollectionExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Advanced);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[ ]")]
    [InlineData("[           ]")]
    [InlineData("[ /* Comment */ ]")]
    public void ShouldAddValueToEmptyCollectionExpression(string expression)
    {
        var expressionSyntax = (CollectionExpressionSyntax)SyntaxFactory.ParseExpression(expression);
        var target = new CollectionExpressionMutator();
        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Collection expression mutation");
        var replacement = mutation.ReplacementNode.Should().BeOfType<CollectionExpressionSyntax>().Which;
        var element = replacement.Elements.Should().ContainSingle().Which;
        var token = element.Should().BeOfType<ExpressionElementSyntax>().Which.Expression.Should().BeOfType<LiteralExpressionSyntax>().Which.Token;
        token.Kind().Should().Be(SyntaxKind.DefaultKeyword);
    }

    [Theory]
    [InlineData("[1, 2, 3]")]
    [InlineData("[-1, 3]")]
    [InlineData("[1, .. abc, 3]")]
    [InlineData("[..abc]")]
    public void ShouldRemoveValuesFromCollectionExpression(string expression)
    {
        var expressionSyntax = (CollectionExpressionSyntax)SyntaxFactory.ParseExpression(expression);
        var target = new CollectionExpressionMutator();
        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Collection expression mutation");
        var replacement = mutation.ReplacementNode.Should().BeOfType<CollectionExpressionSyntax>().Which;
        replacement.Elements.Should().BeEmpty();
    }

    [Fact(Skip = "ARCHITECTURAL DEFERRAL: custom [CollectionExpressionTest] MSTest attribute with multi-line C# fixture inputs (e.g. 'Should mutate collection expression with spread elements' / 'with explicit cast' / 'with conditional element'). Re-port = MemberData rewrite + fixture-loader helper. Defer to dedicated CollectionExpression deep-port sprint.")]
    public void CollectionExpressionMutator_CustomAttribute_FixtureDeferral() { /* defer */ }
}
