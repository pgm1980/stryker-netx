using System;
using Stryker.Abstractions;

namespace Stryker.Core.Engines;

/// <summary>
/// v2.0.0 (ADR-016, Sprint 8): SCAFFOLDING for the in-process MetadataUpdater
/// hot-swap engine. The Sprint-8 Maxential locked the design (see
/// architecture_specification.md ADR-016 + sprint_8_lessons.md), but the
/// actual implementation — emit IL deltas via
/// <c>Microsoft.CodeAnalysis.CSharp.EmitDifference</c>, apply via
/// <c>System.Reflection.Metadata.MetadataUpdater.ApplyUpdate</c>, and integrate
/// with the MTP test-runner host process — is a focused follow-up sub-sprint.
///
/// For v2.0.0-preview.3, instantiating this engine works (the orchestrator
/// can dispatch on <see cref="Kind"/>) but invoking the still-to-be-added
/// <c>RunAsync</c> hook throws so users opting in get a clear error.
/// </summary>
public sealed class HotSwapEngine : IMutationEngine
{
    /// <inheritdoc />
    public MutationEngine Kind => MutationEngine.HotSwap;

    /// <summary>
    /// Sprint 8 scaffolding gate: any code path that actually tries to run
    /// the hot-swap engine bails here with a clear pointer to ADR-016. Once
    /// the MetadataUpdater impl lands (post-v2.0.0-preview.3), this method
    /// disappears and the engine transparently takes over from
    /// <see cref="RecompileEngine"/>.
    /// </summary>
    public static void ThrowIfInvoked() =>
        throw new NotSupportedException(
            "HotSwap engine scaffolding only — full MetadataUpdater impl is a focused follow-up sub-sprint. " +
            "See ADR-016 in _docs/architecture spec/architecture_specification.md and " +
            "_docs/sprint_8_lessons.md for the design + path forward. " +
            "Run with --engine recompile (default) for v2.0.0-preview.3.");
}
