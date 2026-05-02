using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
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
public class StringMethodMutatorTests
{
    private static (SemanticModel semanticModel, InvocationExpressionSyntax expression) CreateSemanticModelFromExpression(string input)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            $$"""
              using System.Linq;

              class TestClass {
              private string testString = "test";
              private char c = 't';

              void TestMethod() {
                      {{input}} ";
                  }
              }
              """);

        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var expression = syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        return (semanticModel, expression);
    }

    [Theory]
    [InlineData("testString.EndsWith(c)", "StartsWith", "String Method Mutation (Replace EndsWith() with StartsWith())")]
    [InlineData("testString.StartsWith(c)", "EndsWith", "String Method Mutation (Replace StartsWith() with EndsWith())")]
    [InlineData("testString.TrimStart()", "TrimEnd", "String Method Mutation (Replace TrimStart() with TrimEnd())")]
    [InlineData("testString.TrimEnd()", "TrimStart", "String Method Mutation (Replace TrimEnd() with TrimStart())")]
    [InlineData("testString.ToUpper()", "ToLower", "String Method Mutation (Replace ToUpper() with ToLower())")]
    [InlineData("testString.ToLower()", "ToUpper", "String Method Mutation (Replace ToLower() with ToUpper())")]
    [InlineData("testString.ToUpperInvariant()", "ToLowerInvariant", "String Method Mutation (Replace ToUpperInvariant() with ToLowerInvariant())")]
    [InlineData("testString.ToLowerInvariant()", "ToUpperInvariant", "String Method Mutation (Replace ToLowerInvariant() with ToUpperInvariant())")]
    [InlineData("testString.PadLeft(10)", "PadRight", "String Method Mutation (Replace PadLeft() with PadRight())")]
    [InlineData("testString.PadRight(10)", "PadLeft", "String Method Mutation (Replace PadRight() with PadLeft())")]
    [InlineData("testString.LastIndexOf(c)", "IndexOf", "String Method Mutation (Replace LastIndexOf() with IndexOf())")]
    [InlineData("testString.IndexOf(c)", "LastIndexOf", "String Method Mutation (Replace IndexOf() with LastIndexOf())")]
    public void ShouldMutateStringMethods(string expression, string mutatedMethod, string expectedDisplayName)
    {
        var (semanticModel, expressionSyntax) = CreateSemanticModelFromExpression(expression);
        var target = new StringMethodMutator();
        var result = target.ApplyMutations((MemberAccessExpressionSyntax)expressionSyntax.Expression, semanticModel).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];

        mutation.Type.Should().Be(Mutator.StringMethod);
        mutation.DisplayName.Should().Be(expectedDisplayName);

        var access = mutation.ReplacementNode.Should().BeOfType<MemberAccessExpressionSyntax>().Which;
        access.Name.Identifier.Text.Should().Be(mutatedMethod);
    }

    [Fact]
    public void ShouldNotMutateWhenNotAString()
    {
        var expression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("Enumerable.Max(new[] { 1, 2, 3 })");
        var target = new StringMethodMutator();
        var result = target.ApplyMutations((MemberAccessExpressionSyntax)expression.Expression, null!).ToList();

        result.Should().BeEmpty();
    }
}
