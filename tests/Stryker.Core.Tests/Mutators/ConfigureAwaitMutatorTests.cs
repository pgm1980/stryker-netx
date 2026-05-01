using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ConfigureAwaitMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<ConfigureAwaitMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("x.ConfigureAwait(false)")]
    [InlineData("x.ConfigureAwait(true)")]
    public void ApplyMutations_OnConfigureAwait_EmitsBooleanSwap(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        var m = AssertSingleMutation(ApplyMutations<ConfigureAwaitMutator, InvocationExpressionSyntax>(new(), node));
        m.DisplayName.Should().Contain("ConfigureAwait");
    }

    [Fact]
    public void ApplyMutations_OnNonConfigureAwait_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("x.Foo(false)");
        AssertNoMutations(ApplyMutations<ConfigureAwaitMutator, InvocationExpressionSyntax>(new(), node));
    }
}
