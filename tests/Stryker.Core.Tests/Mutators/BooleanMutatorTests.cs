using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class BooleanMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<BooleanMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsStandard()
        => AssertMutationLevel<BooleanMutator>(MutationLevel.Standard);

    [Fact]
    public void ApplyMutations_OnTrue_EmitsFalse()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("true");
        var m = AssertSingleMutation(ApplyMutations<BooleanMutator, LiteralExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Be("false");
        m.Type.Should().Be(Mutator.Boolean);
        m.DisplayName.Should().Be("Boolean mutation");
    }

    [Fact]
    public void ApplyMutations_OnFalse_EmitsTrue()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("false");
        var m = AssertSingleMutation(ApplyMutations<BooleanMutator, LiteralExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Be("true");
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"hello\"")]
    [InlineData("null")]
    public void ApplyMutations_OnNonBooleanLiteral_ReturnsNoMutation(string source)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        AssertNoMutations(ApplyMutations<BooleanMutator, LiteralExpressionSyntax>(new(), node));
    }
}
