using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 94 (v2.80.0) defer-doc placeholder. CSharpRollbackProcess (903 LOC upstream)
/// is the largest non-Init test file: drives the full mutation-level rollback heuristic across
/// dozens of Roslyn diagnostic IDs. Defer to dedicated rollback deep-port sprint.</summary>
public class CSharpRollbackProcessTests
{
    [Fact(Skip = "903 LOC + full diagnostic-ID matrix — defer to rollback deep-port sprint.")]
    public void Rollback_ShouldRemoveBrokenMutant() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Rollback_ShouldHandleMultipleMutantsInBlock() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Rollback_ShouldRestoreOriginalSyntax() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Rollback_ShouldRespectAttribute() { /* placeholder */ }
}
