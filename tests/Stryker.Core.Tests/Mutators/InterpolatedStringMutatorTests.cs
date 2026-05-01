using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class InterpolatedStringMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<InterpolatedStringMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnInterpolatedString_DoesNotCrash()
    {
        var node = ParseExpression<InterpolatedStringExpressionSyntax>("$\"hello {x}\"");
        var mutations = ApplyMutations<InterpolatedStringMutator, InterpolatedStringExpressionSyntax>(new(), node);
        mutations.Should().NotBeNull();
    }
}
