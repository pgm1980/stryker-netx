using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using FsCheck.Xunit;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Properties;

/// <summary>
/// v2.6.0 (Sprint 19, Item C / ToT property P2): reflection invariants
/// over every concrete <c>Mutator</c> subclass in
/// <c>Stryker.Core.Mutators</c>. Catches regressions when a new mutator
/// is added without the required <see cref="MutationProfileMembershipAttribute"/>.
/// </summary>
public class MutatorReflectionProperties
{
    private static readonly List<Type> AllConcreteMutators =
        [.. typeof(MutatorBase<>).Assembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Stryker.Core.Mutators", StringComparison.Ordinal)
                     && t is { IsClass: true, IsAbstract: false }
                     && t.Name.EndsWith("Mutator", StringComparison.Ordinal))];

    [Fact]
    public void AllConcreteMutators_HaveProfileMembershipAttribute()
    {
        AllConcreteMutators.Should().NotBeEmpty();
        AllConcreteMutators.Should().AllSatisfy(t =>
            t.GetCustomAttribute<MutationProfileMembershipAttribute>().Should().NotBeNull(
                $"{t.Name} must carry [MutationProfileMembership(...)] per CLAUDE.md profile-discipline"));
    }

    /// <summary>
    /// Sprint 142 (ADR-026): mutators that are intentionally disabled across all
    /// profiles via Profile.None because their target slot is incompatible with
    /// the current ConditionalInstrumentationEngine. Re-enabling tracked in ADR-026.
    /// </summary>
    private static readonly HashSet<string> IntentionallyDisabledMutators = new(StringComparer.Ordinal)
    {
        "SpanReadOnlySpanDeclarationMutator", // Sprint 142, ADR-026: TypeSyntax-slot conditional-instrumentation crash
    };

    [Fact]
    public void AllConcreteMutators_HaveNonNoneProfile_ExceptIntentionallyDisabled()
    {
        AllConcreteMutators.Should().AllSatisfy(t =>
        {
            var attr = t.GetCustomAttribute<MutationProfileMembershipAttribute>();
            attr.Should().NotBeNull();
            if (IntentionallyDisabledMutators.Contains(t.Name))
            {
                attr!.Profiles.Should().Be(MutationProfile.None,
                    $"{t.Name} is on the intentionally-disabled list (ADR-026); its profile must be None");
            }
            else
            {
                attr!.Profiles.Should().NotBe(MutationProfile.None,
                    $"{t.Name}'s profile membership must include at least one profile flag (or add to IntentionallyDisabledMutators with an ADR reference)");
            }
        });
    }

    [Fact]
    public void AllConcreteMutators_ImplementIMutator()
    {
        AllConcreteMutators.Should().AllSatisfy(t =>
            typeof(IMutator).IsAssignableFrom(t).Should().BeTrue(
                $"{t.Name} must implement IMutator"));
    }

    [Property(MaxTest = 50)]
    public bool RandomMutatorIndex_AlwaysReturnsValidProfileOrIntentionallyDisabled(int rawIndex)
    {
        if (AllConcreteMutators.Count == 0) return true;
        var idx = Math.Abs(rawIndex) % AllConcreteMutators.Count;
        var t = AllConcreteMutators[idx];
        var attr = t.GetCustomAttribute<MutationProfileMembershipAttribute>();
        if (attr is null) return false;
        return attr.Profiles != MutationProfile.None || IntentionallyDisabledMutators.Contains(t.Name);
    }
}
