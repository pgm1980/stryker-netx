using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 48 (v2.35.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class StringMutatorTests
{
    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new StringMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData("", "Stryker was here!")]
    [InlineData("foo", "")]
    public void ShouldMutate(string original, string expected)
    {
        var node = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(original));
        var mutator = new StringMutator();

        var result = mutator.ApplyMutations(node, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.ReplacementNode.Should().BeOfType<LiteralExpressionSyntax>()
            .Which.Token.Value.Should().Be(expected);
        mutation.DisplayName.Should().Be("String mutation");
    }

    [Fact]
    public void ShouldNotMutateOnRegexExpression()
    {
        var expressionSyntax = SyntaxFactory.ParseExpression("new Regex(\"myregex\")");
        var literalExpression = expressionSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateOnFullyDefinedRegexExpression()
    {
        var expressionSyntax = SyntaxFactory.ParseExpression("new System.Text.RegularExpressions.Regex(\"myregex\")");
        var literalExpression = expressionSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateOnRegularExpressionInClass()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"using System.Text.RegularExpressions;
namespace Stryker.Core.UnitTest.Mutators
{
    public class Test {
        public Regex GetRegex(){
            return new Regex(""myregex"");
        }
    }
}
");
        var literalExpression = syntaxTree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateOnGuidExpression()
    {
        var expressionSyntax = SyntaxFactory.ParseExpression("new Guid(\"00000-0000\")");
        var literalExpression = expressionSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateOnFullyDefinedGuidExpression()
    {
        var expressionSyntax = SyntaxFactory.ParseExpression("new System.Guid(\"00000-0000\")");
        var literalExpression = expressionSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateOnGuidInClass()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"using System;
namespace Stryker.Core.UnitTest.Mutators
{
    public class Test {
        public Guid GetGuid(){
            return new Guid(""00000-0000"");
        }
    }
}
");
        var literalExpression = syntaxTree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        var mutator = new StringMutator();
        var result = mutator.ApplyMutations(literalExpression, null!).ToList();

        result.Should().BeEmpty();
    }
}
