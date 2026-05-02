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
public class NegateConditionMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new NegateConditionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    private static InvocationExpressionSyntax GenerateExpressions(string expression)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
using System;

namespace TestApplication
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            {expression}
        }}
    }}
}}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single();
    }

    [Theory]
    [InlineData("if (Method()) => return true;")]
    [InlineData("while (Method()) => age++;")]
    public void MutatesStatementWithMethodCallWithNoArguments(string method)
    {
        var target = new NegateConditionMutator();

        var node = GenerateExpressions(method);

        var result = target.ApplyMutations(node, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.ToString().Should().Be("!(Method())");
        mutation.DisplayName.Should().Be("Negate expression");
    }

    [Theory]
    [InlineData("var y = x is object result ? result.ToString() : null;")]
    public void ShouldNotMutateThis(string method)
    {
        var target = new NegateConditionMutator();
        var tree = CSharpSyntaxTree.ParseText(method);

        var expressionSyntax = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
        var result = target.ApplyMutations(expressionSyntax.Condition, null!).ToList();

        result.Should().BeEmpty();
    }
}
