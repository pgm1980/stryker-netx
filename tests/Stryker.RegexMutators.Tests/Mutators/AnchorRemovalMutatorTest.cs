using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.AnchorNodes;
using Stryker.Regex.Parser.Nodes.GroupNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 44 (v2.31.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class AnchorRemovalMutatorTest
{
    [Fact]
    public void ShouldRemoveStartOfLineNode()
    {
        var startOfLineNode = new StartOfLineNode();
        var childNodes = new List<RegexNode>
        {
            startOfLineNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(startOfLineNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(startOfLineNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"^\" was removed at offset 0.");
    }

    [Fact]
    public void ShouldRemoveEndOfLineNode()
    {
        var endOfLineNode = new EndOfLineNode();
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), endOfLineNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(endOfLineNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(endOfLineNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"$\" was removed at offset 3.");
    }

    [Fact]
    public void ShouldRemoveStartOfStringNode()
    {
        var startOfStringNode = new StartOfStringNode();
        var childNodes = new List<RegexNode>
        {
            startOfStringNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(startOfStringNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(startOfStringNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\A\" was removed at offset 0.");
    }

    [Fact]
    public void ShouldRemoveEndOfStringNode()
    {
        var endOfStringNode = new EndOfStringNode();
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), endOfStringNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(endOfStringNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(endOfStringNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\z\" was removed at offset 3.");
    }

    [Fact]
    public void ShouldRemoveEndOfStringZNode()
    {
        var endOfStringZNode = new EndOfStringZNode();
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), endOfStringZNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(endOfStringZNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(endOfStringZNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\Z\" was removed at offset 3.");
    }

    [Fact]
    public void ShouldRemoveWordBoundaryNode()
    {
        var wordBoundaryNode = new WordBoundaryNode();
        var childNodes = new List<RegexNode>
        {
            wordBoundaryNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(wordBoundaryNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(wordBoundaryNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\b\" was removed at offset 0.");
    }

    [Fact]
    public void ShouldRemoveNonWordBoundaryNode()
    {
        var nonWordBoundaryNode = new NonWordBoundaryNode();
        var childNodes = new List<RegexNode>
        {
            nonWordBoundaryNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(nonWordBoundaryNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(nonWordBoundaryNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\B\" was removed at offset 0.");
    }

    [Fact]
    public void ShouldRemoveContiguousMatchNode()
    {
        var contiguousMatchNode = new ContiguousMatchNode();
        var childNodes = new List<RegexNode>
        {
            contiguousMatchNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(contiguousMatchNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(contiguousMatchNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"\\G\" was removed at offset 0.");
    }

    [Fact]
    public void MutateShouldNotMutateNonAnchorNode()
    {
        var characterNode = new CharacterNode('a');
        var rootNode = new ConcatenationNode(characterNode);
        var target = new AnchorRemovalMutator();

        var result = target.Mutate(characterNode, rootNode);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MutationShouldNotContainOriginalNodesPrefixInDescription()
    {
        var prefix = new CommentGroupNode("This is a comment.");
        var startOfLineNode = new StartOfLineNode { Prefix = prefix };
        var childNodes = new List<RegexNode>
        {
            startOfLineNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new AnchorRemovalMutator();

        var result = target.ApplyMutations(startOfLineNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(startOfLineNode);
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("(?#This is a comment.)abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"^\" was removed at offset 22.");
    }
}
