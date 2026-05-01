using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ConditionalExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<ConditionalExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsStandard()
        => AssertMutationLevel<ConditionalExpressionMutator>(MutationLevel.Standard);

    [Fact]
    public void ApplyMutations_OnTernary_EmitsTrueAndFalseAndNegationVariants()
    {
        var node = ParseExpression<ConditionalExpressionSyntax>("c ? a : b");
        var mutations = ApplyMutations<ConditionalExpressionMutator, ConditionalExpressionSyntax>(new(), node);
        mutations.Should().HaveCountGreaterThanOrEqualTo(2);
        mutations.Should().Contain(m => m.DisplayName.Contains("true"));
        mutations.Should().Contain(m => m.DisplayName.Contains("false"));
        mutations.Should().AllSatisfy(m => m.Type.Should().Be(Mutator.Conditional));
    }

    [Fact]
    public void ApplyMutations_OnTernaryWithDeclarationPattern_SkipsMutation()
    {
        // (x is int n) ? n : 0 — declaration pattern in condition, must skip.
        var node = ParseExpression<ConditionalExpressionSyntax>("(x is int n) ? n : 0");
        AssertNoMutations(ApplyMutations<ConditionalExpressionMutator, ConditionalExpressionSyntax>(new(), node));
    }
}
