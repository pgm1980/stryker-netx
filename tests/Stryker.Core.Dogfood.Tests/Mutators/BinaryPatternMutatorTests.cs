using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 47 (v2.34.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class BinaryPatternMutatorTests
{
    [Theory]
    [InlineData("and", new[] { SyntaxKind.OrPattern })]
    [InlineData("or", new[] { SyntaxKind.AndPattern })]
    public void ShouldMutateLogicalPattern(string @operator, SyntaxKind[] mutated)
    {
        var target = new BinaryPatternMutator();

        var expression = GenerateWithBinaryPattern(@operator);

        var result = target.ApplyMutations(expression, null!).ToList();

        foreach (var mutation in result)
        {
            mutation.OriginalNode.Should().BeOfType<BinaryPatternSyntax>();
            mutation.ReplacementNode.Should().BeOfType<BinaryPatternSyntax>();
            mutation.DisplayName.Should().Be("Logical mutation");
        }

        result
            .Select(mutation => (BinaryPatternSyntax)mutation.ReplacementNode)
            .Select(pattern => pattern.Kind())
            .Should().BeEquivalentTo(mutated);
    }

    private static BinaryPatternSyntax GenerateWithBinaryPattern(string pattern)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
using System;

namespace TestApplication
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var a = 1 is (1 {pattern} 2);
        }}
    }}
}}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<BinaryPatternSyntax>()
            .Single();
    }
}
