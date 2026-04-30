using System.Reflection;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants;

/// <summary>
/// v2.0.0 (ADR-018): membership-test helper used by <see cref="CsharpMutantOrchestrator"/>
/// and any future orchestrator to decide whether a given <see cref="IMutator"/> should
/// participate in a run. Reads the optional
/// <see cref="MutationProfileMembershipAttribute"/> on the mutator's runtime type;
/// mutators without the attribute are treated as members of every profile (safe default
/// for v1.x mutators that haven't been explicitly tagged yet).
/// </summary>
public static class MutatorProfileFilter
{
    /// <summary>
    /// Returns true when the mutator participates in the given <paramref name="activeProfile"/>.
    /// </summary>
    public static bool IsInProfile(IMutator mutator, MutationProfile activeProfile)
    {
        var attr = mutator.GetType().GetCustomAttribute<MutationProfileMembershipAttribute>();
        var membership = attr?.Profiles
            ?? (MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);
        return (membership & activeProfile) != MutationProfile.None;
    }
}
