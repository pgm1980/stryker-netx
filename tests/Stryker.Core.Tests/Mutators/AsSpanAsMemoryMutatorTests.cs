using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class AsSpanAsMemoryMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<AsSpanAsMemoryMutator>(MutationProfile.All);

    [Theory]
    [InlineData("arr.AsSpan()")]
    [InlineData("arr.AsMemory()")]
    [InlineData("arr.AsReadOnlySpan()")]
    [InlineData("arr.AsReadOnlyMemory()")]
    public void ApplyMutations_OnViewMethod_EmitsSwap(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        AssertSingleMutation(ApplyMutations<AsSpanAsMemoryMutator, InvocationExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnUnrelatedMethod_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("arr.Foo()");
        AssertNoMutations(ApplyMutations<AsSpanAsMemoryMutator, InvocationExpressionSyntax>(new(), node));
    }
}
