using System.Collections.Generic;
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
public class InitializerMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new InitializerMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Fact]
    public void ShouldRemoveValuesFromArrayInitializer()
    {
        var initializerExpression = SyntaxFactory.InitializerExpression(
            SyntaxKind.ArrayInitializerExpression,
            SyntaxFactory.SeparatedList(new List<ExpressionSyntax>
            {
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(5)),
            }));
        var target = new InitializerMutator();

        var result = target.ApplyMutations(initializerExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Array initializer mutation");

        var replacement = mutation.ReplacementNode.Should().BeOfType<InitializerExpressionSyntax>().Which;
        replacement.Expressions.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateEmptyInitializer()
    {
        var emptyInitializerExpression = SyntaxFactory.InitializerExpression(
            SyntaxKind.ArrayInitializerExpression,
            SyntaxFactory.SeparatedList<ExpressionSyntax>());
        var target = new InitializerMutator();

        var result = target.ApplyMutations(emptyInitializerExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStackallocArrayCreationExpressionSyntax()
    {
        var arrayCreationExpression = (SyntaxFactory.ParseExpression("stackalloc int[] { 0 }") as StackAllocArrayCreationExpressionSyntax)!;

        var target = new InitializerMutator();

        var result = target.ApplyMutations(arrayCreationExpression.Initializer!, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateArrayCreationExpressionSyntax()
    {
        var arrayCreationExpression = (SyntaxFactory.ParseExpression("new int[] { 0 }") as ArrayCreationExpressionSyntax)!;

        var target = new InitializerMutator();

        var result = target.ApplyMutations(arrayCreationExpression.Initializer!, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateImplicitArrayCreationExpressionSyntax()
    {
        var arrayCreationExpression = (SyntaxFactory.ParseExpression("new [] { 0 }") as ImplicitArrayCreationExpressionSyntax)!;

        var target = new InitializerMutator();

        var result = target.ApplyMutations(arrayCreationExpression.Initializer, null!).ToList();

        result.Should().BeEmpty();
    }
}
