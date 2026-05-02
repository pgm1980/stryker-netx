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
public class ObjectCreationMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new ObjectCreationMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData("new List<int> { 1, 3 }")]
    [InlineData("new Collection<int> { 1, 3 }")]
    [InlineData(@"new Dictionary<int, StudentName>()
        {
            { 111, new StudentName { FirstName='Foo', LastName='Bar', ID=211 } }
        };")]
    public void ShouldRemoveValuesFromCollectionInitializer(string initializer)
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression(initializer) as ObjectCreationExpressionSyntax)!;

        var target = new ObjectCreationMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.Type.Should().Be(Mutator.Initializer);

        var replacement = mutation.ReplacementNode.Should().BeOfType<ObjectCreationExpressionSyntax>().Which;
        replacement.Initializer!.Expressions.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRemoveValuesFromObjectInitializer()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new SomeClass { SomeProperty = SomeValue }") as ObjectCreationExpressionSyntax)!;

        var target = new ObjectCreationMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Object initializer mutation");
        mutation.Type.Should().Be(Mutator.Initializer);

        var replacement = mutation.ReplacementNode.Should().BeOfType<ObjectCreationExpressionSyntax>().Which;
        replacement.Initializer!.Expressions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("new List<int> { }")]
    [InlineData("new SomeClass { }")]
    public void ShouldNotMutateEmptyInitializer(string initializer)
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression(initializer) as ObjectCreationExpressionSyntax)!;

        var target = new ObjectCreationMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().BeEmpty();
    }
}
