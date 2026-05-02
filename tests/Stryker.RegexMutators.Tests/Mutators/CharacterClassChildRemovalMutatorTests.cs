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
public sealed class CharacterClassChildRemovalMutatorTests
{
    [Theory]
    [InlineData("[A-Z]", new string[0])]
    [InlineData("[ab0-9A-Zcd]", new[]
    {
        "[b0-9A-Zcd]", "[a0-9A-Zcd]", "[abA-Zcd]", "[ab0-9cd]", "[ab0-9A-Zd]", "[ab0-9A-Zc]",
    })]
    [InlineData("[A-Z-[CD]]", new[] { "[A-Z-[D]]", "[A-Z-[C]]", "[A-Z]", "[CD]" })]
    [InlineData("[a-zA-Z-[CD]]", new[] { "[a-zA-Z-[D]]", "[a-zA-Z-[C]]", "[a-zA-Z]", "[A-Z-[CD]]", "[a-z-[CD]]" })]
    [InlineData("[a-zA-Z-[CDE-[D-F]]]", new[]
    {
        "[a-zA-Z-[CDE]]", "[a-zA-Z-[DE-[D-F]]]", "[a-zA-Z-[CE-[D-F]]]", "[a-zA-Z-[CD-[D-F]]]", "[a-zA-Z]",
        "[A-Z-[CDE-[D-F]]]", "[a-z-[CDE-[D-F]]]",
    })]
    public void CharacterClassRemoveNode(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new CharacterClassChildRemovalMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("[A-Z]")]
    [InlineData("[A]")]
    [InlineData(@"[ሴ]")]
    public void DoesNotRemoveSingleItemClasses(string pattern)
    {
        var result = TestHelpers.ParseAndMutate(pattern, new CharacterClassChildRemovalMutator());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRemoveEachChildOfTheCharacterClass()
    {
        var a = new CharacterNode('a');
        var b = new CharacterNode('b');
        var c = new CharacterNode('c');
        var characterClassNode = new CharacterClassNode(new CharacterClassCharacterSetNode([a, b, c]), false);

        var childNodes = new List<RegexNode> { characterClassNode, new CharacterNode('a') };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassChildRemovalMutator();

        var result = target.ApplyMutations(characterClassNode, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(3);
        regexMutations[0].OriginalNode.Should().Be(a);
        regexMutations[0].ReplacementNode.Should().BeNull();
        regexMutations[0].ReplacementPattern.Should().Be("[bc]a");
        regexMutations[0].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[0].Description.Should().Be("""Removed child "a" from character class "[abc]" at offset 0.""");

        regexMutations[1].OriginalNode.Should().Be(b);
        regexMutations[1].ReplacementNode.Should().BeNull();
        regexMutations[1].ReplacementPattern.Should().Be("[ac]a");
        regexMutations[1].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[1].Description.Should().Be("""Removed child "b" from character class "[abc]" at offset 0.""");

        regexMutations[2].OriginalNode.Should().Be(c);
        regexMutations[2].ReplacementNode.Should().BeNull();
        regexMutations[2].ReplacementPattern.Should().Be("[ab]a");
        regexMutations[2].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[2].Description.Should().Be("""Removed child "c" from character class "[abc]" at offset 0.""");
    }

    [Fact]
    public void ShouldRemoveEachChildOfTheCharacterClassAndTheSubstitution()
    {
        var a = new CharacterNode('a');
        var b = new CharacterNode('b');
        var c = new CharacterNode('c');
        var sub = new CharacterClassNode(new CharacterClassCharacterSetNode([new CharacterNode('b')]), false);
        var characterClassNode = new CharacterClassNode(new CharacterClassCharacterSetNode([a, b, c]), sub, false);

        var childNodes = new List<RegexNode> { characterClassNode, new CharacterNode('a') };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassChildRemovalMutator();

        var result = target.ApplyMutations(characterClassNode, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(4);
        regexMutations[0].OriginalNode.Should().Be(sub);
        regexMutations[0].ReplacementNode.Should().BeNull();
        regexMutations[0].ReplacementPattern.Should().Be("[abc]a");
        regexMutations[0].DisplayName.Should().Be("Regex character class subtraction removal");
        regexMutations[0].Description.Should().Be("""Character Class Subtraction "-[b]" was removed at offset 4.""");

        regexMutations[1].OriginalNode.Should().Be(a);
        regexMutations[1].ReplacementNode.Should().BeNull();
        regexMutations[1].ReplacementPattern.Should().Be("[bc-[b]]a");
        regexMutations[1].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[1].Description.Should().Be("""Removed child "a" from character class "[abc-[b]]" at offset 0.""");

        regexMutations[2].OriginalNode.Should().Be(b);
        regexMutations[2].ReplacementNode.Should().BeNull();
        regexMutations[2].ReplacementPattern.Should().Be("[ac-[b]]a");
        regexMutations[2].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[2].Description.Should().Be("""Removed child "b" from character class "[abc-[b]]" at offset 0.""");

        regexMutations[3].OriginalNode.Should().Be(c);
        regexMutations[3].ReplacementNode.Should().BeNull();
        regexMutations[3].ReplacementPattern.Should().Be("[ab-[b]]a");
        regexMutations[3].DisplayName.Should().Be("Regex character class child removal");
        regexMutations[3].Description.Should().Be("""Removed child "c" from character class "[abc-[b]]" at offset 0.""");
    }

    [Fact]
    public void ShouldRemoveOnlyChildOfTheCharacterClassAndTheSubstitution()
    {
        var sub = new CharacterClassNode(new CharacterClassCharacterSetNode([new CharacterNode('b')]), false);
        var characterClassNode = new CharacterClassNode(new CharacterClassCharacterSetNode([new CharacterNode('a')]), sub, false);

        var childNodes = new List<RegexNode> { characterClassNode, new CharacterNode('a') };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new CharacterClassChildRemovalMutator();

        var result = target.ApplyMutations(characterClassNode, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(2);
        regexMutations[0].OriginalNode.Should().Be(sub);
        regexMutations[0].ReplacementNode.Should().BeNull();
        regexMutations[0].ReplacementPattern.Should().Be("[a]a");
        regexMutations[0].DisplayName.Should().Be("Regex character class subtraction removal");
        regexMutations[0].Description.Should().Be("""Character Class Subtraction "-[b]" was removed at offset 2.""");

        regexMutations[1].OriginalNode.Should().Be(characterClassNode);
        regexMutations[1].ReplacementNode!.ToString().Should().Be(sub.ToString());
        regexMutations[1].ReplacementPattern.Should().Be("[b]a");
        regexMutations[1].DisplayName.Should().Be("Regex character class subtraction replacement");
        regexMutations[1].Description.Should().Be("""Character Class "[a-[b]]" was replace with its subtraction "[b]" at offset 0.""");
    }

    [Fact]
    public void ShouldNotMutateNonCharacterClassNode()
    {
        var characterNode = new CharacterNode('a');
        var rootNode = new ConcatenationNode(characterNode);
        var target = new CharacterClassChildRemovalMutator();

        var result = target.Mutate(characterNode, rootNode);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateCharacterClassWithSingleChild()
    {
        var characterClassNode = new CharacterClassNode(new CharacterClassCharacterSetNode([new CharacterNode('a')]), false);
        var rootNode = new ConcatenationNode(characterClassNode);
        var target = new CharacterClassChildRemovalMutator();

        var result = target.Mutate(characterClassNode, rootNode);

        result.Should().BeEmpty();
    }
}
