namespace Stryker.Core.Initialisation;

public interface IInitialBuildProcess
{
    void InitialBuild(bool fullFramework,
        string projectPath,
        string solutionPath,
        string? configuration = null,
        string? platform = null,
        string? targetFramework = null,
        string? msbuildPath = null);
}
