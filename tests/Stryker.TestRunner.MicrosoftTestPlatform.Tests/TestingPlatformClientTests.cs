using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nerdbank.Streams;
using StreamJsonRpc;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 36 (v2.23.0) port of upstream stryker-net 4.14.1
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/TestingPlatformClientTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Production API drift: ctor signature `(JsonRpc, TcpClient, IProcessHandle, bool)` upstream
/// → `(JsonRpc, TcpClient, IProcessHandle, ILogger, string? rpcLogFilePath = null)` stryker-netx
/// (Sprint 33 lesson surfaced again on a different class).
/// All `false` last-arg → `NullLogger.Instance`.
/// Closes MTP dogfood track (Sprints 30-36).
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "MA0004:Use Task.ConfigureAwait",
    Justification = "xUnit1030 forbids ConfigureAwait(false) in test bodies; xUnit wins.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "xUnit1031:Do not use blocking task operations in test method",
    Justification = "Upstream port uses .GetAwaiter().GetResult() in one synchronous test (ExitAsync_NotGracefully_*); preserving 1:1 parity.")]
public class TestingPlatformClientTests
{
    [Fact]
    public void Constructor_ShouldInitializeClient()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var tcpClient = new Mock<TcpClient>();
        var processHandle = new Mock<IProcessHandle>();

        using var client = new TestingPlatformClient(jsonRpc, tcpClient.Object, processHandle.Object, NullLogger.Instance);

