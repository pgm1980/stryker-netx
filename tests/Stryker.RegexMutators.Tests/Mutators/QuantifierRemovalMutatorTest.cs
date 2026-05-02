using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.QuantifierNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 44 (v2.31.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class QuantifierRemovalMutatorTest
{
    [Fact]
    public void ShouldRemoveQuantifierStar()
    {
        var quantifierNode = new QuantifierStarNode(new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"*\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveQuantifierPlus()
    {
        var quantifierNode = new QuantifierPlusNode(new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"+\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveQuantifierQuestionMark()
    {
        var quantifierNode = new QuantifierQuestionMarkNode(new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"?\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveQuantifierN()
    {
        var quantifierNode = new QuantifierNNode(5, new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"{5}\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveQuantifierNOrMore()
    {
        var quantifierNode = new QuantifierNOrMoreNode(5, new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"{5,}\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveQuantifierNM()
    {
        var quantifierNode = new QuantifierNMNode(5, 10, new CharacterNode('X'));
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), quantifierNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(quantifierNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"{5,10}\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveLazyQuantifier()
    {
        var quantifierNode = new QuantifierStarNode(new CharacterNode('X'));
        var lazyNode = new LazyNode(quantifierNode);
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), lazyNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new QuantifierRemovalMutator();

        var result = target.ApplyMutations(quantifierNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(lazyNode);
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"*?\" was removed at offset 4.");
    }

    [Fact]
    public void MutateShouldNotMutateNonQuantifierNode()
    {
        var characterNode = new CharacterNode('a');
        var rootNode = new ConcatenationNode(characterNode);
        var target = new QuantifierRemovalMutator();

        var result = target.Mutate(characterNode, rootNode);

        result.Should().BeEmpty();
    }
}
