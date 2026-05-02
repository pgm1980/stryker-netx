using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/Mutators/CharacterClassShortHandNegationMutatorTest.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class CharacterClassShortHandNegationMutatorTest
{
    [Fact]
    public void ShouldNegateUnnegatedShorthand()
    {
        var shorthandNode = new CharacterClassShorthandNode('d');
        var childNodes = new List<RegexNode>
        {
            shorthandNode,
            new CharacterNode('a'),
            new CharacterNode('b'),
            new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassShorthandNegationMutator();

        var result = target.ApplyMutations(shorthandNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(shorthandNode);
        mutation.ReplacementNode!.ToString().Should().Be("\\D");
        mutation.ReplacementPattern.Should().Be("\\Dabc");
        mutation.DisplayName.Should().Be("Regex character class shorthand negation mutation");
        mutation.Description.Should().Be("Character class shorthand \"\\d\" was replaced with \"\\D\" at offset 0.");
    }

    [Fact]
    public void ShouldUnnegateNegatedShorthand()
    {
        var shorthandNode = new CharacterClassShorthandNode('D');
        var childNodes = new List<RegexNode>
        {
            shorthandNode,
            new CharacterNode('a'),
            new CharacterNode('b'),
            new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassShorthandNegationMutator();

        var result = target.ApplyMutations(shorthandNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(shorthandNode);
        mutation.ReplacementNode!.ToString().Should().Be("\\d");
        mutation.ReplacementPattern.Should().Be("\\dabc");
        mutation.DisplayName.Should().Be("Regex character class shorthand negation mutation");
        mutation.Description.Should().Be("Character class shorthand \"\\D\" was replaced with \"\\d\" at offset 0.");
    }

    [Fact]
    public void MutateShouldNotMutateNonCharacterClassShorthandNode()
    {
        var characterNode = new CharacterNode('a');
        var rootNode = new ConcatenationNode(characterNode);
        var target = new CharacterClassShorthandNegationMutator();

        var result = target.Mutate(characterNode, rootNode);

        result.Should().BeEmpty();
    }
}
