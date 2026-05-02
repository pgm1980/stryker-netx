using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public sealed class CharacterClassToAnyCharMutatorTests
{
    [Theory]
    [InlineData("[abc]", new[] { @"[\w\W]" })]
    [InlineData("[abc][bcd]", new[] { @"[\w\W][bcd]", @"[abc][\w\W]" })]
    [InlineData(@"[\w\W][bcd]", new[] { @"[\w\W][\w\W]" })]
    public void CharacterClassToAnyChar(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new CharacterClassToAnyCharMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(@"[\w\W]")]
    [InlineData(@"[\W\w]")]
    public void DoesNotMutateToItself(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassToAnyCharMutator());

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("(abc)")]
    [InlineData("(?:def)")]
    [InlineData("Alice")]
    [InlineData(@"\d+\w{2,}")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Distinct test contracts: 'to-itself' vs 'non-character-class'. Same body but different intent — preserves upstream test surface.")]
    public void DoesNotMutateNonCharacterClasses(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassToAnyCharMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateCharacterClass()
    {
        var classNode = new CharacterClassNode(new CharacterClassCharacterSetNode([
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        ]), false);
        var rootNode = new ConcatenationNode(classNode);
        var target = new CharacterClassToAnyCharMutator();

        var result = target.ApplyMutations(classNode, rootNode).ToList();

        result.Should().ContainSingle();
        var regexMutation = result[0];
        regexMutation.OriginalNode.Should().Be(classNode);
        regexMutation.ReplacementNode!.ToString().Should().Be(@"[\w\W]");
        regexMutation.ReplacementPattern.Should().Be(@"[\w\W]");
        regexMutation.DisplayName.Should().Be("""Regex character class to "[\w\W]" change""");
        regexMutation.Description.Should().Be("""Replaced regex node "[abc]" with "[\w\W]" at offset 0.""");
    }
}
