using System;
using System.Collections.Generic;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;

namespace Stryker.Configuration.Options.Inputs;

/// <summary>
/// v2.0.0 (ADR-018): the orthogonal mutation-profile selector. Single-value
/// input — Defaults / Stronger / All — that the orchestrator uses to filter
/// mutators by their <see cref="MutationProfileMembershipAttribute"/>.
/// Defaults to <see cref="MutationProfile.Defaults"/> for backward-compat
/// with v1.x behaviour.
/// </summary>
public class MutationProfileInput : Input<string>
{
    public override string Default => MutationProfile.Defaults.ToString();

    protected override string Description =>
        "Mutation profile (orthogonal to mutation-level). 'Defaults' is the curated mainstream set, " +
        "'Stronger' adds academically-stronger operators, 'All' enables every operator including experimental.";

    protected override IEnumerable<string> AllowedOptions => EnumToStrings(typeof(MutationProfile));

    public MutationProfile Validate()
    {
        if (SuppliedInput is null)
        {
            return MutationProfile.Defaults;
        }
        else if (Enum.TryParse(SuppliedInput, ignoreCase: true, out MutationProfile profile))
        {
            return profile;
        }
        else
        {
            throw new InputException(
                $"The given mutation profile ({SuppliedInput}) is invalid. " +
                $"Valid options are: [{string.Join(", ", Enum.GetValues<MutationProfile>())}]");
        }
    }
}
