using System;
using System.Collections.Generic;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;

namespace Stryker.Configuration.Options.Inputs;

/// <summary>
/// v2.0.0 (ADR-016, Sprint 8): selector for the mutation engine.
/// Defaults to <see cref="MutationEngine.Recompile"/> — the v1.x behaviour —
/// so v2.0.0-preview.3 users see no change unless they explicitly opt in to
/// <see cref="MutationEngine.HotSwap"/>. Selecting <c>HotSwap</c> currently
/// throws at runtime until the MetadataUpdater impl lands; the validator
/// nevertheless accepts the value at parse-time so config files are
/// forward-compatible.
/// </summary>
public class MutationEngineInput : Input<string>
{
    public override string Default => MutationEngine.Recompile.ToString();

    protected override string Description =>
        "Mutation execution engine. 'Recompile' (default, v1.x behaviour) compiles per mutant. " +
        "'HotSwap' will use MetadataUpdater for in-process IL deltas (Sprint 8 ships scaffolding " +
        "only — selecting HotSwap throws at runtime until the follow-up impl lands).";

    protected override IEnumerable<string> AllowedOptions => EnumToStrings(typeof(MutationEngine));

    public MutationEngine Validate()
    {
        if (SuppliedInput is null)
        {
            return MutationEngine.Recompile;
        }
        else if (Enum.TryParse(SuppliedInput, ignoreCase: true, out MutationEngine engine))
        {
            return engine;
        }
        else
        {
            throw new InputException(
                $"The given mutation engine ({SuppliedInput}) is invalid. " +
                $"Valid options are: [{string.Join(", ", Enum.GetValues<MutationEngine>())}]");
        }
    }
}
