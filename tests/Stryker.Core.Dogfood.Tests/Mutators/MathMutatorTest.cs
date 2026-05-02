using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 69 (v2.55.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Mutators/MathMutatorTest.cs (subset).
/// MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: MathMutator may use logger via base class.
///
/// Subset port: ShouldBeMutationLevelAdvanced + ShouldNotMutateOtherMethods (8 inline)
/// + ShouldNotMutateOtherClasses (2 inline) + 2 explicit ShouldMutateStatic tests.
/// Skipped: 2 DynamicData [DataMember]-driven tests (need full MethodSwapsTestData enumeration —
/// defer to a future "MathMutator structural rewrite" sprint).
/// </summary>
public class MathMutatorTest : TestBase
{
    private static InvocationExpressionSyntax GenerateClassCallExpression(string memberName, string expression)
    {
        var tree = CSharpSyntaxTree.ParseText($$"""
            using System;
            namespace TestApplication
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        {{memberName}}.{{expression}}(5.0);
                    }
                }
            }
            """);
        return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
    }

    [Fact]
    public void ShouldBeMutationLevelAdvanced()
    {
        var target = new MathMutator();
        target.MutationLevel.Should().Be(MutationLevel.Advanced);
    }

    [Theory]
    [InlineData("Abs")]
    [InlineData("Atan2")]
    [InlineData("Cbrt")]
    [InlineData("DivRem")]
    [InlineData("Log10")]
    [InlineData("Sqrt")]
    [InlineData("Min")]
    [InlineData("Max")]
    public void ShouldNotMutateOtherMethods(string methodName)
    {
        var target = new MathMutator();
        var result = target.ApplyMutations(GenerateClassCallExpression("Math", methodName), null!);
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("MyClass")]
    [InlineData("MyMath")]
    public void ShouldNotMutateOtherClasses(string className)
    {
        var target = new MathMutator();
        var result = target.ApplyMutations(GenerateClassCallExpression(className, "Floor"), null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateStaticFloorToCeiling()
    {
        var sourceCode = """
            using System;
            using static System.Math;
            namespace TestApplication
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Floor(5.0);
                    }
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create("TestCompilation")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Math).Assembly.Location))
            .AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        var target = new MathMutator();
        var result = target.ApplyMutations(expression, semanticModel).ToList();

        result.Should().ContainSingle();
        var mutatedMethodName = ((IdentifierNameSyntax)((InvocationExpressionSyntax)result[0].ReplacementNode).Expression).Identifier.ValueText;
        Enum.Parse<MathExpression>(mutatedMethodName).Should().Be(MathExpression.Ceiling);
    }
}
