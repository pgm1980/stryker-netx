using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class GenericConstraintLoosenMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<GenericConstraintLoosenMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnClassConstraint_EmitsLoosenedAlternatives()
    {
        var clause = ParseMember<TypeParameterConstraintClauseSyntax>("void Foo<T>() where T : class { }");
        var mutations = ApplyMutations<GenericConstraintLoosenMutator, TypeParameterConstraintClauseSyntax>(new(), clause);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnInterfaceConstraint_EmitsClassReplacement()
    {
        var clause = ParseMember<TypeParameterConstraintClauseSyntax>("void Foo<T>() where T : System.IDisposable { }");
        var mutations = ApplyMutations<GenericConstraintLoosenMutator, TypeParameterConstraintClauseSyntax>(new(), clause);
        mutations.Should().NotBeEmpty();
    }
}
