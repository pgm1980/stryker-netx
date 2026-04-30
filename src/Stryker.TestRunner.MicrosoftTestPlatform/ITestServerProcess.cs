using Stryker.TestRunner.MicrosoftTestPlatform.Models;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Wraps the external test server process for testability.
/// </summary>
internal interface ITestServerProcess : IDisposable
{
    /// <summary>
    /// Awaits the process exit.
    /// </summary>
    Task WaitForExitAsync();

    /// <summary>
    /// <see langword="true"/> when the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Underlying process handle for low-level operations (kill, IO).
    /// </summary>
    IProcessHandle ProcessHandle { get; }
}
