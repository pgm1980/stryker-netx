using System.Linq;
using FluentAssertions;
using Stryker.RegexMutators;
using Xunit;

namespace Stryker.RegexMutators.Tests;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/RegexMutantOrchestratorTest.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class RegexMutantOrchestratorTest
{
    [Fact]
    public void ShouldRemoveAnchor()
    {
        var target = new RegexMutantOrchestrator("^abc");

        var result = target.Mutate();

        result.Should().ContainSingle();
        var mutation = result.Single();
        mutation.OriginalNode.ToString().Should().Be("^");
        mutation.ReplacementNode.Should().BeNull();
        mutation.ReplacementPattern.Should().Be("abc");
        mutation.DisplayName.Should().Be("Regex anchor removal mutation");
        mutation.Description.Should().Be("Anchor \"^\" was removed at offset 0.");
    }

    [Fact]
    public void ShouldRemoveQuantifier()
    {
        var target = new RegexMutantOrchestrator("abcX?");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(5);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("X?");
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"?\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldRemoveLazyQuantifier()
    {
        var target = new RegexMutantOrchestrator("abcX??");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(4);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("X??");
        mutation.ReplacementNode!.ToString().Should().Be("X");
        mutation.ReplacementPattern.Should().Be("abcX");
        mutation.DisplayName.Should().Be("Regex quantifier removal mutation");
        mutation.Description.Should().Be("Quantifier \"??\" was removed at offset 4.");
    }

    [Fact]
    public void ShouldNegateUnnegatedCharacterClass()
    {
        var target = new RegexMutantOrchestrator("abc[XY]");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(4);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("[XY]");
        mutation.ReplacementNode!.ToString().Should().Be("[^XY]");
        mutation.ReplacementPattern.Should().Be("abc[^XY]");
        mutation.DisplayName.Should().Be("Regex character class negation mutation");
        mutation.Description.Should().Be("Character class \"[XY]\" was replaced with \"[^XY]\" at offset 3.");
    }

    [Fact]
    public void ShouldUnnegateNegatedCharacterClass()
    {
        var target = new RegexMutantOrchestrator("abc[^XY]");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(4);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("[^XY]");
        mutation.ReplacementNode!.ToString().Should().Be("[XY]");
        mutation.ReplacementPattern.Should().Be("abc[XY]");
        mutation.DisplayName.Should().Be("Regex character class negation mutation");
        mutation.Description.Should().Be("Character class \"[^XY]\" was replaced with \"[XY]\" at offset 3.");
    }

    [Fact]
    public void ShouldNegateUnnegatedCharacterClassShorthand()
    {
        var target = new RegexMutantOrchestrator(@"abc\d");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(3);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("\\d");
        mutation.ReplacementNode!.ToString().Should().Be("\\D");
        mutation.ReplacementPattern.Should().Be("abc\\D");
        mutation.DisplayName.Should().Be("Regex character class shorthand negation mutation");
        mutation.Description.Should().Be("Character class shorthand \"\\d\" was replaced with \"\\D\" at offset 3.");
    }

    [Fact]
    public void ShouldUnnegateNegatedCharacterClassShorthand()
    {
        var target = new RegexMutantOrchestrator(@"abc\D");

        var result = target.Mutate().ToList();

        result.Count.Should().Be(3);
        var mutation = result[0];
        mutation.OriginalNode.ToString().Should().Be("\\D");
        mutation.ReplacementNode!.ToString().Should().Be("\\d");
        mutation.ReplacementPattern.Should().Be("abc\\d");
        mutation.DisplayName.Should().Be("Regex character class shorthand negation mutation");
        mutation.Description.Should().Be("Character class shorthand \"\\D\" was replaced with \"\\d\" at offset 3.");
    }

    [Fact]
    public void ShouldApplyMultipleMutations()
    {
        var target = new RegexMutantOrchestrator(@"^[abc]\d?");

        var result = target.Mutate();

        result.Count().Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void ShouldApplyMultipleMutations2()
    {
        var target = new RegexMutantOrchestrator("^abc(d+|[xyz])$");

        var result = target.Mutate();

        result.Count().Should().BeGreaterThanOrEqualTo(12);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[Z-A]")]
    [InlineData("\\")]
    [InlineData("(abc")]
    [InlineData(@"\p{UnicodeCategory}")]
    public void InvalidRegexShouldNotThrow(string? pattern)
    {
        var target = new RegexMutantOrchestrator(pattern!);

        var result = target.Mutate();

        result.Should().BeEmpty();
    }
}
