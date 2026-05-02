using Xunit;

namespace Stryker.Core.Dogfood.Tests.ToolHelpers;

/// <summary>Sprint 94 (v2.80.0) defer-doc — **architectural-deferral**. BuildalyzerHelper tests
/// exercise the Buildalyzer library which was removed in Sprint 1 Phase 9 in favor of
/// `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`. These tests have no equivalent in our v2.x
/// — they should not be ported. Skip permanently with documented reason.</summary>
public class BuildalyzerHelperTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: Buildalyzer removed in Sprint 1 Phase 9 (replaced with MSBuildWorkspace) — these tests are no-op-by-design in our v2.x.")]
    public void BuildalyzerHelper_DummyPlaceholder() { /* permanently skipped */ }
}
