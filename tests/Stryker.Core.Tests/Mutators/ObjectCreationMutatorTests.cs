using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ObjectCreationMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<ObjectCreationMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnObjectCreation_DoesNotCrash()
    {
        var node = ParseExpression<ObjectCreationExpressionSyntax>("new Foo(1, 2)");
        var mutations = ApplyMutations<ObjectCreationMutator, ObjectCreationExpressionSyntax>(new(), node);
        mutations.Should().NotBeNull();
    }
}
