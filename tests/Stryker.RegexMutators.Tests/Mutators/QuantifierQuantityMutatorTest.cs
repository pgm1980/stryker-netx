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
public class QuantifierQuantityMutatorTest
{
    [Fact]
    public void ShouldApplyVariationsOnRegularInput()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNMNode(5, 8, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierQuantityMutator();

        var expectedResults = new List<string> { "a{4,8}", "a{6,8}", "a{5,7}", "a{5,9}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{5,8}";
        foreach (var mutation in mutations)
        {
            mutation.OriginalNode.Should().Be(quantifierNode);
            mutation.ReplacementNode!.ToString().Should().Be(expectedResults[index]);
            mutation.ReplacementPattern.Should().Be(expectedResults[index]);
            mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
            mutation.Description.Should().Be($"Quantifier \"{OriginalQuantifier}\" was replaced with \"{expectedResults[index]}\" at offset 1.");
            index++;
        }

        mutations.Count.Should().Be(4);
    }

    [Fact]
    public void ShouldSkipDecrementOnZeroStartValue()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNMNode(0, 8, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierQuantityMutator();

        var expectedResults = new List<string> { "a{1,8}", "a{0,7}", "a{0,9}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{0,8}";
        foreach (var mutation in mutations)
        {
            mutation.OriginalNode.Should().Be(quantifierNode);
            mutation.ReplacementNode!.ToString().Should().Be(expectedResults[index]);
            mutation.ReplacementPattern.Should().Be(expectedResults[index]);
            mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
            mutation.Description.Should().Be($"Quantifier \"{OriginalQuantifier}\" was replaced with \"{expectedResults[index]}\" at offset 1.");
            index++;
        }

        mutations.Count.Should().Be(3);
    }

    [Fact]
    public void ShouldSkipDecrementOnZeroEndValue()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNMNode(0, 0, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierQuantityMutator();

        var expectedResults = new List<string> { "a{0,1}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{0,0}";
        foreach (var mutation in mutations)
        {
            mutation.OriginalNode.Should().Be(quantifierNode);
            mutation.ReplacementNode!.ToString().Should().Be(expectedResults[index]);
            mutation.ReplacementPattern.Should().Be(expectedResults[index]);
            mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
            mutation.Description.Should().Be($"Quantifier \"{OriginalQuantifier}\" was replaced with \"{expectedResults[index]}\" at offset 1.");
            index++;
        }

        mutations.Count.Should().Be(1);
    }

    [Fact]
    public void ShouldSkipStartValueHigherThanEndValue()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNMNode(8, 8, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierQuantityMutator();

        var expectedResults = new List<string> { "a{7,8}", "a{8,9}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{8,8}";
        foreach (var mutation in mutations)
        {
            mutation.OriginalNode.Should().Be(quantifierNode);
            mutation.ReplacementNode!.ToString().Should().Be(expectedResults[index]);
            mutation.ReplacementPattern.Should().Be(expectedResults[index]);
            mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
            mutation.Description.Should().Be($"Quantifier \"{OriginalQuantifier}\" was replaced with \"{expectedResults[index]}\" at offset 1.");
            index++;
        }

        mutations.Count.Should().Be(2);
    }

    [Fact]
    public void ShouldAcceptInputWithLeadingZeros()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNMNode("008", "008", characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierQuantityMutator();

        var expectedResults = new List<string> { "a{7,8}", "a{8,9}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{008,008}";
        foreach (var mutation in mutations)
        {
            mutation.OriginalNode.Should().Be(quantifierNode);
            mutation.ReplacementNode!.ToString().Should().Be(expectedResults[index]);
            mutation.ReplacementPattern.Should().Be(expectedResults[index]);
            mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
            mutation.Description.Should().Be($"Quantifier \"{OriginalQuantifier}\" was replaced with \"{expectedResults[index]}\" at offset 1.");
            index++;
        }

        mutations.Count.Should().Be(2);
    }
}
