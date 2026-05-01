using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class SpanReadOnlySpanDeclarationMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<SpanReadOnlySpanDeclarationMutator>(MutationProfile.All);

    [Fact]
    public void Type_IsSpanReadOnlySpanDeclarationMutator()
        => typeof(SpanReadOnlySpanDeclarationMutator).Should().NotBeNull();
}
