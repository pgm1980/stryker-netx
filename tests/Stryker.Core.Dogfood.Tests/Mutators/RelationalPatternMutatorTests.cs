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
public class RelationalPatternMutatorTests
{
    [Theory]
    [InlineData(">", new[] { SyntaxKind.LessThanToken, SyntaxKind.GreaterThanEqualsToken })]
    [InlineData("<", new[] { SyntaxKind.GreaterThanToken, SyntaxKind.LessThanEqualsToken })]
    [InlineData(">=", new[] { SyntaxKind.GreaterThanToken, SyntaxKind.LessThanToken })]
    [InlineData("<=", new[] { SyntaxKind.GreaterThanToken, SyntaxKind.LessThanToken })]
    public void ShouldMutateRelationalPattern(string @operator, SyntaxKind[] mutated)
    {
        var target = new RelationalPatternMutator();

        var expression = GenerateWithRelationalPattern(@operator).DescendantNodes().OfType<RelationalPatternSyntax>().First();

        var result = target.ApplyMutations(expression, null!).ToList();

        foreach (var mutation in result)
        {
            mutation.OriginalNode.Should().BeOfType<RelationalPatternSyntax>();
            mutation.ReplacementNode.Should().BeOfType<RelationalPatternSyntax>();
            mutation.DisplayName.Should().Be("Equality mutation");
        }

        result
            .Select(mutation => (RelationalPatternSyntax)mutation.ReplacementNode)
            .Select(pattern => pattern.OperatorToken.Kind())
            .Should().BeEquivalentTo(mutated);
    }

    private static IsPatternExpressionSyntax GenerateWithRelationalPattern(string @operator)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
using System;

namespace TestApplication
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var a = 1 is ({@operator} 1);
        }}
    }}
}}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<IsPatternExpressionSyntax>()
            .Single();
    }
}
