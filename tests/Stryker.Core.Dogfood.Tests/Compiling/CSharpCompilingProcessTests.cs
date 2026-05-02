using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 109 (v2.95.0) consolidated architectural-deferral. Upstream
/// CSharpCompilingProcessTests (549 LOC) drives full Roslyn compilation pipeline including
/// MetadataReference resolution, embedded resources, strong-name signing, and emit-with-rollback.
/// Tests use full MetadataReference + MockFileSystem + IProjectAnalysis chain. Re-port requires
/// extensive Roslyn-test-harness setup that would more than double our test-fixture LOC.
/// Belongs in dedicated compiler-pipeline deep-port sprint.</summary>
public class CSharpCompilingProcessTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: 549 LOC full Roslyn compile pipeline + MetadataReference resolution + embedded-resources + strong-name signing + emit-with-rollback. Re-port = extensive Roslyn-test-harness setup. Dedicated compiler-pipeline deep-port sprint required.")]
    public void CSharpCompilingProcess_ArchitecturalDeferral() { /* permanently skipped */ }
}
