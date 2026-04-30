using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CliWrap;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// CliWrap-based implementation of <see cref="IProcessHandle"/> wrapping a running dotnet process.
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessHandle(CommandTask<CommandResult> commandTask, Stream output) : IProcessHandle, IDisposable
{
    private bool _disposed;

    /// <inheritdoc />
    public int Id { get; } = commandTask.ProcessId;

    /// <inheritdoc />
    public string ProcessName { get; } = "dotnet";

    /// <inheritdoc />
    public int ExitCode { get; private set; }

    /// <inheritdoc />
    public TextWriter StandardInput => new StringWriter();

    /// <inheritdoc />
    public TextReader StandardOutput
    {
        get
        {
            // Output is rarely consumed, and when it is, it's from a file stream
            if (output.CanSeek && output.Position != 0)
            {
                output.Position = 0;
            }
            return new StreamReader(output);
        }
    }

    /// <inheritdoc />
    public void Kill()
    {
        try
        {
            using var process = Process.GetProcessById(Id);
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Process may have already exited; treat as no-op (debug-only telemetry would be misleading here).
            Debug.WriteLine($"ProcessHandle.Kill: process {Id} kill swallowed: {ex.GetType().Name}");
        }
    }

    /// <inheritdoc />
    public Task<int> StopAsync() => throw new NotSupportedException("StopAsync is not supported by the CliWrap-backed ProcessHandle. Use Kill() to terminate.");

    /// <inheritdoc />
    public async Task<int> WaitForExitAsync()
    {
        var commandResult = await commandTask.ConfigureAwait(false);
        return ExitCode = commandResult.ExitCode;
    }

    /// <inheritdoc />
    public Task WriteInputAsync(string input)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged resources used by this <see cref="ProcessHandle"/>.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // CliWrap's CommandTask.Dispose() throws if the task hasn't completed.
            // Kill the process first, then wait briefly for the task to complete before disposing.
            if (!commandTask.Task.IsCompleted)
            {
                Kill();
                try
                {
                    commandTask.Task.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Process may not finish in time; proceed with disposal anyway.
                    Debug.WriteLine($"ProcessHandle.Dispose: wait swallowed: {ex.GetType().Name}");
                }
            }

            try
            {
                commandTask.Dispose();
            }
            catch (InvalidOperationException)
            {
                // Task may still not be in a completion state after kill — disposal is best-effort.
            }
        }

        _disposed = true;
    }
}
