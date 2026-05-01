using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class StringMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<StringMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnNonEmptyString_DoesNotCrash()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("\"hello\"");
        var mutations = ApplyMutations<StringMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().NotBeNull();
    }
}
