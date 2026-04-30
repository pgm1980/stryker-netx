using Microsoft.CodeAnalysis;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.0.0 (ADR-017): pipeline-stage that decides whether a candidate
/// <see cref="Mutation"/> is semantically equivalent to the original code
/// — i.e. would never be killable because it doesn't change behaviour.
/// Equivalent mutants waste test time and falsely lower the mutation score.
///
/// Filters are conservative by design — they should return <c>true</c>
/// only when they're highly confident the mutation is equivalent. False
/// negatives (a real mutant slipping through as "non-equivalent" and
/// surviving as untested) are preferable to false positives (a real bug
/// being filtered away as "equivalent" and never tested).
///
/// Implementations are stateless and may be cached by the orchestrator.
/// </summary>
public interface IEquivalentMutantFilter
{
    /// <summary>
    /// Stable identifier of this filter (e.g. "IdentityArithmetic"). Used
    /// for diagnostic logging and report grouping.
    /// </summary>
    string FilterId { get; }

    /// <summary>
    /// Returns <c>true</c> when the filter is highly confident the given
    /// mutation produces semantically-identical behaviour to the original.
    /// </summary>
    /// <param name="mutation">The candidate mutation to inspect.</param>
    /// <param name="semanticModel">Semantic model for the source compilation,
    /// or <c>null</c> when type information is unavailable.</param>
    bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel);
}
