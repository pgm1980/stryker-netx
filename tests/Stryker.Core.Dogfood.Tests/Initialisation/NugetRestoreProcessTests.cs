using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 93 (v2.79.0) defer-doc placeholder. NugetRestoreProcess wraps `dotnet restore` —
/// upstream tests mock the entire process pipeline + filesystem. Defer to dedicated sprint.</summary>
public class NugetRestoreProcessTests
{
    [Fact(Skip = "Heavy IProcessExecutor + filesystem mock chain — defer.")]
    public void NugetRestoreProcess_ShouldRestorePackages() { /* placeholder */ }

    [Fact(Skip = "Defer.")]
    public void NugetRestoreProcess_ShouldThrowOnFailure() { /* placeholder */ }
}
