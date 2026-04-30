using System;

namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-018): declares which <see cref="MutationProfile"/>(s) a mutator
/// or mutation-operator participates in. Applied to classes implementing
/// <see cref="IMutator"/> or <see cref="IMutationOperator"/> so that the
/// orchestrator can filter operators by the active CLI/config profile
/// without reflection-based name matching.
///
/// A mutator may join multiple profiles via the bitwise-or'd
/// <see cref="MutationProfile"/> flags — most defaults also belong to Stronger
/// and All. The orchestrator selects an operator if
/// <c>(operator.Profiles &amp; activeProfile) != MutationProfile.None</c>.
///
/// Backed by ADR-018; implemented in Sprint 6 alongside the operator-hierarchy
/// refactor (ADR-014).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class MutationProfileMembershipAttribute : Attribute
{
    /// <summary>The profile(s) the decorated mutator/operator participates in.</summary>
    public MutationProfile Profiles { get; }

    /// <summary>
    /// Initialises the attribute with the given profile membership set. Pass
    /// flags combined with bitwise-or, e.g.
    /// <c>MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All</c>.
    /// </summary>
    public MutationProfileMembershipAttribute(MutationProfile profiles)
    {
        Profiles = profiles;
    }
}
