using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 108 (v2.94.0) consolidated architectural-deferral. Upstream ProjectOrchestratorTests
/// (507 LOC) inherits <c>BuildAnalyzerTestsBase</c> and uses <c>Buildalyzer</c> namespace directly —
/// both removed in Sprint 1 Phase 9 (replaced with MSBuildWorkspace/IProjectAnalysis). The upstream
/// `SourceProjectAnalyzerMock`, `TestProjectAnalyzerMock`, `BuildProjectAnalyzerMock`,
/// `GetSourceProjectDefaultProperties` helpers all return Mock&lt;IAnalyzerResult&gt; objects — we use
/// Mock&lt;IProjectAnalysis&gt; instead. Re-porting requires writing a v2.x-shape `BuildAnalyzerTestsBase`
/// equivalent producing IProjectAnalysis mocks AND adapting all 10 tests' filesystem+solution-parsing
/// assumptions. Belongs in a dedicated multi-sprint ProjectOrchestrator deep-port effort with proper
/// IProjectAnalysisMockBuilder integration (Sprint 61 builder is the right starting point).</summary>
public class ProjectOrchestratorTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: BuildAnalyzerTestsBase + Buildalyzer namespace removed Sprint 1 Phase 9. Re-port requires writing v2.x-shape BuildAnalyzerTestsBase analog producing IProjectAnalysis mocks via ProjectAnalysisMockBuilder + adapting all 10 upstream tests' filesystem+solution-parsing assumptions.")]
    public void ProjectOrchestrator_ArchitecturalDeferral_v2x() { /* permanently skipped */ }
}
