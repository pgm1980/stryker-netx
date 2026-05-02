using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 94 (v2.80.0) defer-doc placeholder. CSharpCompilingProcess (549 LOC upstream)
/// drives the full Roslyn compilation pipeline including reference resolution, embedded resources,
/// strong-name signing, and emit-with-rollback. Tests use full MetadataReference + MockFileSystem +
/// IProjectAnalysis chain. Defer to dedicated compiler-pipeline deep-port sprint.</summary>
public class CSharpCompilingProcessTests
{
    [Fact(Skip = "549 LOC of full Roslyn compile pipeline — defer to compiler-pipeline deep-port sprint.")]
    public void Compile_ShouldEmit() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Compile_ShouldRetryOnRollback() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void Compile_ShouldHandleResources() { /* placeholder */ }
}
