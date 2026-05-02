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
public class StringMethodToConstantMutatorTests
{
    [Theory]
    [InlineData("Trim")]
    [InlineData("Substring")]
    public void ShouldMutateReplaceWithEmptyString(string methodName)
    {
        var expression = $"testString.{methodName}()";
        var (semanticModel, expressionSyntax) = CreateSemanticModelFromExpression(expression);
        var target = new StringMethodToConstantMutator();
        var result = target.ApplyMutations(expressionSyntax, semanticModel).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.Type.Should().Be(Mutator.StringMethod);
        mutation.DisplayName.Should().Be($"String Method Mutation (Replace {methodName}() with Empty String)");

        var syntax = mutation.ReplacementNode.Should().BeOfType<LiteralExpressionSyntax>().Which;
        syntax.Token.ValueText.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("ElementAt")]
    [InlineData("ElementAtOrDefault")]
    public void ShouldMutateReplaceWithChar(string methodName)
    {
        var expression = $"testString.{methodName}()";
        var (semanticModel, expressionSyntax) = CreateSemanticModelFromExpression(expression);
        var target = new StringMethodToConstantMutator();
        var result = target.ApplyMutations(expressionSyntax, semanticModel).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.Type.Should().Be(Mutator.StringMethod);
        mutation.DisplayName.Should().Be($"String Method Mutation (Replace {methodName}() with '\\0' char)");

        var syntax = mutation.ReplacementNode.Should().BeOfType<LiteralExpressionSyntax>().Which;
        syntax.Token.ValueText.Should().Be(char.MinValue.ToString());
    }

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
}
