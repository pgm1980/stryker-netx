using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `[Ignore("Dotnet does not support this regex expression")]` → `[Fact(Skip = "...")]`.
/// </summary>
public sealed class UnicodeCharClassNegationMutatorTests
{
    [Fact]
    public void NegatesUnicodeCharacterClassWithLoneProperty()
    {
        var result = TestHelpers.ParseAndMutate(@"\p{IsBasicLatin}\P{IsBasicLatin}", new UnicodeCharClassNegationMutator());

        result.Select(static a => a.ReplacementPattern)
              .Should().BeEquivalentTo(@"\P{IsBasicLatin}\P{IsBasicLatin}", @"\p{IsBasicLatin}\p{IsBasicLatin}");
    }

    [Fact(Skip = "Dotnet does not support this regex expression (port preserves upstream [Ignore]).")]
    public void NegatesUnicodeCharacterClassWithPropertyAndValue()
    {
        var result = TestHelpers.ParseAndMutate(@"\p{Script_Extensions=Latin}\P{Script_Extensions=Latin}",
                                                new UnicodeCharClassNegationMutator());

        result.Select(static a => a.ReplacementPattern)
              .Should().BeEquivalentTo(
                   @"\P{Script_Extensions=Latin}\P{Script_Extensions=Latin}",
                   @"\p{Script_Extensions=Latin}\p{Script_Extensions=Latin}");
    }

    [Fact]
    public void ShouldNegateUnicodeCharacterClassAtStart()
    {
        var unicodeNode = new UnicodeCategoryNode("IsBasicLatin", false);
        var childNodes = new List<RegexNode>
        {
            unicodeNode, new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new UnicodeCharClassNegationMutator();

        var result = target.ApplyMutations(unicodeNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(unicodeNode);
        mutation.ReplacementNode!.ToString().Should().Be(@"\P{IsBasicLatin}");
        mutation.ReplacementPattern.Should().Be(@"\P{IsBasicLatin}abc");
        mutation.DisplayName.Should().Be("Regex Unicode character class negation mutation");
        mutation.Description.Should().Be("""Unicode category "\p{IsBasicLatin}" was replaced with "\P{IsBasicLatin}" at offset 0.""");
    }

    [Fact]
    public void ShouldNegateUnicodeCharacterClassInMiddle()
    {
        var unicodeNode = new UnicodeCategoryNode("IsBasicLatin", false);
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), unicodeNode, new CharacterNode('b'), new CharacterNode('c'),
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new UnicodeCharClassNegationMutator();

        var result = target.ApplyMutations(unicodeNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(unicodeNode);
        mutation.ReplacementNode!.ToString().Should().Be(@"\P{IsBasicLatin}");
        mutation.ReplacementPattern.Should().Be(@"a\P{IsBasicLatin}bc");
        mutation.DisplayName.Should().Be("Regex Unicode character class negation mutation");
        mutation.Description.Should().Be("""Unicode category "\p{IsBasicLatin}" was replaced with "\P{IsBasicLatin}" at offset 1.""");
    }

    [Fact]
    public void ShouldNegateUnicodeCharacterClassAtEnd()
    {
        var unicodeNode = new UnicodeCategoryNode("IsBasicLatin", false);
        var childNodes = new List<RegexNode>
        {
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'), unicodeNode,
        };
        var rootNode = new ConcatenationNode(childNodes);
        var target = new UnicodeCharClassNegationMutator();

        var result = target.ApplyMutations(unicodeNode, rootNode).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.OriginalNode.Should().Be(unicodeNode);
        mutation.ReplacementNode!.ToString().Should().Be(@"\P{IsBasicLatin}");
        mutation.ReplacementPattern.Should().Be(@"abc\P{IsBasicLatin}");
        mutation.DisplayName.Should().Be("Regex Unicode character class negation mutation");
        mutation.Description.Should().Be("""Unicode category "\p{IsBasicLatin}" was replaced with "\P{IsBasicLatin}" at offset 3.""");
    }

    [Fact]
    public void MutateShouldNotMutateNonUnicodeCharacterNode()
    {
        var characterNode = new CharacterNode('a');
        var rootNode = new ConcatenationNode(characterNode);
        var target = new UnicodeCharClassNegationMutator();

        var result = target.Mutate(characterNode, rootNode);

        result.Should().BeEmpty();
    }
}
