using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class AsyncAwaitResultMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<AsyncAwaitResultMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnAwait_EmitsResultAccess()
    {
        var node = ParseExpression<AwaitExpressionSyntax>("await x");
        var m = AssertSingleMutation(ApplyMutations<AsyncAwaitResultMutator, AwaitExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Contain(".Result");
    }
}
