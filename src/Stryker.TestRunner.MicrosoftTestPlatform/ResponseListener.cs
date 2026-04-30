namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Base class for response listeners that wait for asynchronous JSON-RPC notifications related to a specific request.
/// </summary>
public abstract class ResponseListener(Guid requestId)
{
    private readonly TaskCompletionSource _allMessageReceived = new();

    /// <summary>
    /// Gets the unique request identifier this listener is associated with.
    /// </summary>
    public Guid RequestId { get; } = requestId;

    /// <summary>
    /// Invoked when a message belonging to the request is received.
    /// </summary>
    public abstract Task OnMessageReceiveAsync(object message);

    internal void Complete() => _allMessageReceived.SetResult();

    // VSTHRD003: WaitCompletionAsync deliberately returns the underlying TaskCompletionSource Task
    // for direct awaiting by callers; the StreamJsonRpc threading-context guidance is NOT applicable
    // because the client owns its own JsonRpc instance synchronization.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks — TaskCompletionSource owned by this class
    /// <summary>
    /// Waits indefinitely for the listener to complete.
    /// </summary>
    public Task WaitCompletionAsync() => _allMessageReceived.Task;
#pragma warning restore VSTHRD003

    /// <summary>
    /// Waits for the listener to complete, with a timeout. Returns <see langword="true"/> when completed in time, <see langword="false"/> on timeout.
    /// </summary>
    public async Task<bool> WaitCompletionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        try
        {
            var completedTask = await Task.WhenAny(_allMessageReceived.Task, Task.Delay(Timeout.Infinite, linkedCts.Token)).ConfigureAwait(false);
            return completedTask == _allMessageReceived.Task;
        }
        catch (OperationCanceledException)
        {
            return _allMessageReceived.Task.IsCompleted;
        }
    }
}
