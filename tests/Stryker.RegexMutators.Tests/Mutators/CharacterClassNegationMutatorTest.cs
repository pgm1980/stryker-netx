using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/Mutators/CharacterClassNegationMutatorTest.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class CharacterClassNegationMutatorTest
{
    [Fact]
    public void ShouldNegateUnnegatedCharacterClass()
    {
        var characters = new List<RegexNode> { new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c') };
        var characterSet = new CharacterClassCharacterSetNode(characters);
        var subtractionCharacterSet = new CharacterClassCharacterSetNode(new CharacterNode('a'));
        var subtraction = new CharacterClassNode(subtractionCharacterSet, false);
        var characterClass = new CharacterClassNode(characterSet, subtraction, false);
        var childNodes = new List<RegexNode> { new CharacterNode('x'), characterClass, new CharacterNode('y') };
        var root = new ConcatenationNode(childNodes);
        var target = new CharacterClassNegationMutator();

        var result = target.ApplyMutations(characterClass, root).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(characterClass);
        mutation.ReplacementNode!.ToString().Should().Be("[^abc-[a]]");
        mutation.ReplacementPattern.Should().Be("x[^abc-[a]]y");
        mutation.DisplayName.Should().Be("Regex character class negation mutation");
        mutation.Description.Should().Be("Character class \"[abc-[a]]\" was replaced with \"[^abc-[a]]\" at offset 1.");
    }

    [Fact]
    public void ShouldUnnegateNegatedCharacterClass()
    {
        var characters = new List<RegexNode> { new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c') };
        var characterSet = new CharacterClassCharacterSetNode(characters);
        var subtractionCharacterSet = new CharacterClassCharacterSetNode(new CharacterNode('a'));
        var subtraction = new CharacterClassNode(subtractionCharacterSet, false);
        var characterClass = new CharacterClassNode(characterSet, subtraction, true);
        var childNodes = new List<RegexNode> { new CharacterNode('x'), characterClass, new CharacterNode('y') };
        var root = new ConcatenationNode(childNodes);
        var target = new CharacterClassNegationMutator();

        var result = target.ApplyMutations(characterClass, root).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(characterClass);
        mutation.ReplacementNode!.ToString().Should().Be("[abc-[a]]");
        mutation.ReplacementPattern.Should().Be("x[abc-[a]]y");
        mutation.DisplayName.Should().Be("Regex character class negation mutation");
        mutation.Description.Should().Be("Character class \"[^abc-[a]]\" was replaced with \"[abc-[a]]\" at offset 1.");
    }

    [Fact]
    public void MutateShouldNotMutateNonCharacterClassNode()
    {
        var characterNode = new CharacterNode('a');
        var root = new ConcatenationNode(characterNode);
        var target = new CharacterClassNegationMutator();

        var result = target.Mutate(characterNode, root);

        result.Should().BeEmpty();
    }
}
