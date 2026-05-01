using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class MemberVariableMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<MemberVariableMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnInstanceFieldAssignment_EmitsDefaultReset()
    {
        var (model, assign) = BuildSemanticContext<AssignmentExpressionSyntax>(
            "class C { int _f; void M() { _f = 42; } }");
        var mutations = ApplyTypeAwareMutations<MemberVariableMutator, AssignmentExpressionSyntax>(
            new(), assign, model);
        var m = AssertSingleMutation(mutations);
        m.ReplacementNode.ToString().Should().Contain("default");
    }

    [Fact]
    public void ApplyMutations_OnLocalAssignment_ReturnsNoMutation()
    {
        var (model, assign) = BuildSemanticContext<AssignmentExpressionSyntax>(
            "class C { void M() { int x = 0; x = 42; } }");
        var mutations = ApplyTypeAwareMutations<MemberVariableMutator, AssignmentExpressionSyntax>(
            new(), assign, model);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnAlreadyDefaultRhs_ReturnsNoMutation()
    {
        var (model, assign) = BuildSemanticContext<AssignmentExpressionSyntax>(
            "class C { int _f; void M() { _f = default; } }");
        var mutations = ApplyTypeAwareMutations<MemberVariableMutator, AssignmentExpressionSyntax>(
            new(), assign, model);
        AssertNoMutations(mutations);
    }
}
