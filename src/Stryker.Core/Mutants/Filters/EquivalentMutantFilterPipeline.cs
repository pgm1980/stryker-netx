using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (ADR-017): runs a chain of <see cref="IEquivalentMutantFilter"/>
/// instances and returns true as soon as ANY filter classifies a mutation
/// as equivalent. The any-match short-circuit semantics match PIT's
/// equivalent-mutant exclusion model — multiple filter contributions stack
/// up the catch-rate without each having to know the others.
///
/// The pipeline is constructed once per orchestrator run; filter IDs are
/// captured for diagnostic reports of WHICH rule caught a given mutant.
/// </summary>
public sealed class EquivalentMutantFilterPipeline
{
    private readonly IReadOnlyList<IEquivalentMutantFilter> _filters;

    public EquivalentMutantFilterPipeline(IEnumerable<IEquivalentMutantFilter> filters)
    {
        _filters = [.. filters];
    }

    /// <summary>
    /// Default pipeline shipped with v2.0.0: identity-arithmetic + idempotent-boolean.
    /// New filters should be added here as Sprint-7+ work expands the catalogue.
    /// </summary>
    public static EquivalentMutantFilterPipeline Default { get; } = new(
    [
        new IdentityArithmeticFilter(),
        new IdempotentBooleanFilter(),
        // v2.0.0 Sprint 9: cargo-mutants-style conservative defaults for unsigned types.
        new ConservativeDefaultsEqualityFilter(),
        // v2.1.0 Sprint 14 (mutmut-style Type-Checker integration): re-parses
        // the replacement node and short-circuits on parser-level errors.
        new RoslynDiagnosticsEquivalenceFilter(),
    ]);

    /// <summary>
    /// Returns the FilterId of the first matching filter, or <c>null</c> when
    /// no filter classifies the mutation as equivalent.
    /// </summary>
    public string? FindEquivalentFilter(Mutation mutation, SemanticModel? semanticModel) =>
        _filters.FirstOrDefault(f => f.IsEquivalent(mutation, semanticModel))?.FilterId;

    /// <summary>Convenience: was the mutation flagged as equivalent by any filter?</summary>
    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel) =>
        FindEquivalentFilter(mutation, semanticModel) is not null;

    /// <summary>The full set of registered filters, exposed for diagnostics.</summary>
    public IReadOnlyList<IEquivalentMutantFilter> Filters => _filters;
}
