using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;
using Stryker.RegexMutators;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 44 (v2.31.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest [DataTestMethod] → xUnit [Theory], Shouldly → FluentAssertions.
/// </summary>
public sealed class CharacterClassRangeMutatorTests
{
    [Theory]
    [InlineData("[b-y][B-Y][1-8]", new[]
    {
        "[a-y][B-Y][1-8]", "[c-y][B-Y][1-8]", "[b-x][B-Y][1-8]", "[b-z][B-Y][1-8]",
        "[b-y][A-Y][1-8]", "[b-y][C-Y][1-8]", "[b-y][B-X][1-8]", "[b-y][B-Z][1-8]",
        "[b-y][B-Y][0-8]", "[b-y][B-Y][2-8]", "[b-y][B-Y][1-7]", "[b-y][B-Y][1-9]",
    })]
    [InlineData("[a-y][A-Y][0-8]", new[]
    {
        "[b-y][A-Y][0-8]", "[a-x][A-Y][0-8]", "[a-z][A-Y][0-8]",
        "[a-y][B-Y][0-8]", "[a-y][A-X][0-8]", "[a-y][A-Z][0-8]",
        "[a-y][A-Y][1-8]", "[a-y][A-Y][0-7]", "[a-y][A-Y][0-9]",
    })]
    [InlineData("[b-z][B-Z][1-9]", new[]
    {
        "[a-z][B-Z][1-9]", "[c-z][B-Z][1-9]", "[b-y][B-Z][1-9]",
        "[b-z][A-Z][1-9]", "[b-z][C-Z][1-9]", "[b-z][B-Y][1-9]",
        "[b-z][B-Z][0-9]", "[b-z][B-Z][2-9]", "[b-z][B-Z][1-8]",
    })]
    [InlineData("[a-z][A-Z][0-9]", new[]
    {
        "[b-z][A-Z][0-9]", "[a-y][A-Z][0-9]",
        "[a-z][B-Z][0-9]", "[a-z][A-Y][0-9]",
        "[a-z][A-Z][1-9]", "[a-z][A-Z][0-8]",
    })]
    [InlineData("[b-b][B-B][1-1]", new[]
    {
        "[a-b][B-B][1-1]", "[b-c][B-B][1-1]",
        "[b-b][A-B][1-1]", "[b-b][B-C][1-1]",
        "[b-b][B-B][0-1]", "[b-b][B-B][1-2]",
    })]
    [InlineData("[a-a][A-A][0-0]", new[] { "[a-b][A-A][0-0]", "[a-a][A-B][0-0]", "[a-a][A-A][0-1]" })]
    [InlineData("[z-z][Z-Z][9-9]", new[] { "[y-z][Z-Z][9-9]", "[z-z][Y-Z][9-9]", "[z-z][Z-Z][8-9]" })]
    public void CharacterClassModifyRange(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new CharacterClassRangeMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(@"[\a-\f]")]
    [InlineData(@"[\ca-\cc]")]
    [InlineData(@"[\t-\n]")]
    public void DoesNotModifyNonAlphaNumericRanges(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassRangeMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateCharacterClassRange()
    {
        var leftNode = new CharacterNode('A');
        var rightNode = new CharacterNode('Z');
        var rangeNode = new CharacterClassRangeNode(leftNode, rightNode);
        var rootNode = new CharacterClassNode(new CharacterClassCharacterSetNode(rangeNode), false);
        var target = new CharacterClassRangeMutator();

        var result = target.ApplyMutations(rangeNode, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(2);
        regexMutations[0].OriginalNode.Should().Be(leftNode);
        regexMutations[0].ReplacementNode!.ToString().Should().Be("B");
        regexMutations[0].ReplacementPattern.Should().Be("[B-Z]");
        regexMutations[0].DisplayName.Should().Be("Regex character class range modification");
        regexMutations[0].Description.Should().Be("""Replaced character "A" with "B" at offset 1.""");

        regexMutations[1].OriginalNode.Should().Be(rightNode);
        regexMutations[1].ReplacementNode!.ToString().Should().Be("Y");
        regexMutations[1].ReplacementPattern.Should().Be("[A-Y]");
        regexMutations[1].DisplayName.Should().Be("Regex character class range modification");
        regexMutations[1].Description.Should().Be("""Replaced character "Z" with "Y" at offset 3.""");
    }

    [Fact]
    public void ShouldNotMutateInvalidCharacterClassRange()
    {
        var rangeNode = new CharacterClassRangeNode(new AnyCharacterNode(), new AnyCharacterNode());
        var childNodes = new List<RegexNode> { rangeNode };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassRangeMutator();

        var result = target.ApplyMutations(rangeNode, rootNode);

        result.Should().BeEmpty();
    }
}
