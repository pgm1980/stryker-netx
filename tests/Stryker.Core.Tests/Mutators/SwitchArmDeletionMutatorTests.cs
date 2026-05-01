using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class SwitchArmDeletionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<SwitchArmDeletionMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnSwitchExpressionWithDiscard_EmitsArmDeletions()
    {
        var node = ParseExpression<SwitchExpressionSyntax>("x switch { 1 => \"a\", 2 => \"b\", _ => \"default\" }");
        var mutations = ApplyMutations<SwitchArmDeletionMutator, SwitchExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
    }

    [Fact]
    public void ApplyMutations_OnSwitchExpressionWithoutDiscard_ReturnsNoMutation()
    {
        var node = ParseExpression<SwitchExpressionSyntax>("x switch { 1 => \"a\", 2 => \"b\" }");
        AssertNoMutations(ApplyMutations<SwitchArmDeletionMutator, SwitchExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnSingleArm_ReturnsNoMutation()
    {
        var node = ParseExpression<SwitchExpressionSyntax>("x switch { _ => \"default\" }");
        AssertNoMutations(ApplyMutations<SwitchArmDeletionMutator, SwitchExpressionSyntax>(new(), node));
    }
}
