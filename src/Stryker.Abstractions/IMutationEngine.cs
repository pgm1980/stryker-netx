namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-016, Sprint 8): marker interface that lets the orchestrator
/// dispatch on the active <see cref="MutationEngine"/> without taking a hard
/// dependency on either the recompile or hot-swap implementation.
///
/// Sprint 8 ships only the marker + the two implementations
/// (<c>RecompileEngine</c>, <c>HotSwapEngine</c> stub) so that v2.0.0-preview.3
/// users can already opt into the engine choice at config-time. The
/// <c>HotSwapEngine</c> throws <see cref="System.NotImplementedException"/>
/// at use-time until the follow-up MetadataUpdater impl lands.
///
/// The interface is intentionally minimal — the engine-specific compilation
/// and test-runner integration lives behind the implementation classes, not
/// in the contract. As the engine impls grow in Sprint-N+, additional
/// methods may be added here (likely an async <c>RunAsync(...)</c>).
/// </summary>
public interface IMutationEngine
{
    /// <summary>Identifies which <see cref="MutationEngine"/> this implementation provides.</summary>
    MutationEngine Kind { get; }
}
