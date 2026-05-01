using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 33 (v2.20.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/AssemblyTestServerTests.cs.
/// Framework conversion: MSTest [TestInitialize] → xUnit ctor; MSTest →
/// xUnit; Shouldly → FluentAssertions. Should.ThrowAsync/NotThrowAsync →
/// FluentAssertions Func&lt;Task&gt;.Should().Throw/NotThrowAsync.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "1:1 upstream port; .GetAwaiter().GetResult() on mocked synchronous tasks is safe.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "MA0004:Use Task.ConfigureAwait(false)", Justification = "Test methods need xUnit's synchronization context (xUnit1030 forbids ConfigureAwait(false) in tests).")]
public class AssemblyTestServerTests
{
    private const string TestAssembly = "/test/path/assembly.dll";
    private const string TestRunnerId = "test-runner-1";

    private readonly Dictionary<string, string?> _envVars = new(StringComparer.Ordinal) { ["MY_VAR"] = "value" };
    private readonly Mock<ITestServerConnectionFactory> _factoryMock;
    private readonly Mock<ITestServerListener> _listenerMock;
    private readonly Mock<ITestServerProcess> _processMock;
    private readonly Mock<ITestingPlatformClient> _clientMock;
    private readonly Mock<IProcessHandle> _processHandleMock;

    public AssemblyTestServerTests()
    {
        _factoryMock = new Mock<ITestServerConnectionFactory>();
        _listenerMock = new Mock<ITestServerListener>();
        _processMock = new Mock<ITestServerProcess>();
        _clientMock = new Mock<ITestingPlatformClient>();
        _processHandleMock = new Mock<IProcessHandle>();

        _processMock.SetupGet(p => p.ProcessHandle).Returns(_processHandleMock.Object);
        _processMock.SetupGet(p => p.HasExited).Returns(false);
    }

    private AssemblyTestServer CreateServer() =>
        new(TestAssembly, _envVars, NullLogger.Instance, TestRunnerId, connectionFactory: _factoryMock.Object);

    private void SetupSuccessfulConnection(int port = 12345)
    {
        var stream = new MemoryStream();
        var connection = Mock.Of<IDisposable>();

        _factoryMock.Setup(f => f.CreateListener()).Returns((_listenerMock.Object, port));
        _factoryMock.Setup(f => f.StartProcess(TestAssembly, port, _envVars)).Returns(_processMock.Object);
        _factoryMock.Setup(f => f.CreateClient(stream, _processHandleMock.Object, It.IsAny<Microsoft.Extensions.Logging.ILogger>(), null)).Returns(_clientMock.Object);

        _listenerMock.Setup(l => l.AcceptConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, connection));

        _processMock.Setup(p => p.WaitForExitAsync()).Returns(new TaskCompletionSource().Task);

        _clientMock.Setup(c => c.InitializeAsync()).ReturnsAsync((InitializeResponse)null!);
    }

    [Fact]
    public void Constructor_ShouldSetIsInitializedToFalse()
    {
        using var server = CreateServer();
        server.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldCreateListenerAndStartProcess()
    {
        SetupSuccessfulConnection(port: 9876);

        using var server = CreateServer();
        var result = await server.StartAsync();

        result.Should().BeTrue();
        _factoryMock.Verify(f => f.CreateListener(), Times.Once);
        _factoryMock.Verify(f => f.StartProcess(TestAssembly, 9876, _envVars), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldPassCorrectPortFromListenerToProcess()
    {
        const int ExpectedPort = 55555;
        SetupSuccessfulConnection(port: ExpectedPort);

        using var server = CreateServer();
        await server.StartAsync();

        _factoryMock.Verify(f => f.StartProcess(TestAssembly, ExpectedPort, _envVars), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldPassEnvironmentVariablesToProcess()
    {
        SetupSuccessfulConnection();

        using var server = CreateServer();
        await server.StartAsync();

        _factoryMock.Verify(f => f.StartProcess(TestAssembly, It.IsAny<int>(), _envVars), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldCreateClientWithStreamAndProcessHandle()
    {
        var stream = new MemoryStream();
        var connection = Mock.Of<IDisposable>();
        const int Port = 12345;

        _factoryMock.Setup(f => f.CreateListener()).Returns((_listenerMock.Object, Port));
        _factoryMock.Setup(f => f.StartProcess(TestAssembly, Port, _envVars)).Returns(_processMock.Object);
        _listenerMock.Setup(l => l.AcceptConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, connection));
        _processMock.Setup(p => p.WaitForExitAsync()).Returns(new TaskCompletionSource().Task);
        _factoryMock.Setup(f => f.CreateClient(stream, _processHandleMock.Object, It.IsAny<Microsoft.Extensions.Logging.ILogger>(), null)).Returns(_clientMock.Object);
        _clientMock.Setup(c => c.InitializeAsync()).ReturnsAsync((InitializeResponse)null!);

        using var server = CreateServer();
        await server.StartAsync();

        _factoryMock.Verify(f => f.CreateClient(stream, _processHandleMock.Object, It.IsAny<Microsoft.Extensions.Logging.ILogger>(), null), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldInitializeTheClient()
    {
        SetupSuccessfulConnection();

        using var server = CreateServer();
        await server.StartAsync();

        _clientMock.Verify(c => c.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsInitializedToTrue()
    {
        SetupSuccessfulConnection();

        using var server = CreateServer();
        await server.StartAsync();

        server.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue_WhenAlreadyInitialized()
    {
        SetupSuccessfulConnection();
        using var server = CreateServer();
        await server.StartAsync();

        var secondResult = await server.StartAsync();

        secondResult.Should().BeTrue();
        _factoryMock.Verify(f => f.CreateListener(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnFalse_WhenProcessExitsPrematurely()
    {
        const int Port = 12345;
        _factoryMock.Setup(f => f.CreateListener()).Returns((_listenerMock.Object, Port));
        _factoryMock.Setup(f => f.StartProcess(TestAssembly, Port, _envVars)).Returns(_processMock.Object);

        _processMock.Setup(p => p.WaitForExitAsync()).Returns(Task.CompletedTask);
        _processMock.SetupGet(p => p.HasExited).Returns(true);

        _listenerMock.Setup(l => l.AcceptConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<(Stream, IDisposable)>().Task);

        using var server = CreateServer();
        var result = await server.StartAsync();

        result.Should().BeFalse();
        server.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldReturnFalse_WhenExceptionIsThrown()
    {
        _factoryMock.Setup(f => f.CreateListener()).Throws(new InvalidOperationException("boom"));

        using var server = CreateServer();
        var result = await server.StartAsync();

        result.Should().BeFalse();
        server.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldCleanUpResources_WhenProcessExitsPrematurely()
    {
        const int Port = 12345;
        _factoryMock.Setup(f => f.CreateListener()).Returns((_listenerMock.Object, Port));
        _factoryMock.Setup(f => f.StartProcess(TestAssembly, Port, _envVars)).Returns(_processMock.Object);
        _processMock.Setup(p => p.WaitForExitAsync()).Returns(Task.CompletedTask);
        _processMock.SetupGet(p => p.HasExited).Returns(true);
        _listenerMock.Setup(l => l.AcceptConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<(Stream, IDisposable)>().Task);

        using var server = CreateServer();
        await server.StartAsync();

        _listenerMock.Verify(l => l.Stop(), Times.Once);
        _processMock.Verify(p => p.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldThrow_WhenNotInitialized()
    {
        using var server = CreateServer();

        Func<Task> act = async () => await server.DiscoverTestsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldCallClientDiscoverTests()
    {
        SetupSuccessfulConnection();

        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        listener.Complete();

        _clientMock.Setup(c => c.DiscoverTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), true))
            .ReturnsAsync(listener);

        using var server = CreateServer();
        await server.StartAsync();
        var result = await server.DiscoverTestsAsync();

        result.Should().NotBeNull();
        _clientMock.Verify(c => c.DiscoverTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), true), Times.Once);
    }

    [Fact]
    public async Task DiscoverTestsAsync_ShouldReturnOnlyDiscoveredNodes()
    {
        SetupSuccessfulConnection();

        var discoveredNode = new TestNode("uid-1", "Test1", "action", "discovered");
        var passedNode = new TestNode("uid-2", "Test2", "action", "passed");
        var updates = new[]
        {
            new TestNodeUpdate(discoveredNode, "parent"),
            new TestNodeUpdate(passedNode, "parent"),
        };

        _clientMock.Setup(c => c.DiscoverTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), true))
            .Returns<Guid, Func<TestNodeUpdate[], Task>, bool>(async (id, callback, _) =>
            {
                await callback(updates);
                var listener = new TestNodeUpdatesResponseListener(id, _ => Task.CompletedTask);
                listener.Complete();
                return listener;
            });

        using var server = CreateServer();
        await server.StartAsync();
        var result = await server.DiscoverTestsAsync();

        result.Count.Should().Be(1);
        result[0].Uid.Should().Be("uid-1");
        result[0].DisplayName.Should().Be("Test1");
    }

    [Fact]
    public async Task RunTestsAsync_ShouldThrow_WhenNotInitialized()
    {
        using var server = CreateServer();

        Func<Task> act = async () => await server.RunTestsAsync(null);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RunTestsAsync_ShouldCallClientRunTests()
    {
        SetupSuccessfulConnection();

        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        listener.Complete();

        _clientMock.Setup(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), null))
            .ReturnsAsync(listener);

        using var server = CreateServer();
        await server.StartAsync();
        var result = await server.RunTestsAsync(null);

        result.Should().NotBeNull();
        _clientMock.Verify(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), null), Times.Once);
    }

    [Fact]
    public async Task RunTestsAsync_ShouldPassTestNodesToClient()
    {
        SetupSuccessfulConnection();

        var testNodes = new[] { new TestNode("uid-1", "Test1", "action", "discovered") };
        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        listener.Complete();

        _clientMock.Setup(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), testNodes))
            .ReturnsAsync(listener);

        using var server = CreateServer();
        await server.StartAsync();
        await server.RunTestsAsync(testNodes);

        _clientMock.Verify(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), testNodes), Times.Once);
    }

    [Fact]
    public async Task RunTestsAsync_ShouldCollectTestResults()
    {
        SetupSuccessfulConnection();

        var passedNode = new TestNode("uid-1", "Test1", "action", "passed");
        var failedNode = new TestNode("uid-2", "Test2", "action", "failed");
        var updates = new[]
        {
            new TestNodeUpdate(passedNode, "parent"),
            new TestNodeUpdate(failedNode, "parent"),
        };

        _clientMock.Setup(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), null))
            .Returns<Guid, Func<TestNodeUpdate[], Task>, TestNode[]?>(async (id, callback, _) =>
            {
                await callback(updates);
                var listener = new TestNodeUpdatesResponseListener(id, _ => Task.CompletedTask);
                listener.Complete();
                return listener;
            });

        using var server = CreateServer();
        await server.StartAsync();
        var result = await server.RunTestsAsync(null);

        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task RunTestsAsync_WithTimeout_ShouldReturnTimedOutFalse_WhenCompletesInTime()
    {
        SetupSuccessfulConnection();

        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        listener.Complete();

        _clientMock.Setup(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), null))
            .ReturnsAsync(listener);

        using var server = CreateServer();
        await server.StartAsync();
        var (_, timedOut) = await server.RunTestsAsync(null, TimeSpan.FromSeconds(10));

        timedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunTestsAsync_WithTimeout_ShouldReturnTimedOutTrue_WhenTimesOut()
    {
        SetupSuccessfulConnection();

        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);

        _clientMock.Setup(c => c.RunTestsAsync(It.IsAny<Guid>(), It.IsAny<Func<TestNodeUpdate[], Task>>(), null))
            .ReturnsAsync(listener);

        using var server = CreateServer();
        await server.StartAsync();
        var (_, timedOut) = await server.RunTestsAsync(null, TimeSpan.FromMilliseconds(50));

        timedOut.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_ShouldDisposeResources()
    {
        SetupSuccessfulConnection();

        _clientMock.Setup(c => c.ExitAsync(true)).Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.WaitServerProcessExitAsync()).ReturnsAsync(0);

        using var server = CreateServer();
        await server.StartAsync();
        await server.StopAsync();

        _clientMock.Verify(c => c.ExitAsync(true), Times.Once);
        _clientMock.Verify(c => c.WaitServerProcessExitAsync(), Times.Once);
        _listenerMock.Verify(l => l.Stop(), Times.Once);
        _listenerMock.Verify(l => l.Dispose(), Times.Once);
        _clientMock.Verify(c => c.Dispose(), Times.Once);
        _processMock.Verify(p => p.Dispose(), Times.Once);
        server.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow_WhenClientExitFails()
    {
        SetupSuccessfulConnection();

        _clientMock.Setup(c => c.ExitAsync(true)).ThrowsAsync(new InvalidOperationException("exit failed"));

        using var server = CreateServer();
        await server.StartAsync();

        Func<Task> act = async () => await server.StopAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow_WhenNotInitialized()
    {
        using var server = CreateServer();

        Func<Task> act = async () => await server.StopAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RestartAsync_ShouldStopAndStartAgain()
    {
        var stream1 = new MemoryStream();
        var connection1 = Mock.Of<IDisposable>();
        const int Port1 = 11111;

        _factoryMock.Setup(f => f.CreateListener()).Returns((_listenerMock.Object, Port1));
        _factoryMock.Setup(f => f.StartProcess(TestAssembly, Port1, _envVars)).Returns(_processMock.Object);
        _factoryMock.Setup(f => f.CreateClient(stream1, _processHandleMock.Object, It.IsAny<Microsoft.Extensions.Logging.ILogger>(), null)).Returns(_clientMock.Object);
        _listenerMock.Setup(l => l.AcceptConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream1, connection1));
        _processMock.Setup(p => p.WaitForExitAsync()).Returns(new TaskCompletionSource().Task);
        _clientMock.Setup(c => c.InitializeAsync()).ReturnsAsync((InitializeResponse)null!);
        _clientMock.Setup(c => c.ExitAsync(true)).Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.WaitServerProcessExitAsync()).ReturnsAsync(0);

        using var server = CreateServer();
        await server.StartAsync();
        server.IsInitialized.Should().BeTrue();

        await server.RestartAsync();

        _clientMock.Verify(c => c.ExitAsync(true), Times.Once);
        _factoryMock.Verify(f => f.CreateListener(), Times.Exactly(2));
    }

    [Fact]
    public void Dispose_ShouldCallStopAsync()
    {
        SetupSuccessfulConnection();

        _clientMock.Setup(c => c.ExitAsync(true)).Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.WaitServerProcessExitAsync()).ReturnsAsync(0);

        var server = CreateServer();
        server.StartAsync().GetAwaiter().GetResult();

        server.Dispose();

        _clientMock.Verify(c => c.ExitAsync(true), Times.Once);
        _clientMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        SetupSuccessfulConnection();

        _clientMock.Setup(c => c.ExitAsync(true)).Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.WaitServerProcessExitAsync()).ReturnsAsync(0);

        var server = CreateServer();
        server.StartAsync().GetAwaiter().GetResult();

        server.Dispose();
        server.Dispose();

        _clientMock.Verify(c => c.ExitAsync(true), Times.Once);
    }
}
