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
/// Sprint 49 (v2.36.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class NullCoalescingExpressionMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new NullCoalescingExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Basic);
    }

    [Fact]
    public void ShouldMutate()
    {
        var target = new NullCoalescingExpressionMutator();
        const string OriginalExpressionString = "a ?? b";
        var expectedExpressionStrings = new[] { "a", "b", "b?? a" };
        var originalExpression = SyntaxFactory.ParseExpression(OriginalExpressionString);

        var result = target.ApplyMutations((originalExpression as BinaryExpressionSyntax)!, null!).ToList();

        result.Count.Should().Be(3);

        foreach (var mutant in result)
        {
            expectedExpressionStrings.Should().Contain(mutant.ReplacementNode.ToString());
        }
    }

    [Fact]
    public void ShouldMutateThrowExpression()
    {
        var target = new NullCoalescingExpressionMutator();
        const string OriginalExpressionString = "a ?? throw new ArgumentException(nameof(a))";
        var originalExpression = SyntaxFactory.ParseExpression(OriginalExpressionString);

        var result = target.ApplyMutations((originalExpression as BinaryExpressionSyntax)!, null!).ToList();

        result.Should().ContainSingle();
        var mutant = result[0];
        mutant.ReplacementNode.ToString().Should().Be("a");
    }

    [Fact]
    public void ShouldNotMutateLeftToRightOrRemoveLeftIfNotNullable()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System;
            public TimeSpan? GetLocalDateTime(DateTimeOffset startTime, DateTimeOffset? endTime)
            {
                return (endTime ?? startTime).LocalDateTime;
            }
            """);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var expression = syntaxTree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var target = new NullCoalescingExpressionMutator();
        var result = target.ApplyMutations(expression, semanticModel).ToList();

        result.Should().ContainSingle();
        result.Should().NotContain(x => x.Description == "Null coalescing mutation (left to right)");
        result.Should().NotContain(x => x.Description == "Null coalescing mutation (remove left)");
    }

    [Fact]
    public void ShouldMutateIfBothSidesAreNullable()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            public TimeSpan? GetLocalDateTime(DateTimeOffset? startTime, DateTimeOffset? endTime)
            {
                return (endTime ?? startTime)?.LocalDateTime;
            }
            """);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var expression = syntaxTree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var target = new NullCoalescingExpressionMutator();
        var result = target.ApplyMutations(expression, semanticModel).ToList();

        result.Count.Should().Be(3);
    }

    [Fact]
    public void ShouldMutateCollectionExpressions()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            public void GetLocalDateTime(Stream s)
            {
                AddAll(Deserialize(s, Enumerable.Empty<string>()) ?? [])
            }
            public void AddAll(IEnumerable<int> list)
            {

            }
            public IEnumerable<int>? Deserialize(Stream s, IEnumerable<string> s2) {
                return [];
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var expression = syntaxTree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
        var target = new NullCoalescingExpressionMutator();
        var result = target.ApplyMutations(expression, semanticModel).ToList();
        result.Count.Should().Be(2);
    }
}
