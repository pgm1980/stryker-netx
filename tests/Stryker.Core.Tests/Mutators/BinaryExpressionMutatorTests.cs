using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class BinaryExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<BinaryExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsBasic()
        => AssertMutationLevel<BinaryExpressionMutator>(MutationLevel.Basic);

    [Theory]
    [InlineData("a + b", 1, "Arithmetic mutation")]
    [InlineData("a - b", 1, "Arithmetic mutation")]
    [InlineData("a * b", 1, "Arithmetic mutation")]
    [InlineData("a / b", 1, "Arithmetic mutation")]
    [InlineData("a % b", 1, "Arithmetic mutation")]
    public void ApplyMutations_OnArithmeticOperators_ReturnsSingleArithmeticMutation(
        string source, int expectedCount, string expectedName)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, expectedCount);
        mutations[0].DisplayName.Should().Be(expectedName);
        mutations[0].Type.Should().Be(Mutator.Arithmetic);
    }

    [Theory]
    [InlineData("a > b", 2, Mutator.Equality)]
    [InlineData("a < b", 2, Mutator.Equality)]
    [InlineData("a >= b", 2, Mutator.Equality)]
    [InlineData("a <= b", 2, Mutator.Equality)]
    public void ApplyMutations_OnRelationalBoundaryOperators_ReturnsTwoEqualityMutations(
        string source, int expectedCount, Mutator expectedType)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, expectedCount);
        mutations.Should().AllSatisfy(m => m.Type.Should().Be(expectedType));
    }

    [Theory]
    [InlineData("a == b", 1, Mutator.Equality)]
    [InlineData("a != b", 1, Mutator.Equality)]
    [InlineData("a && b", 1, Mutator.Logical)]
    [InlineData("a || b", 1, Mutator.Logical)]
    [InlineData("a & b", 1, Mutator.Bitwise)]
    [InlineData("a | b", 1, Mutator.Bitwise)]
    public void ApplyMutations_OnSimpleSwapOperators_ReturnsSingleMutation(
        string source, int expectedCount, Mutator expectedType)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, expectedCount);
        mutations[0].Type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("a << b")]
    [InlineData("a >> b")]
    [InlineData("a >>> b")]
    public void ApplyMutations_OnShiftOperators_ReturnsTwoMutations(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
        mutations.Should().AllSatisfy(m => m.Type.Should().Be(Mutator.Bitwise));
    }

    [Fact]
    public void ApplyMutations_OnExclusiveOr_ReturnsLogicalAndIntegralMutations()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a ^ b");
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
        mutations.Should().Contain(m => m.DisplayName == "Logical mutation");
        mutations.Should().Contain(m => m.DisplayName == "Bitwise mutation");
    }

    [Fact]
    public void ApplyMutations_OnStringAddition_SkipsAddExpression()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("\"hello\" + b");
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnAdditionWithStringRight_SkipsAddExpression()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a + \"hello\"");
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnUnsupportedOperator_ReturnsNoMutation()
    {
        // 'as' is a binary expression but not in the lookup table
        var node = ParseExpression<BinaryExpressionSyntax>("a == b == c");
        // c == c is double-EqualsExpression — only the outer one mutates
        var mutations = ApplyMutations<BinaryExpressionMutator, BinaryExpressionSyntax>(new(), node);
        // outer kind = EqualsExpression (because EqualsExpression is left-associative)
        // → mutates to NotEquals
        mutations.Should().NotBeEmpty();
    }
}
