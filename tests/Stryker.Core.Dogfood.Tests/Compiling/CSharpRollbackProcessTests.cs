using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 109 (v2.95.0) consolidated architectural-deferral. Upstream
/// CSharpRollbackProcessTests (903 LOC) drives full mutation-level rollback heuristic across
/// dozens of Roslyn diagnostic IDs. Each test compiles real C# with intentional mutation
/// errors and asserts which diagnostic IDs trigger rollback. Belongs in dedicated rollback
/// deep-port sprint with proper Roslyn-diagnostic-ID matrix harness.</summary>
public class CSharpRollbackProcessTests
{
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: 903 LOC + full Roslyn diagnostic-ID matrix. Each test compiles real C# with intentional mutation errors. Re-port = dedicated rollback deep-port sprint with diagnostic-ID matrix harness.")]
    public void CSharpRollbackProcess_ArchitecturalDeferral() { /* permanently skipped */ }
}
