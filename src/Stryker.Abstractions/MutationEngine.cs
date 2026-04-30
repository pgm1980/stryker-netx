namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-016, Sprint 8): the engine that drives mutation execution.
///
/// <list type="bullet">
///   <item>
///     <c>Recompile</c> — the v1.x default: compile once per mutant, run the
///     test suite, discard the assembly. Slow but maximally compatible.
///   </item>
///   <item>
///     <c>HotSwap</c> — the v2.0.0 trampoline equivalent: emit IL deltas via
///     <c>System.Reflection.Metadata.MetadataUpdater.ApplyUpdate</c> against a
///     pre-built baseline assembly so the test host stays alive across all
///     mutants. Sprint 8 ships SCAFFOLDING only; selecting <c>HotSwap</c>
///     currently throws <see cref="System.NotImplementedException"/> with a
///     pointer to the follow-up implementation work. See ADR-016.
///   </item>
/// </list>
///
/// Sprint-8 Maxential locked the decision to scaffold toward MetadataUpdater
/// (vs the alternative <c>AssemblyLoadContext</c> approach) because every
/// realistic test runner spawns its own process per ALC unload, defeating
/// the trampoline-perf gain. MetadataUpdater patches in-process and is the
/// .NET 10 first-class API for what Stryker needs.
/// </summary>
public enum MutationEngine
{
    /// <summary>v1.x default: compile per mutant. Maximally compatible, slowest.</summary>
    Recompile = 0,

    /// <summary>
    /// v2.0.0 target: in-process IL delta apply via <c>MetadataUpdater.ApplyUpdate</c>.
    /// Sprint 8 ships scaffolding; selecting this throws until the impl arrives.
    /// </summary>
    HotSwap = 1,
}
