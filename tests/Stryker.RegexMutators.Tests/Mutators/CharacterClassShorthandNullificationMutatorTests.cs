using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public sealed class CharacterClassShorthandNullificationMutatorTests
{
    [Theory]
    [InlineData(@"\w\W\d\D\s\S", new[]
    {
        @"w\W\d\D\s\S",
        @"\wW\d\D\s\S",
        @"\w\Wd\D\s\S",
        @"\w\W\dD\s\S",
        @"\w\W\d\Ds\S",
        @"\w\W\d\D\sS",
    })]
    public void CharacterClassToAnyChar(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new CharacterClassShorthandNullificationMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("(abc)")]
    [InlineData("(?:def)")]
    [InlineData("Alice")]
    [InlineData(@"\n+\t{2,}")]
    public void DoesNotMutateNonCharacterClasses(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassShorthandNullificationMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateCharacterClassShorthand()
    {
        var shorthandNode = new CharacterClassShorthandNode('w');
        var rootNode = new ConcatenationNode(shorthandNode);
        var target = new CharacterClassShorthandNullificationMutator();

        var result = target.ApplyMutations(shorthandNode, rootNode).ToList();

        result.Should().ContainSingle();
        var regexMutation = result[0];
        regexMutation.OriginalNode.Should().Be(shorthandNode);
        regexMutation.ReplacementNode!.ToString().Should().Be("w");
        regexMutation.ReplacementPattern.Should().Be("w");
        regexMutation.DisplayName.Should().Be("Regex predefined character class nullification");
        regexMutation.Description.Should().Be("""Character class shorthand "\w" was replaced with "w" at offset 0.""");
    }
}
