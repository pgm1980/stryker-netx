namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-015): marker interface for mutators that REQUIRE a working
/// <see cref="Microsoft.CodeAnalysis.SemanticModel"/> to produce their
/// substitutions. Examples: typed default-return mutators (e.g.
/// <c>Task&lt;T&gt; → Task.FromResult(default(T))</c>), conservative-defaults
/// filter for unsigned types, and the Roslyn-diagnostics filter for unviable
/// mutants.
///
/// Distinguishing type-aware from purely syntax-driven mutators lets the
/// orchestrator skip type-aware mutators when no <see cref="Microsoft.CodeAnalysis.SemanticModel"/>
/// is available (e.g. when the project failed to compile) instead of erroring.
///
/// Implementation begins in Sprint 7 (SemanticModel infrastructure) and is
/// applied broadly in Sprint 9 (type-driven mutator wave).
/// </summary>
public interface ITypeAwareMutator : IMutator
{
}
