using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.GroupNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/Mutators/LookAroundMutatorTest.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class LookAroundMutatorTest
{
    [Fact]
    public void FlipsPositiveLookBehindToNegativeLookBehind()
    {
        var foo = new List<RegexNode>
        {
            new CharacterNode('f'),
            new CharacterNode('o'),
            new CharacterNode('o'),
        };
        var lookaroundGroupNode = new LookaroundGroupNode(false, true, foo);
        var rootNode = new ConcatenationNode(lookaroundGroupNode);
        var target = new LookAroundMutator();

        var result = target.ApplyMutations(lookaroundGroupNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(lookaroundGroupNode);
        mutation.ReplacementNode!.ToString().Should().Be("(?<!foo)");
        mutation.ReplacementPattern.Should().Be("(?<!foo)");
        mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        mutation.Description.Should().Be("Quantifier \"(?<=foo)\" was replaced with \"(?<!foo)\" at offset 0.");
    }

    [Fact]
    public void FlipsNegativeLookAheadToPositiveLookAhead()
    {
        var foo = new List<RegexNode>
        {
            new CharacterNode('f'),
            new CharacterNode('o'),
            new CharacterNode('o'),
        };
        var lookaroundGroupNode = new LookaroundGroupNode(true, false, foo);
        var rootNode = new ConcatenationNode(lookaroundGroupNode);
        var target = new LookAroundMutator();

        var result = target.ApplyMutations(lookaroundGroupNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(lookaroundGroupNode);
        mutation.ReplacementNode!.ToString().Should().Be("(?=foo)");
        mutation.ReplacementPattern.Should().Be("(?=foo)");
        mutation.DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        mutation.Description.Should().Be("Quantifier \"(?!foo)\" was replaced with \"(?=foo)\" at offset 0.");
    }
}
