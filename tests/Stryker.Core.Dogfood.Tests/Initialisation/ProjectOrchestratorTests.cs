using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 94 (v2.80.0) defer-doc placeholder. ProjectOrchestrator (507 LOC upstream)
/// orchestrates Solution → Source/Test projects → MutationTestInput chain with deep DI graph
/// (IInputFileResolver + IInitialBuildProcess + IInitialTestProcess + ITestRunner +
/// IMutationTestProcess + IReporterFactory + IBuildalyzerProvider). Defer to dedicated deep-port sprint.</summary>
public class ProjectOrchestratorTests
{
    [Fact(Skip = "507 LOC + deep DI graph + IBuildalyzerProvider abstraction (removed Sprint 1) — defer to architectural-deep-port sprint.")]
    public void ProjectOrchestrator_ShouldOrchestrate() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ProjectOrchestrator_ShouldHandleSolutionFile() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void ProjectOrchestrator_ShouldEnumerateProjects() { /* placeholder */ }
}
