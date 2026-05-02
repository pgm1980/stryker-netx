using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 49 (v2.36.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase to seed ApplicationLogging.LoggerFactory (RegexMutator ctor needs it).
/// </summary>
public class RegexMutatorTest : TestBase
{
    [Fact]
    public void ShouldBeMutationLevelAdvanced()
    {
        var target = new RegexMutator();
        target.MutationLevel.Should().Be(MutationLevel.Advanced);
    }

    [Fact]
    public void ShouldMutateStringLiteralInRegexConstructor()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new Regex(@\"^abc\")") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        var replacementNode = mutation.ReplacementNode as ObjectCreationExpressionSyntax;
        replacementNode.Should().NotBeNull();
        var argument = replacementNode!.ArgumentList!.Arguments.First().Expression as LiteralExpressionSyntax;
        argument.Should().NotBeNull();
        argument!.Token.ValueText.Should().Be("abc");
    }

    [Fact]
    public void ShouldMutateStringLiteralInRegexConstructorWithFullName()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new System.Text.RegularExpressions.Regex(@\"^abc\")") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        var replacementNode = mutation.ReplacementNode as ObjectCreationExpressionSyntax;
        replacementNode.Should().NotBeNull();
        replacementNode!.Type.ToString().Should().Be("System.Text.RegularExpressions.Regex");
        var argument = replacementNode.ArgumentList!.Arguments.FirstOrDefault();
        argument.Should().NotBeNull();
        argument!.ToString().Should().Be("\"abc\"");
    }

    [Fact]
    public void ShouldNotMutateRegexWithoutParameters()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new Regex()") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();
        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStringLiteralInOtherConstructor()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new Other(@\"^abc\")") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();
        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateStringLiteralMultipleTimes()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new Regex(@\"^abc$\")") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Count.Should().Be(2);
        result.Should().AllSatisfy(mutant => mutant.DisplayName.Should().Be("Regex anchor removal mutation"));

        var firstReplacement = result[0].ReplacementNode as ObjectCreationExpressionSyntax;
        firstReplacement.Should().NotBeNull();
        firstReplacement!.ArgumentList!.Arguments.Count.Should().Be(1);
        firstReplacement.ArgumentList.Arguments[0].Expression.Should().BeOfType<LiteralExpressionSyntax>();
        ((LiteralExpressionSyntax)firstReplacement.ArgumentList.Arguments[0].Expression).Token.ValueText.Should().Be("abc$");

        var lastReplacement = result[1].ReplacementNode as ObjectCreationExpressionSyntax;
        lastReplacement.Should().NotBeNull();
        lastReplacement!.ArgumentList!.Arguments.Count.Should().Be(1);
        lastReplacement.ArgumentList.Arguments[0].Expression.Should().BeOfType<LiteralExpressionSyntax>();
        ((LiteralExpressionSyntax)lastReplacement.ArgumentList.Arguments[0].Expression).Token.ValueText.Should().Be("^abc");
    }

    [Fact]
    public void ShouldMutateStringLiteralAsNamedArgumentPatternInRegexConstructor()
    {
        var objectCreationExpression = (SyntaxFactory.ParseExpression("new Regex(options: RegexOptions.None, pattern: @\"^abc\")") as ObjectCreationExpressionSyntax)!;
        var target = new RegexMutator();

        var result = target.ApplyMutations(objectCreationExpression, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        var replacementNode = mutation.ReplacementNode as ObjectCreationExpressionSyntax;
        replacementNode.Should().NotBeNull();

        var argumentList = replacementNode!.ArgumentList;
        argumentList.Should().NotBeNull();
        argumentList!.Arguments.Count.Should().Be(2);

        var patternArgument = argumentList.Arguments.First(arg => string.Equals(arg.NameColon?.Name.Identifier.Text, "pattern", System.StringComparison.Ordinal));
        patternArgument.Should().NotBeNull();
        patternArgument.Expression.ToString().Should().Be("\"abc\"");

        var optionsArgument = argumentList.Arguments.First(arg => string.Equals(arg.NameColon?.Name.Identifier.Text, "options", System.StringComparison.Ordinal));
        optionsArgument.Should().NotBeNull();
        optionsArgument.Expression.ToString().Should().Be("RegexOptions.None");
    }
}
