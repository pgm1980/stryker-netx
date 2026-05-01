using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class MathMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<MathMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("Math.Sin(x)")]
    [InlineData("Math.Cos(x)")]
    [InlineData("Math.Floor(x)")]
    [InlineData("Math.Ceiling(x)")]
    [InlineData("Math.Log(x)")]
    [InlineData("Math.Exp(x)")]
    public void ApplyMutations_OnMathMethod_EmitsMutation(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        var mutations = ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnNonMathMethod_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("Foo.Bar()");
        AssertNoMutations(ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node));
    }
}
