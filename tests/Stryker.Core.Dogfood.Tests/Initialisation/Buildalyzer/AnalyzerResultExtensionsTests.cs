using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation.Buildalyzer;

/// <summary>Sprint 94 (v2.80.0) defer-doc — **architectural-deferral**. AnalyzerResultExtensions
/// tests target Buildalyzer's IAnalyzerResult interface which was removed in Sprint 1 Phase 9
/// in favor of `Stryker.Abstractions.Analysis.IProjectAnalysis` +
/// `Stryker.Utilities.MSBuild.IProjectAnalysisExtensions`. The equivalent v2.x tests live in
/// existing extension tests + production tests — no need for a 1:1 port.</summary>
public class AnalyzerResultExtensionsTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: Buildalyzer.IAnalyzerResult removed in Sprint 1 Phase 9 — replaced by IProjectAnalysis + IProjectAnalysisExtensions covered by Sprint 61 ProjectAnalysisMockBuilder integration tests.")]
    public void AnalyzerResult_DummyPlaceholder() { /* permanently skipped */ }
}
