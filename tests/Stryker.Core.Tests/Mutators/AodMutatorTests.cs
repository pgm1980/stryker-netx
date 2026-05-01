using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class AodMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<AodMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("a + b")]
    [InlineData("a - b")]
    [InlineData("a * b")]
    [InlineData("a / b")]
    [InlineData("a % b")]
    public void ApplyMutations_OnArithmetic_EmitsTwoMutations(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<AodMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
    }

    [Fact]
    public void ApplyMutations_OnNonArithmetic_ReturnsNoMutation()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a == b");
        AssertNoMutations(ApplyMutations<AodMutator, BinaryExpressionSyntax>(new(), node));
    }
}
