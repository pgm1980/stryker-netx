using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 108 (v2.94.0) consolidated architectural-deferral. Upstream InputFileResolverTests
/// (1722 LOC — largest single upstream test file) exercises Buildalyzer project-graph resolution +
/// MSBuildWorkspace + .slnx/.sln parsing across hundreds of fixtures. Sprint 1 Phase 9 removed
/// Buildalyzer for `Stryker.Utilities.MSBuild.RoslynProjectAnalysis` — a 1:1 port is fundamentally
/// inapplicable. Needs structural rewrite for v2.x architecture, belongs in dedicated multi-sprint
/// InputFileResolver deep-port project.</summary>
public class InputFileResolverTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: 1722 LOC + Buildalyzer removed Sprint 1 Phase 9. Re-port = structural rewrite for IProjectAnalysis-based RoslynProjectAnalysis pipeline. Multi-sprint deep-port effort required.")]
    public void InputFileResolver_ArchitecturalDeferral_v2x() { /* permanently skipped */ }
}
