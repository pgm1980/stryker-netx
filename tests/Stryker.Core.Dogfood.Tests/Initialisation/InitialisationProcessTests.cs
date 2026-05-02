using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholders. InitialisationProcess upstream tests use
/// Mock&lt;IInputFileResolver&gt; + Mock&lt;IInitialBuildProcess&gt; + Mock&lt;IInitialTestProcess&gt; chains;
/// our v2.x DI graph differs (Buildalyzer removal). Defer to dedicated Initialisation deep-port sprint.</summary>
public class InitialisationProcessTests
{
    [Fact(Skip = "Heavy DI mock chain + Buildalyzer-shape adapters — defer to Initialisation deep-port sprint.")]
    public void InitialisationProcess_ShouldCallNeededResolvers() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void InitialisationProcess_ShouldThrowOnFailedInitialTestRun() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void InitialisationProcess_NoTestProjectsFound() { /* placeholder */ }
}
