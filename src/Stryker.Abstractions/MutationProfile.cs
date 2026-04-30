using System;

namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-018): orthogonal mutation-profile axis added alongside the
/// existing ordinal <see cref="MutationLevel"/>. Profiles cluster mutators
/// by intent rather than by sequential strength — DEFAULTS is the curated
/// mainstream set, STRONGER adds academically-stronger operators (PIT
/// terminology), ALL turns on every operator including experimental ones.
///
/// Combinable as <see cref="FlagsAttribute"/>: a mutator may participate in
/// multiple profiles simultaneously (and most do — most "Defaults" mutators
/// also belong to Stronger and All).
///
/// Backed by `mutation_framework_comparison.md` §5 Punkt 6 + §3 PIT-Stärken;
/// implementation arrives in Sprint 6 alongside ADR-014's operator-hierarchy
/// refactor.
/// </summary>
[Flags]
public enum MutationProfile
{
    /// <summary>Empty membership; sentinel value, not user-selectable.</summary>
    None = 0,

    /// <summary>The curated default set Stryker runs without explicit configuration.</summary>
    Defaults = 1 << 0,

    /// <summary>
    /// Defaults plus academically-stronger operators (PIT "STRONGER" equivalent).
    /// Includes higher-cost mutations such as ROR-Vollmatrix expansions and
    /// AOD (Arithmetic Operator Deletion) that produce more equivalent-mutant
    /// candidates but higher mutation-score discrimination.
    /// </summary>
    Stronger = 1 << 1,

    /// <summary>
    /// Every operator including experimental and controversial ones (e.g.
    /// access-modifier mutation). Use only with full test-suite coverage.
    /// </summary>
    All = 1 << 2,
}
