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
public sealed class CharacterClassShorthandAnyCharMutatorTests
{
    [Theory]
    [InlineData(@"\w\W\d\D\s\S", new[]
    {
        @"[\w\W]\W\d\D\s\S",
        @"\w[\W\w]\d\D\s\S",
        @"\w\W[\d\D]\D\s\S",
        @"\w\W\d[\D\d]\s\S",
        @"\w\W\d\D[\s\S]\S",
        @"\w\W\d\D\s[\S\s]",
    })]
    public void CharacterClassToAnyChar(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new CharacterClassShorthandAnyCharMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("(abc)")]
    [InlineData("(?:def)")]
    [InlineData("Alice")]
    [InlineData(@"\n+\t{2,}")]
    public void DoesNotMutateNonCharacterClasses(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassShorthandAnyCharMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateCharacterClassShorthand()
    {
        var shorthandNode = new CharacterClassShorthandNode('w');
        var rootNode = new ConcatenationNode(shorthandNode);
        var target = new CharacterClassShorthandAnyCharMutator();

        var result = target.ApplyMutations(shorthandNode, rootNode).ToList();

        result.Should().ContainSingle();
        var regexMutation = result[0];
        regexMutation.OriginalNode.Should().Be(shorthandNode);
        regexMutation.ReplacementNode!.ToString().Should().Be(@"[\w\W]");
        regexMutation.ReplacementPattern.Should().Be(@"[\w\W]");
        regexMutation.DisplayName.Should().Be("Regex predefined character class to character class with its negation change");
        regexMutation.Description.Should().Be("""Character class shorthand "\w" was replaced with "[\w\W]" at offset 1.""");
    }
}
