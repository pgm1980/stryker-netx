namespace Stryker.Core.Initialisation;

public interface INugetRestoreProcess
{
    void RestorePackages(string solutionPath, string? msbuildPath = null);
}
