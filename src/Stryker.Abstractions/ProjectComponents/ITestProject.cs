using System.Collections.Generic;
using Stryker.Abstractions.Analysis;

namespace Stryker.Abstractions.ProjectComponents;

public interface ITestProject
{
    IProjectAnalysis Analysis { get; }
    string ProjectFilePath { get; }
    IEnumerable<ITestFile> TestFiles { get; }

    bool Equals(object obj);
    bool Equals(ITestProject other);
    int GetHashCode();
}
