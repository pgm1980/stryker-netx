using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class IsPatternExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<IsPatternExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnIsPattern_EmitsAtLeastOneMutation()
    {
        var node = ParseExpression<IsPatternExpressionSyntax>("x is 1");
        var mutations = ApplyMutations<IsPatternExpressionMutator, IsPatternExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }

    // Sprint 180 (issue #278, 360°-Analyse F-29): a pattern that binds a variable leaks
    // that variable into the enclosing statement scope. Negating the root pattern breaks
    // definite assignment for every later use of the variable, which the compiler rejects
    // with CS0165 — a guaranteed compile error on real-world code shapes. Such expressions
    // must therefore produce no mutations at all.
    [Theory]
    [InlineData("x is int n")]
    [InlineData("x is var v")]
    [InlineData("x is { } o")]
    [InlineData("x is string { Length: 1 } s")]
    [InlineData("x is [1, .. var rest]")]
    [InlineData("x is not int n")]
    public void ApplyMutations_OnPatternBindingAVariable_EmitsNoMutation(string source)
    {
        var node = ParseExpression<IsPatternExpressionSyntax>(source);
        var mutations = ApplyMutations<IsPatternExpressionMutator, IsPatternExpressionSyntax>(new(), node);
        mutations.Should().BeEmpty();
    }

    // A discard designation binds nothing, so negation stays compilable — it must
    // remain mutable to keep mutator coverage on these shapes.
    [Theory]
    [InlineData("x is int _")]
    [InlineData("x is { Length: 1 }")]
    [InlineData("x is null")]
    public void ApplyMutations_OnPatternWithoutVariableBinding_StillMutates(string source)
    {
        var node = ParseExpression<IsPatternExpressionSyntax>(source);
        var mutations = ApplyMutations<IsPatternExpressionMutator, IsPatternExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }
}
