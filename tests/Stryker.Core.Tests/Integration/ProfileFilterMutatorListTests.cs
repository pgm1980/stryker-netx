using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// L2 integration layer (Sprint 20): the <see cref="MutationProfile"/> ↔
/// active-mutator-list contract — combines reflection over every concrete
/// mutator type, the <see cref="MutatorProfileFilter"/> membership check, and
/// the <see cref="CsharpMutantOrchestrator"/>'s constructor-time filter
/// (Sprint 6 ADR-018). Catches drift between the declared
/// <c>[MutationProfileMembership]</c> attribute and what the orchestrator
/// actually loads.
/// </summary>
[Trait("Category", "Integration")]
public class ProfileFilterMutatorListTests : IntegrationTestBase
{
    private static readonly List<Type> AllConcreteMutatorTypes =
        [.. typeof(MutatorBase<>).Assembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Stryker.Core.Mutators", StringComparison.Ordinal)
                     && t is { IsClass: true, IsAbstract: false }
                     && t.Name.EndsWith("Mutator", StringComparison.Ordinal))];

    private static MutationProfile MembershipOf(Type t) =>
        t.GetCustomAttribute<MutationProfileMembershipAttribute>()?.Profiles
            ?? (MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData(MutationProfile.Defaults)]
    [InlineData(MutationProfile.Stronger)]
    [InlineData(MutationProfile.All)]
    public void ActiveSet_OnlyContainsMutatorsWhoseMembershipOverlapsActiveProfile(MutationProfile profile)
    {
        var active = GetActiveMutators(BuildOrchestrator(profile));
        active.Should().AllSatisfy(m =>
        {
            var membership = MembershipOf(m.GetType());
            (membership & profile).Should().NotBe(MutationProfile.None,
                $"{m.GetType().Name} is active under {profile} but its declared membership ({membership}) does not include {profile}");
        });
    }

    [Theory]
    [InlineData(MutationProfile.Defaults)]
    [InlineData(MutationProfile.Stronger)]
    [InlineData(MutationProfile.All)]
    public void ActiveSet_ContainsEveryMutatorWhoseMembershipIncludesProfile(MutationProfile profile)
    {
        // Build the expected set from the static catalog: every mutator type whose
        // attribute claims the profile, AND that the orchestrator's DefaultMutatorList
        // is willing to instantiate. We cannot guarantee every concrete mutator type
        // in the assembly is registered (e.g. helper types), so we intersect with the
        // All-profile active set, which is the maximum the orchestrator wires up.
        var fullActive = GetActiveMutators(BuildOrchestrator(MutationProfile.All))
            .Select(m => m.GetType()).ToHashSet();
        var expected = fullActive
            .Where(t => (MembershipOf(t) & profile) != MutationProfile.None)
            .ToHashSet();

        var actual = GetActiveMutators(BuildOrchestrator(profile))
            .Select(m => m.GetType()).ToHashSet();

        actual.SetEquals(expected).Should().BeTrue(
            $"profile {profile}: missing={string.Join(",", expected.Except(actual).Select(t => t.Name))}, extra={string.Join(",", actual.Except(expected).Select(t => t.Name))}");
    }

    [Fact]
    public void DefaultsActiveSet_IsSubsetOfStrongerActiveSet()
    {
        // Sprint 6/9 invariant: Stronger ⊇ Defaults. Stronger only adds, never removes.
        var defaults = GetActiveMutators(BuildOrchestrator(MutationProfile.Defaults))
            .Select(m => m.GetType()).ToHashSet();
        var stronger = GetActiveMutators(BuildOrchestrator(MutationProfile.Stronger))
            .Select(m => m.GetType()).ToHashSet();
        defaults.IsSubsetOf(stronger).Should().BeTrue(
            $"Stronger must include every Defaults mutator. Missing: {string.Join(",", defaults.Except(stronger).Select(t => t.Name))}");
    }

    [Fact]
    public void StrongerActiveSet_IsSubsetOfAllActiveSet()
    {
        var stronger = GetActiveMutators(BuildOrchestrator(MutationProfile.Stronger))
            .Select(m => m.GetType()).ToHashSet();
        var all = GetActiveMutators(BuildOrchestrator(MutationProfile.All))
            .Select(m => m.GetType()).ToHashSet();
        stronger.IsSubsetOf(all).Should().BeTrue(
            $"All must include every Stronger mutator. Missing: {string.Join(",", stronger.Except(all).Select(t => t.Name))}");
    }

    [Fact]
    public void AllActiveSet_IsLargerThanOrEqualToStronger()
    {
        var stronger = GetActiveMutators(BuildOrchestrator(MutationProfile.Stronger));
        var all = GetActiveMutators(BuildOrchestrator(MutationProfile.All));
        all.Count.Should().BeGreaterThanOrEqualTo(stronger.Count);
    }

    [Fact]
    public void AllProfile_HasAtLeastOneMutatorTaggedAllOnly()
    {
        // Sprint 9-12 produced All-only operators (e.g. ConservativeDefaultsEqualityFilter
        // companion behaviour, exotic returns). Verify that the catalogue still has
        // at least one mutator whose membership is exactly [All] — drift here means
        // the profile-discipline has been silently broken.
        var allOnly = AllConcreteMutatorTypes
            .Where(t => MembershipOf(t) == MutationProfile.All)
            .ToList();
        allOnly.Should().NotBeEmpty(
            "the catalogue must retain at least one All-only mutator (Sprint-9–12 invariants)");
    }

    [Fact]
    public void AllProfile_HasAtLeastOneStrongerOnlyMutator()
    {
        // Symmetric: at least one Stronger-only mutator exists (i.e. tagged
        // [Stronger | All] but NOT [Defaults]).
        var strongerNotDefaults = AllConcreteMutatorTypes
            .Where(t =>
            {
                var m = MembershipOf(t);
                return m.HasFlag(MutationProfile.Stronger)
                    && !m.HasFlag(MutationProfile.Defaults);
            })
            .ToList();
        strongerNotDefaults.Should().NotBeEmpty(
            "at least one mutator must be Stronger-or-higher but not in Defaults");
    }

    [Fact]
    public void EveryActiveMutator_HasNonNoneMembership()
    {
        // Defensive: an active mutator whose attribute resolves to None means the
        // filter let it through accidentally — that would be a contract bug.
        var active = GetActiveMutators(BuildOrchestrator(MutationProfile.All));
        active.Should().AllSatisfy(m =>
            MembershipOf(m.GetType()).Should().NotBe(MutationProfile.None,
                $"{m.GetType().Name} has membership=None but is active in the All profile"));
    }

    [Fact]
    public void DefaultsProfile_HasFewerMutatorsThanAll()
    {
        var defaults = GetActiveMutators(BuildOrchestrator(MutationProfile.Defaults));
        var all = GetActiveMutators(BuildOrchestrator(MutationProfile.All));
        defaults.Count.Should().BeLessThan(all.Count,
            "Defaults must be a strict subset of All (else profile-discipline is moot)");
    }

    [Fact]
    public void MutatorProfileFilter_RejectsMutatorWhoseMembershipDoesNotOverlap()
    {
        // Direct contract check on MutatorProfileFilter via a synthetic mutator.
        // We pick TypeDrivenReturnMutator because it's [Stronger | All] (Sprint 9),
        // so it must NOT be in Defaults.
        var typeDriven = new TypeDrivenReturnMutator();
        MutatorProfileFilter.IsInProfile(typeDriven, MutationProfile.Defaults).Should().BeFalse(
            "TypeDrivenReturnMutator is Stronger|All only; Defaults must reject it");
        MutatorProfileFilter.IsInProfile(typeDriven, MutationProfile.Stronger).Should().BeTrue();
        MutatorProfileFilter.IsInProfile(typeDriven, MutationProfile.All).Should().BeTrue();
    }
}
