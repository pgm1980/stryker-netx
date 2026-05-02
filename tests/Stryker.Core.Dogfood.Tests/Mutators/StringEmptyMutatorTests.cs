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
public class StringEmptyMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new StringEmptyMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Fact]
    public void ShouldMutateLowercaseString()
    {
        var node = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            SyntaxFactory.IdentifierName("Empty"));
        var mutator = new StringEmptyMutator();

        var result = mutator.ApplyMutations(node, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("String mutation");
        var replacement = mutation.ReplacementNode.Should().BeOfType<LiteralExpressionSyntax>().Which;
        replacement.Token.ValueText.Should().Be("Stryker was here!");
    }

    [Fact]
    public void ShouldNotMutateUppercaseString()
    {
        var node = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("String"),
            SyntaxFactory.IdentifierName("Empty"));
        var mutator = new StringEmptyMutator();

        var result = mutator.ApplyMutations(node, null!).ToList();

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("x")]
    [InlineData("string.Empty")]
    [InlineData("args[0].Substring(1)")]
    public void ShouldMutateIsNullOrEmpty(string argument)
    {
        var expression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression($"string.IsNullOrEmpty({argument})");
        var target = new StringEmptyMutator();
        var mutated = target.ApplyMutations(expression, null!).ToList();

        mutated.Count.Should().Be(2);
        ValidateMutationIsNullCheck(mutated[0], expression);
        ValidateMutationIsEmptyCheck(mutated[1], expression);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("string.Empty")]
    [InlineData("args[0].Substring(1)")]
    public void ShouldMutateIsNullOrWhiteSpace(string argument)
    {
        var expression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression($"string.IsNullOrWhiteSpace({argument})");
        var target = new StringEmptyMutator();
        var mutated = target.ApplyMutations(expression, null!).ToList();

        mutated.Count.Should().Be(3);
        ValidateMutationIsNullCheck(mutated[0], expression);
        ValidateMutationIsEmptyCheck(mutated[1], expression);
        ValidateMutationIsWhiteSpaceCheck(mutated[2], expression);
    }

    [Theory]
    [InlineData("IsNormalized")]
    [InlineData("Test")]
    [InlineData("IsNotNullOrNotEmpty")]
    public void ShouldNotMutateOtherMethods(string method)
    {
        var expression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression($"string.{method}(x)");
        var target = new StringEmptyMutator();
        var mutated = target.ApplyMutations(expression, null!).ToList();

        mutated.Should().BeEmpty();
    }

    private static void ValidateMutationIsNullCheck(Mutation mutation, InvocationExpressionSyntax original)
    {
        mutation.OriginalNode.Should().Be(original);
        mutation.DisplayName.Should().Be("String mutation");
        mutation.Type.Should().Be(Mutator.String);

        var parenthesizedExpression = mutation.ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>().Which;
        var binaryExpression = parenthesizedExpression.Expression.Should().BeOfType<BinaryExpressionSyntax>().Which;

        binaryExpression.Kind().Should().Be(SyntaxKind.NotEqualsExpression);
        binaryExpression.Left.ToString().Should().Be(original.ArgumentList.Arguments[0].Expression.ToString());

        var nullLiteral = binaryExpression.Right.Should().BeOfType<LiteralExpressionSyntax>().Which;
        nullLiteral.Kind().Should().Be(SyntaxKind.NullLiteralExpression);
    }

    private static void ValidateMutationIsEmptyCheck(Mutation mutation, InvocationExpressionSyntax original)
    {
        mutation.OriginalNode.Should().Be(original);
        mutation.DisplayName.Should().Be("String mutation");
        mutation.Type.Should().Be(Mutator.String);

        var parenthesizedExpression = mutation.ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>().Which;
        var binaryExpression = parenthesizedExpression.Expression.Should().BeOfType<BinaryExpressionSyntax>().Which;

        binaryExpression.Kind().Should().Be(SyntaxKind.NotEqualsExpression);
        binaryExpression.Left.ToString().Should().Be(original.ArgumentList.Arguments[0].Expression.ToString());

        var emptyLiteral = binaryExpression.Right.Should().BeOfType<LiteralExpressionSyntax>().Which;
        emptyLiteral.Kind().Should().Be(SyntaxKind.StringLiteralExpression);
        emptyLiteral.Token.ToString().Should().Be(@"""""");
    }

    private static void ValidateMutationIsWhiteSpaceCheck(Mutation mutation, InvocationExpressionSyntax original)
    {
        mutation.OriginalNode.Should().Be(original);
        mutation.DisplayName.Should().Be("String mutation");
        mutation.Type.Should().Be(Mutator.String);

        var parenthesizedExpression = mutation.ReplacementNode.Should().BeOfType<ParenthesizedExpressionSyntax>().Which;
        var binaryExpression = parenthesizedExpression.Expression.Should().BeOfType<BinaryExpressionSyntax>().Which;

        binaryExpression.Kind().Should().Be(SyntaxKind.NotEqualsExpression);
        binaryExpression.Left.ToString().Should().Be(original.ArgumentList.Arguments[0].Expression.ToString() + ".Trim()");

        var emptyLiteral = binaryExpression.Right.Should().BeOfType<LiteralExpressionSyntax>().Which;
        emptyLiteral.Kind().Should().Be(SyntaxKind.StringLiteralExpression);
        emptyLiteral.Token.ToString().Should().Be(@"""""");
    }
}
