using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class NakedReceiverMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<NakedReceiverMutator>(MutationProfile.All);

    [Fact]
    public void MutationLevel_IsComplete()
        => AssertMutationLevel<NakedReceiverMutator>(MutationLevel.Complete);

    [Theory]
    [InlineData("a.Method(b)")]
    [InlineData("foo.Bar(x, y, z)")]
    [InlineData("obj.DoIt()")]
    public void ApplyMutations_OnMemberAccessInvocation_EmitsMutation(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), node);
        AssertSingleMutation(mutations);
    }

    [Fact]
    public void ApplyMutations_DisplayName_FormatsCorrectly()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("a.Method(b)");
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), node);
        var mutation = AssertSingleMutation(mutations);
        mutation.DisplayName.Should().Contain("Naked receiver");
    }

    [Fact]
    public void ApplyMutations_OnNonMemberAccessInvocation_ReturnsNoMutation()
    {
        // Just `Foo()` — no member-access receiver.
        var node = ParseExpression<InvocationExpressionSyntax>("Foo()");
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_InsideAwaitExpression_SkipsMutation()
    {
        // await x.Foo() — must skip because `await x` is rarely valid (x must be awaitable).
        var stmt = ParseStatement<ExpressionStatementSyntax>("await x.Foo();");
        var awaitExpr = stmt.Expression.Should().BeOfType<AwaitExpressionSyntax>().Subject;
        var invocation = awaitExpr.Expression.Should().BeOfType<InvocationExpressionSyntax>().Subject;
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), invocation);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_InsideThrowExpression_SkipsMutation()
    {
        // x ?? throw e.GetException() — naked receiver inside throw must skip.
        var throwSource = "x ?? throw e.GetException()";
        var throwExpr = ParseExpression<BinaryExpressionSyntax>(throwSource);
        var throwInner = throwExpr.Right.Should().BeOfType<ThrowExpressionSyntax>().Subject;
        var invocation = throwInner.Expression.Should().BeOfType<InvocationExpressionSyntax>().Subject;
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), invocation);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_InsideThrowStatement_SkipsMutation()
    {
        var stmt = ParseStatement<ThrowStatementSyntax>("throw factory.Make();");
        var invocation = stmt.Expression.Should().BeOfType<InvocationExpressionSyntax>().Subject;
        var mutations = ApplyMutations<NakedReceiverMutator, InvocationExpressionSyntax>(new(), invocation);
        AssertNoMutations(mutations);
    }
}
