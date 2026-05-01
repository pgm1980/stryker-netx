using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class SpanMemoryMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<SpanMemoryMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnSliceWithStartAndLength_EmitsZeroStartReplacement()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("span.Slice(2, 5)");
        var m = AssertSingleMutation(ApplyMutations<SpanMemoryMutator, InvocationExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Contain("Slice(0,");
    }

    [Fact]
    public void ApplyMutations_OnSliceWithSingleArg_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("span.Slice(2)");
        AssertNoMutations(ApplyMutations<SpanMemoryMutator, InvocationExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnUnrelatedMethod_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("span.Foo(2, 5)");
        AssertNoMutations(ApplyMutations<SpanMemoryMutator, InvocationExpressionSyntax>(new(), node));
    }
}
