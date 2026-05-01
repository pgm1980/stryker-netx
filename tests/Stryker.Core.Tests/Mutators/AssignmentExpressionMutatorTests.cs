using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class AssignmentExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<AssignmentExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsStandard()
        => AssertMutationLevel<AssignmentExpressionMutator>(MutationLevel.Standard);

    [Theory]
    [InlineData("a += b")]
    [InlineData("a -= b")]
    [InlineData("a *= b")]
    [InlineData("a /= b")]
    [InlineData("a %= b")]
    public void ApplyMutations_OnArithmeticAssignment_EmitsAtLeastOneMutation(string source)
    {
        var node = ParseExpression<AssignmentExpressionSyntax>(source);
        var mutations = ApplyMutations<AssignmentExpressionMutator, AssignmentExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("a &= b", "bitwise")]
    [InlineData("a |= b", "bitwise")]
    [InlineData("a ^= b", "bitwise")]
    [InlineData("a <<= b", "shift")]
    [InlineData("a >>= b", "shift")]
    public void ApplyMutations_OnBitwiseOrShiftAssignment_EmitsTwoMutations(string source, string variant)
    {
        var node = ParseExpression<AssignmentExpressionSyntax>(source);
        var mutations = ApplyMutations<AssignmentExpressionMutator, AssignmentExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
        variant.Should().BeOneOf("bitwise", "shift");
    }

    [Fact]
    public void ApplyMutations_OnCoalesceAssignment_EmitsSimpleAssignment()
    {
        var node = ParseExpression<AssignmentExpressionSyntax>("a ??= b");
        var mutations = ApplyMutations<AssignmentExpressionMutator, AssignmentExpressionSyntax>(new(), node);
        AssertSingleMutation(mutations);
    }

    [Fact]
    public void ApplyMutations_OnSimpleAssignment_ReturnsNoMutation()
    {
        var node = ParseExpression<AssignmentExpressionSyntax>("a = b");
        AssertNoMutations(ApplyMutations<AssignmentExpressionMutator, AssignmentExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnStringAddAssignment_SkipsMutation()
    {
        var node = ParseExpression<AssignmentExpressionSyntax>("\"prefix\" += b");
        AssertNoMutations(ApplyMutations<AssignmentExpressionMutator, AssignmentExpressionSyntax>(new(), node));
    }
}
