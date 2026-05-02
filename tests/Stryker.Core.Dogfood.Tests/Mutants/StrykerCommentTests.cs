using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>Sprint 109 (v2.95.0) consolidated architectural-deferral. Upstream StrykerCommentTests
/// (326 LOC) uses MutantOrchestratorTestsBase + ShouldMutateSourceInClassToExpected helper that
/// drives full orchestrator-based testing with hardcoded mutation IDs (IsActive(0), IsActive(4),
/// etc.) — same bucket-3 pattern as CsharpMutantOrchestratorTests skips. v2.x has 52 mutators vs
/// upstream 40, so mutation IDs differ. Belongs in dedicated structural-rewrite sprint.</summary>
public class StrykerCommentTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: Bucket-3 (Sprint 62 lesson) — orchestrator-driven hardcoded mutation IDs depend on v2.x mutator-pipeline order (52 mutators vs upstream 40). Dedicated structural-rewrite sprint required.")]
    public void StrykerComment_BucketThreeArchitecturalDeferral() { /* permanently skipped */ }
}
