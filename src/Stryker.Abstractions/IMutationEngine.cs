using System;

namespace Stryker.Abstractions;

/// <summary>
/// <para><b>Obsolete in v2.2.0 — deprecated per ADR-021.</b></para>
///
/// <para>This marker interface was introduced in v2.0.0 (ADR-016, Sprint 8)
/// to let an orchestrator dispatch on the active <see cref="MutationEngine"/>
/// without depending on the (planned) recompile / hot-swap implementations.
/// v2.2.0 walks back ADR-016 because Stryker.NET's actual cost structure
/// makes the engine abstraction unnecessary — the all-mutations-in-one-
/// assembly + <c>ActiveMutationId</c>-runtime-switching pattern that
/// Stryker.NET has used since v1.x is already efficient and there is no
/// per-mutant compile to optimize away.</para>
///
/// <para>The interface is kept as a deprecated shim for v2.x source
/// compatibility; v3.0 may hard-remove it.</para>
/// </summary>
// S1133 / CA1040 deferred to v3.0 per ADR-021. The shim is the v2.x backwards-
// compat surface; removing or "fattening" it before v3.0 would break source-level
// consumers.
#pragma warning disable S1133, CA1040
[Obsolete("Deprecated in v2.2.0 (ADR-021): the engine abstraction was based on a wrong mental model of Stryker.NET's cost structure. Implementations were removed; the interface is a shim. v3.0 may remove it.")]
public interface IMutationEngine
{
    /// <summary>Identifies which <see cref="MutationEngine"/> this implementation provides.</summary>
    MutationEngine Kind { get; }
}
#pragma warning restore S1133, CA1040
