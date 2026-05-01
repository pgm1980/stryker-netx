using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class DateTimeAddSignMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<DateTimeAddSignMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("dt.AddDays(1)")]
    [InlineData("dt.AddHours(2)")]
    [InlineData("dt.AddMinutes(3)")]
    [InlineData("dt.AddSeconds(4)")]
    [InlineData("dt.AddMonths(5)")]
    [InlineData("dt.AddYears(6)")]
    public void ApplyMutations_OnAddMethod_EmitsSignFlip(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        AssertSingleMutation(ApplyMutations<DateTimeAddSignMutator, InvocationExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnNegatedArg_DropsMinus()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("dt.AddDays(-1)");
        var m = AssertSingleMutation(ApplyMutations<DateTimeAddSignMutator, InvocationExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Contain("AddDays(1)");
    }

    [Fact]
    public void ApplyMutations_OnUnrelatedMethod_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("dt.Foo(1)");
        AssertNoMutations(ApplyMutations<DateTimeAddSignMutator, InvocationExpressionSyntax>(new(), node));
    }
}
