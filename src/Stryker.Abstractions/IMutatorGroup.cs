using System.Collections.Generic;

namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-014): the top of the new operator hierarchy
/// <c>IMutatorGroup → IMutator → IMutationOperator</c>. A
/// <see cref="IMutatorGroup"/> is a coherent bundle of related mutators —
/// e.g. "all C# language-level operators" or "all LINQ-method swaps" —
/// that share an identifier, a description, and (optionally) a default
/// <see cref="MutationProfile"/> membership.
///
/// Groups exist primarily for documentation, CLI grouping ("show me which
/// groups are active"), and selective enable/disable at the bundle level.
/// Profiles (ADR-018) operate at the per-mutator/per-operator level and
/// trump group-level defaults when more specific.
///
/// Implementation arrives in Sprint 6.
/// </summary>
public interface IMutatorGroup
{
    /// <summary>
    /// Stable identifier of this group (e.g. "CoreOperators", "LinqOperators").
    /// </summary>
    string GroupId { get; }

    /// <summary>
    /// Human-readable summary of what the group covers.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The mutators that belong to this group. Each is an
    /// <see cref="IMutator"/> implementation; sub-operators are enumerable via
    /// each mutator's exposed <see cref="IMutationOperator"/> collection.
    /// </summary>
    IReadOnlyList<IMutator> Mutators { get; }
}
