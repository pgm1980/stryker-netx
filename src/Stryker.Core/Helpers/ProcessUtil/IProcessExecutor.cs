using System.Collections.Generic;

namespace Stryker.Core.Helpers.ProcessUtil;

/// <summary>
/// Used for mocking System.Process
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Starts a process and returns the result when done. Takes an environment variable for active mutation
    /// </summary>
    /// <param name="path">The path the process will use as base path</param>
    /// <param name="application">example: dotnet</param>
    /// <param name="arguments">example: --no-build</param>
    /// <param name="environmentVariables">Environment variables (and their values)</param>
    /// <param name="timeoutMs">time allotted to the process for execution (0 or-1 for no limit)</param>
    /// <returns>ProcessResult</returns>
    ProcessResult Start(string path, string application, string arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables = null, int timeoutMs = 0);

}
