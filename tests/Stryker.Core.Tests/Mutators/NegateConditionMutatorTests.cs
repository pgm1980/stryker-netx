using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class NegateConditionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<NegateConditionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsStandard()
        => AssertMutationLevel<NegateConditionMutator>(MutationLevel.Standard);

    [Fact]
    public void ApplyMutations_OnIfCondition_NegatesCondition()
    {
        // We must build via a real parent so node.Parent is an IfStatementSyntax.
        var ifStmt = ParseStatement<IfStatementSyntax>("if (someFlag) { }");
        var condition = ifStmt.Condition;
        var mutations = ApplyMutations<NegateConditionMutator, ExpressionSyntax>(new(), condition);
        var m = AssertSingleMutation(mutations);
        m.ReplacementNode.ToString().Should().Be("!(someFlag)");
    }

    [Fact]
    public void ApplyMutations_OnWhileCondition_NegatesCondition()
    {
        var whileStmt = ParseStatement<WhileStatementSyntax>("while (cond) { }");
        var condition = whileStmt.Condition;
        var mutations = ApplyMutations<NegateConditionMutator, ExpressionSyntax>(new(), condition);
        AssertSingleMutation(mutations).ReplacementNode.ToString().Should().Contain("!");
    }

    [Fact]
    public void ApplyMutations_OnIsPatternExpression_SkipsMutation()
    {
        // is-pattern (with declaration) — must skip per the mutator's documented contract.
        var pattern = ParseExpression<IsPatternExpressionSyntax>("x is int n");
        var mutations = ApplyMutations<NegateConditionMutator, ExpressionSyntax>(new(), pattern);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnBareExpressionWithoutParent_ReturnsNoMutation()
    {
        // Expression without if/while parent — no mutation per design.
        var expr = ParseExpression<ExpressionSyntax>("foo()");
        var mutations = ApplyMutations<NegateConditionMutator, ExpressionSyntax>(new(), expr);
        AssertNoMutations(mutations);
    }
}
