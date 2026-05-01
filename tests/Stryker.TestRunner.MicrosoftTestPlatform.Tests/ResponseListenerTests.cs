using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 31 (v2.18.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/ResponseListenerTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class ResponseListenerTests
{
    [Fact]
    public void Constructor_ShouldSetRequestId()
    {
        var requestId = Guid.NewGuid();

        var listener = new TestNodeUpdatesResponseListener(requestId, _ => Task.CompletedTask);

        listener.RequestId.Should().Be(requestId);
    }

    [Fact]
    public async Task OnMessageReceiveAsync_ShouldInvokeAction()
    {
        var requestId = Guid.NewGuid();
        var messageReceived = false;
        TestNodeUpdate[]? receivedUpdates = null;

        var listener = new TestNodeUpdatesResponseListener(requestId, updates =>
        {
            messageReceived = true;
            receivedUpdates = updates;
            return Task.CompletedTask;
        });

        var testNode = new TestNode("test1", "Test 1", "test", "discovered");
        var updates = new[] { new TestNodeUpdate(testNode, string.Empty) };

        await listener.OnMessageReceiveAsync(updates);

        messageReceived.Should().BeTrue();
        receivedUpdates.Should().NotBeNull();
        receivedUpdates!.Length.Should().Be(1);
        receivedUpdates[0].Node.Uid.Should().Be("test1");
    }

    [Fact]
    public async Task WaitCompletionAsync_ShouldReturnTrue_WhenCompleted()
    {
        var requestId = Guid.NewGuid();
        var listener = new TestNodeUpdatesResponseListener(requestId, _ => Task.CompletedTask);

        var completionTask = listener.WaitCompletionAsync(TimeSpan.FromSeconds(1));

        var completeMethod = typeof(ResponseListener).GetMethod("Complete",
            BindingFlags.NonPublic | BindingFlags.Instance);
        completeMethod?.Invoke(listener, null);

        var result = await completionTask;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WaitCompletionAsync_ShouldReturnFalse_WhenTimeout()
    {
        var requestId = Guid.NewGuid();
        var listener = new TestNodeUpdatesResponseListener(requestId, _ => Task.CompletedTask);

        var result = await listener.WaitCompletionAsync(TimeSpan.FromMilliseconds(10));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WaitCompletionAsync_WithoutTimeout_ShouldWaitIndefinitely()
    {
        var requestId = Guid.NewGuid();
        var listener = new TestNodeUpdatesResponseListener(requestId, _ => Task.CompletedTask);

        var completionTask = listener.WaitCompletionAsync();

        var completeMethod = typeof(ResponseListener).GetMethod("Complete",
            BindingFlags.NonPublic | BindingFlags.Instance);
        completeMethod?.Invoke(listener, null);

        var completedInTime = await Task.WhenAny(completionTask, Task.Delay(1000)) == completionTask;

        completedInTime.Should().BeTrue();
    }

    [Fact]
    public async Task WaitCompletionAsync_ShouldHandleCancellation()
    {
        var requestId = Guid.NewGuid();
        var listener = new TestNodeUpdatesResponseListener(requestId, _ => Task.CompletedTask);
        using var cts = new CancellationTokenSource();

        var completionTask = listener.WaitCompletionAsync(TimeSpan.FromSeconds(10), cts.Token);
        await cts.CancelAsync();

        var result = await completionTask;

        result.Should().BeFalse();
    }
}
