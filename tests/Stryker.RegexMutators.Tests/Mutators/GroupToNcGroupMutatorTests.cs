using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.GroupNodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/Mutators/GroupToNcGroupMutatorTests.cs.
/// Framework conversion: MSTest+DataRow custom attribute → xUnit Theory+InlineData (using string[] params).
/// </summary>
public sealed class GroupToNcGroupMutatorTests
{
    [Theory]
    [InlineData("([abc])", new[] { "(?:[abc])" })]
    [InlineData("([abc][bcd])", new[] { "(?:[abc][bcd])" })]
    [InlineData(@"([\w\W])([bcd])", new[] { @"(?:[\w\W])([bcd])", @"([\w\W])(?:[bcd])" })]
    [InlineData("(([bcd])([bcd]))", new[] { "((?:[bcd])([bcd]))", "(([bcd])(?:[bcd]))", "(?:([bcd])([bcd]))" })]
    public void GroupToNcGroup(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new GroupToNcGroupMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("(?:def)")]
    [InlineData("Alice")]
    [InlineData(@"\d+\w{2,}")]
    public void DoesNotMutateNonCaptureGroups(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new GroupToNcGroupMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void FlipsCaptureGroupToNonCaptureGroup()
    {
        var lookaroundGroupNode = new CaptureGroupNode([
            new CharacterNode('f'), new CharacterNode('o'), new CharacterNode('o'),
        ]);
        var rootNode = new ConcatenationNode(lookaroundGroupNode);
        var target = new GroupToNcGroupMutator();

        var result = target.ApplyMutations(lookaroundGroupNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(lookaroundGroupNode);
        mutation.ReplacementNode!.ToString().Should().Be("(?:foo)");
        mutation.ReplacementPattern.Should().Be("(?:foo)");
        mutation.DisplayName.Should().Be("Regex capturing group to non-capturing group modification");
        mutation.Description.Should().Be("""Capturing group "(foo)" was replaced with "(?:foo)" at offset 0.""");
    }
}
