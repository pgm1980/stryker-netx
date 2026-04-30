using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.MicrosoftTestPlatform.RPC;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Represents an RPC client for the Microsoft Testing Platform, handling communication and process management.
/// </summary>
public sealed class TestingPlatformClient : ITestingPlatformClient
{
    private readonly TcpClient _tcpClient;
    private readonly IProcessHandle _processHandler;
    private readonly TargetHandler _targetHandler = new();
    private readonly StringBuilder _disconnectionReason = new();

    /// <summary>
    /// Initializes a new instance of <see cref="TestingPlatformClient"/>.
    /// </summary>
    public TestingPlatformClient(JsonRpc jsonRpc, TcpClient tcpClient, IProcessHandle processHandler, ILogger logger, string? rpcLogFilePath = null)
    {
        JsonRpcClient = jsonRpc;
        _tcpClient = tcpClient;
        _processHandler = processHandler;
        JsonRpcClient.AddLocalRpcTarget(
            _targetHandler,
            new JsonRpcTargetOptions
            {
                MethodNameTransform = CommonMethodNameTransforms.CamelCase,
            });

        if (rpcLogFilePath is not null)
        {
            JsonRpcClient.TraceSource.Switch.Level = SourceLevels.All;
            JsonRpcClient.TraceSource.Listeners.Add(new FileRpcListener(rpcLogFilePath, logger));
        }

        JsonRpcClient.Disconnected += JsonRpcClient_Disconnected;
        JsonRpcClient.StartListening();
    }

    private void JsonRpcClient_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        _disconnectionReason.AppendLine("Disconnected reason:");
        _disconnectionReason.AppendLine(CultureInfo.InvariantCulture, $"{e.Reason}");
        _disconnectionReason.AppendLine(e.Description);
        _disconnectionReason.AppendLine(CultureInfo.InvariantCulture, $"{e.Exception}");
    }

    /// <inheritdoc />
    public int ExitCode => _processHandler.ExitCode;

    /// <inheritdoc />
    public async Task<int> WaitServerProcessExitAsync()
    {
        await _processHandler.WaitForExitAsync().ConfigureAwait(false);
        return _processHandler.ExitCode;
    }

    /// <summary>
    /// The underlying JsonRpc client.
    /// </summary>
    public JsonRpc JsonRpcClient { get; }

    private async Task CheckedInvokeAsync(Func<Task> func)
    {
        try
        {
            await func().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_disconnectionReason.Length > 0)
            {
                throw new InvalidOperationException($"{ex.Message}\n{_disconnectionReason}", ex);
            }

            throw;
        }
    }

    private async Task<T> CheckedInvokeAsync<T>(Func<Task<T>> func, bool @checked = true)
    {
        try
        {
            return await func().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (@checked)
            {
                if (_disconnectionReason.Length > 0)
                {
                    throw new InvalidOperationException($"{ex.Message}\n{_disconnectionReason}", ex);
                }

                throw;
            }
        }

        return default!;
    }

    /// <inheritdoc />
    public void RegisterLogListener(LogsCollector listener)
        => _targetHandler.RegisterLogListener(listener);

    /// <inheritdoc />
    public void RegisterTelemetryListener(TelemetryCollector listener)
        => _targetHandler.RegisterTelemetryListener(listener);

    /// <inheritdoc />
    public async Task<InitializeResponse> InitializeAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(3));
        return await CheckedInvokeAsync(async () => await JsonRpcClient.InvokeWithParameterObjectAsync<InitializeResponse>(
            "initialize",
            new InitializeRequest(Environment.ProcessId, new ClientInfo("test-client"),
                new ClientCapabilities(new ClientTestingCapabilities(DebuggerProvider: false))), cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExitAsync(bool gracefully = true)
    {
        if (gracefully)
        {
            using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(3));
            await CheckedInvokeAsync(async () => await JsonRpcClient.NotifyWithParameterObjectAsync("exit", new object()).ConfigureAwait(false)).ConfigureAwait(false);
        }
        else
        {
            _tcpClient.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<ResponseListener> DiscoverTestsAsync(Guid requestId, Func<TestNodeUpdate[], Task> action, bool @checked = true)
        => await CheckedInvokeAsync(
            async () =>
            {
                using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(3));
                var discoveryListener = new TestNodeUpdatesResponseListener(requestId, action);
                _targetHandler.RegisterResponseListener(discoveryListener);
                await JsonRpcClient.InvokeWithParameterObjectAsync("testing/discoverTests", new DiscoveryRequest(RunId: requestId), cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
                return discoveryListener;
            }, @checked).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ResponseListener> RunTestsAsync(Guid requestId, Func<TestNodeUpdate[], Task> action, TestNode[]? testNodes = null)
        => await CheckedInvokeAsync(async () =>
        {
            using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMinutes(3));
            var runListener = new TestNodeUpdatesResponseListener(requestId, action);
            _targetHandler.RegisterResponseListener(runListener);
            await JsonRpcClient.InvokeWithParameterObjectAsync("testing/runTests", new RunTestsRequest(RunId: requestId, TestCases: testNodes), cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
            return runListener;
        }).ConfigureAwait(false);

    /// <inheritdoc />
    public void Dispose()
    {
        JsonRpcClient.Dispose();
        _tcpClient.Dispose();
    }

    /// <summary>
    /// Represents a single log entry forwarded by the test server.
    /// </summary>
    public record Log(Microsoft.Testing.Platform.Logging.LogLevel LogLevel, string Message);

    private sealed class TargetHandler
    {
        private readonly ConcurrentDictionary<Guid, ResponseListener> _listeners
            = new();

        private readonly ConcurrentBag<LogsCollector> _logListeners
            = [];

        private readonly ConcurrentBag<TelemetryCollector> _telemetryPayloads
            = [];

        public void RegisterTelemetryListener(TelemetryCollector listener)
            => _telemetryPayloads.Add(listener);

        public void RegisterLogListener(LogsCollector listener)
            => _logListeners.Add(listener);

        public void RegisterResponseListener(ResponseListener responseListener)
            => _ = _listeners.TryAdd(responseListener.RequestId, responseListener);

        [JsonRpcMethod("client/attachDebugger", UseSingleObjectParameterDeserialization = true)]
        public static Task AttachDebuggerAsync(AttachDebuggerInfo attachDebuggerInfo) => throw new NotSupportedException("Debugger attach is not supported by stryker-netx test runs.");

        [JsonRpcMethod("testing/testUpdates/tests")]
        public async Task TestsUpdateAsync(Guid runId, TestNodeUpdate[]? changes)
        {
            if (_listeners.TryGetValue(runId, out var responseListener))
            {
                if (changes is null)
                {
                    responseListener.Complete();
                    _listeners.TryRemove(runId, out _);
                    return;
                }

                await responseListener.OnMessageReceiveAsync(changes).ConfigureAwait(false);
            }
        }

        [JsonRpcMethod("telemetry/update", UseSingleObjectParameterDeserialization = true)]
        public Task TelemetryAsync(TelemetryPayload telemetry)
        {
            foreach (var listener in _telemetryPayloads)
            {
                listener.Add(telemetry);
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod("client/log")]
        public Task LogAsync(string level, string message)
        {
            foreach (var listener in _logListeners)
            {
                listener.Add(new Log(Enum.Parse<Microsoft.Testing.Platform.Logging.LogLevel>(level), message));
            }

            return Task.CompletedTask;
        }
    }
}
