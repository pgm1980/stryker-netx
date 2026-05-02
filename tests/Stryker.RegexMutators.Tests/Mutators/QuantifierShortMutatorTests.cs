using System.Linq;
using FluentAssertions;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;
using Stryker.Regex.Parser.Nodes.QuantifierNodes;
using Stryker.RegexMutators;
using Stryker.RegexMutators.Mutators;
using Xunit;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 43 (v2.30.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public sealed class QuantifierShortMutatorTests
{
    [Theory]
    [InlineData("abc?", new[] { "abc{1,1}", "abc{0,0}", "abc{0,2}" })]
    [InlineData("abc*", new[] { "abc{1,}" })]
    [InlineData("abc+", new[] { "abc{0,}", "abc{2,}" })]
    [InlineData("a?b*c+", new[]
    {
        "a{1,1}b*c+", "a{0,0}b*c+", "a{0,2}b*c+", "a?b{1,}c+", "a?b*c{0,}", "a?b*c{2,}",
    })]
    public void QuantifierShort(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new QuantifierShortMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("c+", new[] { "c{0,}", "c{2,}" })]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Distinct upstream test cases preserved as separate methods for parity.")]
    public void QuantifierShort2(string input, string[] expected)
    {
        var result = TestHelpers.ParseAndMutate(input, new QuantifierShortMutator());

        result.Select(static a => a.ReplacementPattern).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldMutatePlusQuantifier()
    {
        var classNode = new CharacterClassNode(new CharacterClassCharacterSetNode([
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        ]), false);
        var quantity = new QuantifierPlusNode(classNode);
        var rootNode = new ConcatenationNode(quantity);
        var target = new QuantifierShortMutator();

        var result = target.ApplyMutations(quantity, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(2);
        regexMutations[0].OriginalNode.Should().Be(quantity);
        regexMutations[0].ReplacementNode!.ToString().Should().Be("[abc]{0,}");
        regexMutations[0].ReplacementPattern.Should().Be("[abc]{0,}");
        regexMutations[0].DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        regexMutations[0].Description.Should().Be("""Quantifier "[abc]+" was replaced with "[abc]{0,}" at offset 5.""");

        regexMutations[1].OriginalNode.Should().Be(quantity);
        regexMutations[1].ReplacementNode!.ToString().Should().Be("[abc]{2,}");
        regexMutations[1].ReplacementPattern.Should().Be("[abc]{2,}");
        regexMutations[1].DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        regexMutations[1].Description.Should().Be("""Quantifier "[abc]+" was replaced with "[abc]{2,}" at offset 5.""");
    }

    [Fact]
    public void ShouldMutateQuestionMarkQuantifier()
    {
        var classNode = new CharacterClassNode(new CharacterClassCharacterSetNode([
            new CharacterNode('a'), new CharacterNode('b'), new CharacterNode('c'),
        ]), false);
        var quantity = new QuantifierQuestionMarkNode(classNode);
        var rootNode = new ConcatenationNode(quantity);
        var target = new QuantifierShortMutator();

        var result = target.ApplyMutations(quantity, rootNode);

        var regexMutations = result.ToArray();
        regexMutations.Length.Should().Be(3);
        regexMutations[0].OriginalNode.Should().Be(quantity);
        regexMutations[0].ReplacementNode!.ToString().Should().Be("[abc]{1,1}");
        regexMutations[0].ReplacementPattern.Should().Be("[abc]{1,1}");
        regexMutations[0].DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        regexMutations[0].Description.Should().Be("""Quantifier "[abc]?" was replaced with "[abc]{1,1}" at offset 5.""");

        regexMutations[1].OriginalNode.Should().Be(quantity);
        regexMutations[1].ReplacementNode!.ToString().Should().Be("[abc]{0,0}");
        regexMutations[1].ReplacementPattern.Should().Be("[abc]{0,0}");
        regexMutations[1].DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        regexMutations[1].Description.Should().Be("""Quantifier "[abc]?" was replaced with "[abc]{0,0}" at offset 5.""");

        regexMutations[2].OriginalNode.Should().Be(quantity);
        regexMutations[2].ReplacementNode!.ToString().Should().Be("[abc]{0,2}");
        regexMutations[2].ReplacementPattern.Should().Be("[abc]{0,2}");
        regexMutations[2].DisplayName.Should().Be("Regex greedy quantifier quantity mutation");
        regexMutations[2].Description.Should().Be("""Quantifier "[abc]?" was replaced with "[abc]{0,2}" at offset 5.""");
    }
}
