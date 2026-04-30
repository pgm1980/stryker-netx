using System.Collections.Generic;
using System.Threading.Tasks;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.SourceProjects;

namespace Stryker.Core.Initialisation;

public interface IInitialisationProcess
{
    /// <summary>
    /// Gets all projects to mutate based on the given options
    /// </summary>
    /// <param name="options">stryker options</param>
    /// <returns>an enumeration of <see cref="SourceProjectInfo"/>, one for each found project (if any).</returns>
    IReadOnlyCollection<SourceProjectInfo> GetMutableProjectsInfo(IStrykerOptions options);

    void BuildProjects(IStrykerOptions options, IEnumerable<SourceProjectInfo> projects);

    Task<IReadOnlyCollection<MutationTestInput>> GetMutationTestInputsAsync(IStrykerOptions options,
        IReadOnlyCollection<SourceProjectInfo> projects, ITestRunner runner);
}
