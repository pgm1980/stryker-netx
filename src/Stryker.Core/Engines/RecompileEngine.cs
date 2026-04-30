using Stryker.Abstractions;

namespace Stryker.Core.Engines;

/// <summary>
/// v2.0.0 (ADR-016, Sprint 8): the v1.x default mutation engine — compile once
/// per mutant, run the test suite, discard the assembly. The actual compile-
/// and-test loop lives in <c>CsharpCompilingProcess</c> + <c>MutationTestExecutor</c>;
/// this class is currently a marker the orchestrator dispatches on rather than
/// a separate execution path. As the engine boundary firms up in Sprint-N+,
/// the recompile loop will move into this class so the engine becomes truly
/// swappable.
/// </summary>
public sealed class RecompileEngine : IMutationEngine
{
    /// <inheritdoc />
    public MutationEngine Kind => MutationEngine.Recompile;
}
