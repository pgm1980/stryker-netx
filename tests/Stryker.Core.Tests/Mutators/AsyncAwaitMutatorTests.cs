using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class AsyncAwaitMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<AsyncAwaitMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnAwait_EmitsGetAwaiterGetResult()
    {
        var node = ParseExpression<AwaitExpressionSyntax>("await x");
        var m = AssertSingleMutation(ApplyMutations<AsyncAwaitMutator, AwaitExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Contain("GetAwaiter");
    }
}
