using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;
using Stryker.Regex.Parser.Nodes.QuantifierNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public sealed class QuantifierReluctantAdditionMutatorTests
{
    [Theory]
    [InlineData("ab+", new[] { "ab+?" })]
    [InlineData("ab*", new[] { "ab*?" })]
    [InlineData("ab?", new[] { "ab??" })]
    [InlineData("ab{2,}", new[] { "ab{2,}?" })]
    public void QuantifierReluctantAddition(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new QuantifierReluctantAdditionMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("ab+?")]
    [InlineData("ab*?")]
    [InlineData("ab??")]
    [InlineData("ab{2,}?")]
    public void DoesNotMutateLazyQuantifierNodes(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new QuantifierReluctantAdditionMutator());

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("(abc)")]
    [InlineData("(?:def)")]
    [InlineData("Alice")]
    [InlineData(@"\d\w")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Distinct test contracts: 'lazy quantifier' vs 'non-quantity'. Preserves upstream test surface.")]
    public void DoesNotMutateNonQuantityNodes(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new QuantifierReluctantAdditionMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateQuantifier()
    {
        var classNode = new CharacterClassNode(new CharacterClassCharacterSetNode([
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        ]), false);
        var quantity = new QuantifierPlusNode(classNode);
        var rootNode = new ConcatenationNode(quantity);
        var target = new QuantifierReluctantAdditionMutator();

        var result = target.ApplyMutations(quantity, rootNode).ToList();

        result.Should().ContainSingle();
        var regexMutation = result[0];
        regexMutation.OriginalNode.Should().Be(quantity);
        regexMutation.ReplacementNode!.ToString().Should().Be("[abc]+?");
        regexMutation.ReplacementPattern.Should().Be("[abc]+?");
        regexMutation.DisplayName.Should().Be("Regex greedy quantifier to reluctant quantifier modification");
        regexMutation.Description.Should().Be("""Quantifier "[abc]+" was replace with "[abc]+?" at offset 5.""");
    }
}
