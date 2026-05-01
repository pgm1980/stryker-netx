using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class NullCoalescingExpressionMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<NullCoalescingExpressionMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnNullCoalescing_EmitsMutations()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a ?? b");
        var mutations = ApplyMutations<NullCoalescingExpressionMutator, BinaryExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }
}
