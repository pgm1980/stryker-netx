namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Wraps TCP listener operations for testability.
/// </summary>
internal interface ITestServerListener : IDisposable
{
    /// <summary>
    /// Asynchronously accepts the first incoming TCP connection and returns the resulting stream and disposable handle.
    /// </summary>
    Task<(Stream Stream, IDisposable Connection)> AcceptConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the underlying listener.
    /// </summary>
    void Stop();
}
