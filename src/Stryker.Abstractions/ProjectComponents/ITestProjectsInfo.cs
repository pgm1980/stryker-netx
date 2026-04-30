using System.Collections.Generic;
using Stryker.Abstractions.Analysis;

namespace Stryker.Abstractions.ProjectComponents;

public interface ITestProjectsInfo
{
    IEnumerable<IProjectAnalysis> Analyses { get; }
    IEnumerable<ITestFile> TestFiles { get; }
    IEnumerable<ITestProject> TestProjects { get; set; }

    void BackupOriginalAssembly(IProjectAnalysis sourceProject);
    IReadOnlyList<string> GetTestAssemblies();
    void RestoreOriginalAssembly(IProjectAnalysis sourceProject);
}
