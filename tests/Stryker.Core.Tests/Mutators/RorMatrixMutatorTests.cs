using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class RorMatrixMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<RorMatrixMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsComplete()
        => AssertMutationLevel<RorMatrixMutator>(MutationLevel.Complete);

    [Theory]
    [InlineData("a < b")]
    [InlineData("a <= b")]
    [InlineData("a > b")]
    [InlineData("a >= b")]
    [InlineData("a == b")]
    [InlineData("a != b")]
    public void ApplyMutations_OnRelationalOperator_EmitsExactly5Replacements(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 5);
        mutations.Should().AllSatisfy(m => m.Type.Should().Be(Mutator.Equality));
    }

    [Fact]
    public void ApplyMutations_OnLessThan_EmitsAllExpectedReplacements()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a < b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        var replacementOps = mutations.Select(m => m.ReplacementNode.ToString().Replace("a", "").Replace("b", "").Trim()).ToList();
        replacementOps.Should().BeEquivalentTo("<=", ">", ">=", "==", "!=");
    }

    [Fact]
    public void ApplyMutations_OnEquals_DoesNotEmitItselfAsReplacement()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a == b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 5);
        mutations.Should().NotContain(m => m.ReplacementNode.ToString().Contains("=="));
    }

    [Fact]
    public void ApplyMutations_DisplayName_FollowsExpectedFormat()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a < b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        mutations.Should().AllSatisfy(m => m.DisplayName.Should().StartWith("ROR matrix: '<' -> "));
    }

    [Theory]
    [InlineData("a + b")]
    [InlineData("a && b")]
    [InlineData("a || b")]
    [InlineData("a & b")]
    [InlineData("a | b")]
    [InlineData("a ^ b")]
    [InlineData("a * b")]
    public void ApplyMutations_OnNonRelationalOperator_ReturnsNoMutation(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertNoMutations(mutations);
    }
}
