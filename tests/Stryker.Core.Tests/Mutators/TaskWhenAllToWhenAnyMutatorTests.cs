using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class TaskWhenAllToWhenAnyMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<TaskWhenAllToWhenAnyMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("Task.WhenAll(t1, t2)")]
    [InlineData("Task.WhenAny(t1, t2)")]
    public void ApplyMutations_OnTaskWhen_EmitsSwap(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        AssertSingleMutation(ApplyMutations<TaskWhenAllToWhenAnyMutator, InvocationExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnUnrelated_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("Task.Run(() => {})");
        AssertNoMutations(ApplyMutations<TaskWhenAllToWhenAnyMutator, InvocationExpressionSyntax>(new(), node));
    }
}
