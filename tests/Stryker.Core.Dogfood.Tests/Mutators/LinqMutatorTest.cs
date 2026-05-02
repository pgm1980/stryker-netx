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
public class LinqMutatorTest
{
    private static MemberAccessExpressionSyntax GenerateExpressions(string expression)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestApplication
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            IEnumerable<string> Test = new[] {{}};

            Test.{expression}(_ => _!=null);
        }}
    }}
}}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Single();
    }

    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new LinqMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData(LinqExpression.FirstOrDefault, LinqExpression.First)]
    [InlineData(LinqExpression.First, LinqExpression.FirstOrDefault)]
    [InlineData(LinqExpression.SingleOrDefault, LinqExpression.Single)]
    [InlineData(LinqExpression.Single, LinqExpression.SingleOrDefault)]
    [InlineData(LinqExpression.Last, LinqExpression.First)]
    [InlineData(LinqExpression.All, LinqExpression.Any)]
    [InlineData(LinqExpression.Any, LinqExpression.All)]
    [InlineData(LinqExpression.Skip, LinqExpression.Take)]
    [InlineData(LinqExpression.Take, LinqExpression.Skip)]
    [InlineData(LinqExpression.SkipWhile, LinqExpression.TakeWhile)]
    [InlineData(LinqExpression.TakeWhile, LinqExpression.SkipWhile)]
    [InlineData(LinqExpression.Min, LinqExpression.Max)]
    [InlineData(LinqExpression.Max, LinqExpression.Min)]
    [InlineData(LinqExpression.Sum, LinqExpression.Max)]
    [InlineData(LinqExpression.Average, LinqExpression.Min)]
    [InlineData(LinqExpression.OrderBy, LinqExpression.OrderByDescending)]
    [InlineData(LinqExpression.OrderByDescending, LinqExpression.OrderBy)]
    [InlineData(LinqExpression.ThenBy, LinqExpression.ThenByDescending)]
    [InlineData(LinqExpression.ThenByDescending, LinqExpression.ThenBy)]
    [InlineData(LinqExpression.Reverse, LinqExpression.AsEnumerable)]
    [InlineData(LinqExpression.AsEnumerable, LinqExpression.Reverse)]
    [InlineData(LinqExpression.Union, LinqExpression.Intersect)]
    [InlineData(LinqExpression.Intersect, LinqExpression.Union)]
    [InlineData(LinqExpression.Concat, LinqExpression.Except)]
    [InlineData(LinqExpression.Except, LinqExpression.Concat)]
    [InlineData(LinqExpression.MinBy, LinqExpression.MaxBy)]
    [InlineData(LinqExpression.MaxBy, LinqExpression.MinBy)]
    [InlineData(LinqExpression.SkipLast, LinqExpression.TakeLast)]
    [InlineData(LinqExpression.TakeLast, LinqExpression.SkipLast)]
    [InlineData(LinqExpression.Order, LinqExpression.OrderDescending)]
    [InlineData(LinqExpression.OrderDescending, LinqExpression.Order)]
    [InlineData(LinqExpression.UnionBy, LinqExpression.IntersectBy)]
    [InlineData(LinqExpression.IntersectBy, LinqExpression.UnionBy)]
    [InlineData(LinqExpression.Append, LinqExpression.Prepend)]
    [InlineData(LinqExpression.Prepend, LinqExpression.Append)]
    public void ShouldMutate(LinqExpression original, LinqExpression expected)
    {
        var target = new LinqMutator();

        var expression = GenerateExpressions(original.ToString());

        var result = target.ApplyMutations(expression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        var replacement = mutation.ReplacementNode.Should().BeOfType<MemberAccessExpressionSyntax>().Which;
        replacement.Name.Identifier.ValueText.Should().Be(expected.ToString());

        mutation.DisplayName.Should().Be($"Linq method mutation ({original}() to {expected}())");
    }

    [Theory]
    [InlineData("AllData")]
    [InlineData("PriceFirstOrDefault")]
    [InlineData("TakeEntry")]
    [InlineData("ShouldNotMutate")]
    [InlineData("WriteLine")]
    public void ShouldNotMutate(string methodName)
    {
        var target = new LinqMutator();

        var result = target.ApplyMutations(GenerateExpressions(methodName), null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateProperlyConditionalExpression()
    {
        var tree = CSharpSyntaxTree.ParseText(@"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<string> Test = new[] {};

            Test?.First.Second.Third.All(_ => _!=null);
        }
    }
}");
        var memberAccessExpression = tree
            .GetRoot()
            .DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>().Single(x => string.Equals(x.Name.ToString(), "All", System.StringComparison.Ordinal));
        var target = new LinqMutator();

        var result = target.ApplyMutations(memberAccessExpression, null!).ToList();

        result.Should().ContainSingle();
        result[0].ReplacementNode.Should().BeOfType<MemberAccessExpressionSyntax>()
            .Which.Name.ToString().Should().Be("Any");
    }
}
