using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;

namespace Stryker.Configuration.Options.Inputs;

/// <summary>
/// Sprint 166 (ADR-046 §C, Aisess Wishlist #9): diagnostic flag that runs Stryker
/// UP TO AND INCLUDING the named pipeline phase, then terminates cleanly without
/// the (expensive) per-mutant test loop. Backed by <see cref="BreakAfterPhase"/>.
/// </summary>
/// <remarks>
/// Accepts case-insensitive aliases for friendlier CLI ergonomics:
/// <list type="bullet">
///   <item><c>analysis</c> → <see cref="BreakAfterPhase.Analysis"/></item>
///   <item><c>build</c> → <see cref="BreakAfterPhase.Build"/></item>
///   <item><c>initial-test-run</c> (or <c>initial-test</c>, or <c>initialtestrun</c>) → <see cref="BreakAfterPhase.InitialTestRun"/></item>
///   <item><c>mutation-generation</c> (or <c>mutation</c>, or <c>mutationgeneration</c>) → <see cref="BreakAfterPhase.MutationGeneration"/></item>
/// </list>
/// </remarks>
public class BreakAfterInput : Input<string>
{
    public override string Default => BreakAfterPhase.None.ToString();

    protected override string Description =>
        "Diagnostic flag: stop Stryker after the named pipeline phase without running the per-mutant test loop. " +
        "Useful for verifying configuration or mutation coverage without paying for the full mutation run. " +
        "Allowed values: analysis | build | initial-test-run | mutation-generation.";

    protected override IEnumerable<string> AllowedOptions =>
        // Lower-case + kebab-case versions of the BreakAfterPhase values. We surface
        // the user-friendly aliases rather than the PascalCase enum identifiers.
        ["analysis", "build", "initial-test-run", "mutation-generation"];

    /// <summary>
    /// Resolves the user-supplied string to the corresponding <see cref="BreakAfterPhase"/>.
    /// Returns <see cref="BreakAfterPhase.None"/> when the input is null or empty.
    /// Throws <see cref="InputException"/> with the allowed-values list on parse failure.
    /// </summary>
    public BreakAfterPhase Validate()
    {
        if (string.IsNullOrWhiteSpace(SuppliedInput))
        {
            return BreakAfterPhase.None;
        }

        var normalized = SuppliedInput.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var phase = normalized switch
        {
            "NONE" => BreakAfterPhase.None,
            "ANALYSIS" => BreakAfterPhase.Analysis,
            "BUILD" => BreakAfterPhase.Build,
            "INITIALTESTRUN" or "INITIALTEST" => BreakAfterPhase.InitialTestRun,
            "MUTATIONGENERATION" or "MUTATION" or "MUTATIONGEN" => BreakAfterPhase.MutationGeneration,
            _ => (BreakAfterPhase?)null,
        };

        if (phase is null)
        {
            throw new InputException(
                string.Create(CultureInfo.InvariantCulture,
                    $"The given --break-after value ('{SuppliedInput}') is invalid. ") +
                "Allowed values: [" + string.Join(", ", AllowedOptions) + "].");
        }

        return phase.Value;
    }
}
