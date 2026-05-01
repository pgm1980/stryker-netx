using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ArgumentPropagationMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<ArgumentPropagationMutator>(MutationProfile.All);

    [Fact]
    public void Type_IsArgumentPropagationMutator()
        => typeof(ArgumentPropagationMutator).Should().NotBeNull();
}
