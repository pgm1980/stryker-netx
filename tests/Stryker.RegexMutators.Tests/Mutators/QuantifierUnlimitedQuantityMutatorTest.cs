using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.QuantifierNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class QuantifierUnlimitedQuantityMutatorTest
{
    [Fact]
    public void ShouldApplyVariationsOnRegularInput()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNOrMoreNode(5, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierUnlimitedQuantityMutator();

        var expectedResults = new List<string>
        {
            "a{4,}",
            "a{6,}",
        };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{5,}";
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
    public void ShouldSkipDecrementOnZeroStartValue()
    {
        var characterNode = new CharacterNode('a');
        var quantifierNode = new QuantifierNOrMoreNode(0, characterNode);
        var rootNode = new ConcatenationNode(quantifierNode);
        var target = new QuantifierUnlimitedQuantityMutator();

        var expectedResults = new List<string> { "a{1,}" };

        var mutations = target.ApplyMutations(quantifierNode, rootNode).ToList();

        var index = 0;
        const string OriginalQuantifier = "a{0,}";
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
}
