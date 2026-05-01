using System;

namespace Stryker.Abstractions;

/// <summary>
/// <para><b>Obsolete in v2.2.0 — deprecated per ADR-021.</b></para>
///
/// <para>This enum was introduced in v2.0.0 (ADR-016, Sprint 8) as the
/// selector for a planned <c>HotSwap</c> mutation-execution engine. The
/// underlying ADR-016 was based on a wrong mental model of Stryker.NET's
/// cost structure: the assumption that Stryker compiles per mutant and
/// would therefore benefit from a hot-swap pattern is incorrect — Stryker
/// compiles ALL mutations into a single assembly with runtime
/// <c>ActiveMutationId</c> switching. The "5–10× perf boost" claim of
/// ADR-016 has no basis in the actual pipeline.</para>
///
/// <para>v2.2.0 walks back ADR-016 (see ADR-021) and deletes the
/// <c>HotSwapEngine</c> + <c>RecompileEngine</c> implementation classes.
/// The enum remains as a deprecated shim for v2.x source compatibility;
/// v3.0 may hard-remove it.</para>
///
/// <para>Use the existing <c>--coverage-analysis</c> flag (default
/// <c>perTest</c>) for the actual perf-relevant configuration; future
/// incremental-mutation-testing work is tracked in ADR-022 (Proposed).</para>
/// </summary>
// S1133 ("don't forget to remove deprecated code"): intentionally deferred to v3.0
// per ADR-021 + ADR-022. The shim is the v2.x backwards-compat surface; removing
// it before v3.0 would break source-level consumers.
#pragma warning disable S1133
[Obsolete("Deprecated in v2.2.0 (ADR-021): HotSwap engine was based on a wrong mental model of Stryker.NET's cost structure. The enum is kept as a shim; v3.0 may remove it. Use --coverage-analysis for performance tuning instead.")]
public enum MutationEngine
#pragma warning restore S1133
{
    /// <summary>Originally: v1.x default compile-per-mutant marker. In v2.2.0, no functional difference (Stryker.NET has always done all-mutations-in-one-compile).</summary>
    Recompile = 0,

    /// <summary>Originally: v2.0.0 hot-swap target. Removed per ADR-021; setting this is treated identically to <c>Recompile</c> with a deprecation warning.</summary>
    HotSwap = 1,
}