        client.Should().NotBeNull();
        client.JsonRpcClient.Should().Be(jsonRpc);
    }

    [Fact]
    public void ExitCode_ShouldReturnProcessHandleExitCode()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var tcpClient = new Mock<TcpClient>();
        var processHandle = new Mock<IProcessHandle>();
        processHandle.SetupGet(x => x.ExitCode).Returns(42);

        using var client = new TestingPlatformClient(jsonRpc, tcpClient.Object, processHandle.Object, NullLogger.Instance);

        client.ExitCode.Should().Be(42);
    }

    [Fact]
    public async Task WaitServerProcessExitAsync_ShouldReturnProcessExitCode()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var tcpClient = new Mock<TcpClient>();
        var processHandle = new Mock<IProcessHandle>();
        processHandle.Setup(x => x.WaitForExitAsync()).ReturnsAsync(0);
        processHandle.SetupGet(x => x.ExitCode).Returns(0);

        using var client = new TestingPlatformClient(jsonRpc, tcpClient.Object, processHandle.Object, NullLogger.Instance);
        var exitCode = await client.WaitServerProcessExitAsync();

        exitCode.Should().Be(0);
        processHandle.Verify(x => x.WaitForExitAsync(), Times.Once);
    }

    [Fact]
    public void RegisterLogListener_ShouldAcceptListener()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var tcpClient = new Mock<TcpClient>();
        var processHandle = new Mock<IProcessHandle>();
        var listener = new LogsCollector();

        using var client = new TestingPlatformClient(jsonRpc, tcpClient.Object, processHandle.Object, NullLogger.Instance);

        client.RegisterLogListener(listener);
        listener.Should().NotBeNull();
    }

    [Fact]
    public void RegisterTelemetryListener_ShouldAcceptListener()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var tcpClient = new Mock<TcpClient>();
        var processHandle = new Mock<IProcessHandle>();
        var listener = new TelemetryCollector();

        using var client = new TestingPlatformClient(jsonRpc, tcpClient.Object, processHandle.Object, NullLogger.Instance);

        client.RegisterTelemetryListener(listener);
        listener.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        var stream = new MemoryStream();
        var messageHandler = new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter());
        var testableJsonRpc = new TestableJsonRpc(messageHandler);
        var testableTcpClient = new TestableTcpClient();

        var processHandle = new Mock<IProcessHandle>();

        var client = new TestingPlatformClient(testableJsonRpc, testableTcpClient, processHandle.Object, NullLogger.Instance);

        client.Dispose();

        testableJsonRpc.WasDisposed.Should().BeTrue("JsonRpc.Dispose should be called when TestingPlatformClient is disposed");
        testableTcpClient.WasDisposed.Should().BeTrue("TcpClient.Dispose should be called when TestingPlatformClient is disposed");
    }

    [Fact]
    public void Dispose_ShouldNotDisposeProcessHandle()
    {
        var stream = new MemoryStream();
        var messageHandler = new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter());
        var jsonRpc = new TestableJsonRpc(messageHandler);
        var tcpClient = new TestableTcpClient();
        var processHandle = new Mock<IProcessHandle>();

        var client = new TestingPlatformClient(jsonRpc, tcpClient, processHandle.Object, NullLogger.Instance);

        client.Dispose();

        // ProcessHandle is not owned by TestingPlatformClient; it should not be disposed here
        processHandle.Verify(x => x.Kill(), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_ShouldInvokeInitializeRpcMethod()
    {
        using var connection = RpcTestConnection.Create();

        connection.ServerRpc.AddLocalRpcTarget(new FakeTestServer());
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var response = await client.InitializeAsync();

        response.Should().NotBeNull();
        response.ServerInfo.Name.Should().Be("fake-server");
        response.Capabilities.Testing.SupportsDiscovery.Should().BeTrue();
    }

    [Fact]
    public async Task ExitAsync_Gracefully_ShouldSendExitNotification()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        await client.ExitAsync(gracefully: true);

        // NotifyWithParameterObjectAsync is fire-and-forget; allow processing time
        await Task.Delay(50);

        server.ExitCalled.Should().BeTrue();
    }

    [Fact]
    public void ExitAsync_NotGracefully_ShouldDisposeTcpClient()
    {
        var stream = new MemoryStream();
        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter()));
        var testableTcpClient = new TestableTcpClient();
        var processHandle = new Mock<IProcessHandle>();

        using var client = new TestingPlatformClient(jsonRpc, testableTcpClient, processHandle.Object, NullLogger.Instance);

        // ExitAsync(gracefully: false) disposes TcpClient directly
        client.ExitAsync(gracefully: false).GetAwaiter().GetResult();

        testableTcpClient.WasDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldInvokeDiscoverTestsRpcMethod()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        List<TestNodeUpdate> receivedUpdates = [];

        var listener = await client.DiscoverTestsAsync(requestId, updates =>
        {
            receivedUpdates.AddRange(updates);
            return Task.CompletedTask;
        });

        listener.Should().NotBeNull();
        server.LastDiscoveryRunId.Should().Be(requestId);
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldReceiveTestUpdatesFromServer()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        List<TestNodeUpdate> receivedUpdates = [];

        var listener = await client.DiscoverTestsAsync(requestId, updates =>
        {
            receivedUpdates.AddRange(updates);
            return Task.CompletedTask;
        });

        // Server sends test updates via the client's TargetHandler callback
        var testNode = new TestNode("uid-1", "TestMethod1", "action", "discovered");
        var update = new TestNodeUpdate(testNode, "parent");
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = new[] { update } });

        // Server signals completion by sending null changes
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = (TestNodeUpdate[]?)null });

        await listener.WaitCompletionAsync();

        receivedUpdates.Count.Should().Be(1);
        receivedUpdates[0].Node.Uid.Should().Be("uid-1");
        receivedUpdates[0].Node.DisplayName.Should().Be("TestMethod1");
        receivedUpdates[0].Node.ExecutionState.Should().Be("discovered");
    }

    [Fact]
    public async Task RunTestsAsync_ShouldInvokeRunTestsRpcMethod()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        List<TestNodeUpdate> receivedUpdates = [];

        var listener = await client.RunTestsAsync(requestId, updates =>
        {
            receivedUpdates.AddRange(updates);
            return Task.CompletedTask;
        });

        listener.Should().NotBeNull();
        server.LastRunTestsRunId.Should().Be(requestId);
    }

    [Fact]
    public async Task RunTestsAsync_ShouldPassTestNodesToServer()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        var testNodes = new[] { new TestNode("uid-1", "Test1", "action", "discovered") };

        var listener = await client.RunTestsAsync(requestId, _ => Task.CompletedTask, testNodes);

        listener.Should().NotBeNull();
        server.LastRunTestCases.Should().NotBeNull();
        server.LastRunTestCases!.Length.Should().Be(1);
        server.LastRunTestCases[0].Uid.Should().Be("uid-1");
    }

    [Fact]
    public async Task RunTestsAsync_ShouldReceiveTestResultsFromServer()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        List<TestNodeUpdate> receivedUpdates = [];

        var listener = await client.RunTestsAsync(requestId, updates =>
        {
            receivedUpdates.AddRange(updates);
            return Task.CompletedTask;
        });

        // Server sends test results
        var passedNode = new TestNode("uid-1", "Test1", "action", "passed");
        var failedNode = new TestNode("uid-2", "Test2", "action", "failed");
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = new[] { new TestNodeUpdate(passedNode, "p"), new TestNodeUpdate(failedNode, "p") } });

        // Signal completion
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = (TestNodeUpdate[]?)null });

        await listener.WaitCompletionAsync();

        receivedUpdates.Count.Should().Be(2);
        receivedUpdates.Should().Contain(u => u.Node.ExecutionState == "passed");
        receivedUpdates.Should().Contain(u => u.Node.ExecutionState == "failed");
    }

    [Fact]
    public async Task RunTestsAsync_ShouldPassNullTestCases_WhenNoFilterProvided()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();

        await client.RunTestsAsync(requestId, _ => Task.CompletedTask, testNodes: null);

        server.LastRunTestCases.Should().BeNull();
    }

    [Fact]
    public async Task TestsUpdate_ShouldCompleteListener_WhenNullChangesReceived()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        var callbackInvoked = false;

        var listener = await client.DiscoverTestsAsync(requestId, _ =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        // Send null changes to signal completion
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = (TestNodeUpdate[]?)null });

        var completed = await listener.WaitCompletionAsync(TimeSpan.FromSeconds(5));

        completed.Should().BeTrue();
        callbackInvoked.Should().BeFalse("Callback should not be invoked for null changes");
    }

    [Fact]
    public async Task TestsUpdate_ShouldIgnoreUpdatesForUnknownRunId()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        var unknownRunId = Guid.NewGuid();
        var callbackInvoked = false;

        await client.DiscoverTestsAsync(requestId, _ =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        // Send update for an unknown runId
        var testNode = new TestNode("uid-1", "Test1", "action", "discovered");
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = unknownRunId, changes = new[] { new TestNodeUpdate(testNode, "p") } });

        // Give it a moment to process
        await Task.Delay(100);

        callbackInvoked.Should().BeFalse("Callback should not be invoked for unknown runId");
    }

    [Fact]
    public async Task TestsUpdate_ShouldDeliverMultipleBatchesOfUpdates()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        List<TestNodeUpdate> receivedUpdates = [];

        var listener = await client.RunTestsAsync(requestId, updates =>
        {
            receivedUpdates.AddRange(updates);
            return Task.CompletedTask;
        });

        // Send batch 1
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = new[] { new TestNodeUpdate(new TestNode("uid-1", "Test1", "action", "passed"), "p") } });

        // Send batch 2
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = new[] { new TestNodeUpdate(new TestNode("uid-2", "Test2", "action", "failed"), "p") } });

        // Signal completion
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = (TestNodeUpdate[]?)null });

        await listener.WaitCompletionAsync();

        receivedUpdates.Count.Should().Be(2);
        receivedUpdates[0].Node.Uid.Should().Be("uid-1");
        receivedUpdates[1].Node.Uid.Should().Be("uid-2");
    }

    [Fact]
    public async Task TestsUpdate_ShouldRemoveListenerAfterCompletion()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();

        var requestId = Guid.NewGuid();
        var callCount = 0;

        var listener = await client.DiscoverTestsAsync(requestId, _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        // Signal completion
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = (TestNodeUpdate[]?)null });

        await listener.WaitCompletionAsync();

        // Send another update with the same runId after completion
        var testNode = new TestNode("uid-1", "Test1", "action", "discovered");
        await connection.ServerRpc.InvokeWithParameterObjectAsync(
            "testing/testUpdates/tests",
            new { runId = requestId, changes = new[] { new TestNodeUpdate(testNode, "p") } });

        await Task.Delay(100);

        callCount.Should().Be(0, "Listener should have been removed after completion, no further callbacks");
    }

    [Fact]
    public async Task LogCallback_ShouldDeliverLogsToRegisteredListeners()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();
        var logCollector = new LogsCollector();
        client.RegisterLogListener(logCollector);

        await connection.ServerRpc.InvokeAsync("client/log", "Warning", "something went wrong");

        await Task.Delay(100);

        logCollector.Count.Should().Be(1);
        logCollector.First().Message.Should().Be("something went wrong");
    }

    [Fact]
    public async Task TelemetryCallback_ShouldDeliverTelemetryToRegisteredListeners()
    {
        using var connection = RpcTestConnection.Create();

        var server = new FakeTestServer();
        connection.ServerRpc.AddLocalRpcTarget(server);
        connection.ServerRpc.StartListening();

        using var client = connection.CreateClient();
        var telemetryCollector = new TelemetryCollector();
        client.RegisterTelemetryListener(telemetryCollector);

        var payload = new TelemetryPayload("test.event", new Dictionary<string, object>(StringComparer.Ordinal) { ["key"] = "value" });
        await connection.ServerRpc.InvokeWithParameterObjectAsync("telemetry/update", payload);

        await Task.Delay(100);

        telemetryCollector.Count.Should().Be(1);
        telemetryCollector.First().EventName.Should().Be("test.event");
    }

    /// <summary>
    /// Creates a bidirectional JSON-RPC connection using duplex pipes, with a "server" side and a "client" side.
    /// </summary>
    private sealed class RpcTestConnection : IDisposable
    {
        private readonly Stream _clientStream;
        private readonly TcpClient _dummyTcpClient = new();
        private readonly Mock<IProcessHandle> _processHandle = new();

        private RpcTestConnection(JsonRpc serverRpc, Stream clientStream)
        {
            ServerRpc = serverRpc;
            _clientStream = clientStream;
        }

        public JsonRpc ServerRpc { get; }

        public static RpcTestConnection Create()
        {
            var (serverStream, clientStream) = FullDuplexStream.CreatePair();

            var serverHandler = new HeaderDelimitedMessageHandler(
                serverStream, serverStream, new SystemTextJsonFormatter());

            var serverRpc = new JsonRpc(serverHandler);

            return new RpcTestConnection(serverRpc, clientStream);
        }

        public TestingPlatformClient CreateClient()
        {
            var clientHandler = new HeaderDelimitedMessageHandler(
                _clientStream, _clientStream, new SystemTextJsonFormatter());

            var clientRpc = new JsonRpc(clientHandler);
            return new TestingPlatformClient(clientRpc, _dummyTcpClient, _processHandle.Object, NullLogger.Instance);
        }

        public void Dispose()
        {
            ServerRpc.Dispose();
            _dummyTcpClient.Dispose();
        }
    }

    /// <summary>
    /// Simulates a Microsoft Testing Platform server that handles JSON-RPC requests.
    /// </summary>
    private sealed class FakeTestServer
    {
        public bool ExitCalled { get; private set; }

        public Guid LastDiscoveryRunId { get; private set; }

        public Guid LastRunTestsRunId { get; private set; }

        public TestNode[]? LastRunTestCases { get; private set; }

        [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "JSON-RPC reflection requires this to be an instance method.")]
        public InitializeResponse Initialize(InitializeRequest request)
        {
            _ = request;
            return new InitializeResponse(
                new ServerInfo("fake-server", "1.0.0"),
                new ServerCapabilities(new ServerTestingCapabilities(
                    SupportsDiscovery: true,
                    MultiRequestSupport: false,
                    VSTestProvider: false)));
        }

        [JsonRpcMethod("exit", UseSingleObjectParameterDeserialization = true)]
        public void Exit(object _)
        {
            ExitCalled = true;
        }

        [JsonRpcMethod("testing/discoverTests", UseSingleObjectParameterDeserialization = true)]
        public void DiscoverTests(DiscoveryRequest request)
        {
            LastDiscoveryRunId = request.RunId;
        }

        [JsonRpcMethod("testing/runTests", UseSingleObjectParameterDeserialization = true)]
        public void RunTests(RunTestsRequest request)
        {
            LastRunTestsRunId = request.RunId;
            LastRunTestCases = request.TestCases;
        }
    }

    private sealed class TestableJsonRpc : JsonRpc
    {
        public TestableJsonRpc(IJsonRpcMessageHandler messageHandler) : base(messageHandler)
        {
        }

        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TestableTcpClient : TcpClient
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
