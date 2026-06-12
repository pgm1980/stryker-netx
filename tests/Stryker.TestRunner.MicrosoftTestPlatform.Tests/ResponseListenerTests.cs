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

    // Sprint 182 (issue #297c, 360-Grad-Analyse I-15): bei einem Server-Disconnect sammelte
    // der Client nur Text, komplettierte aber keine registrierten Listener — der
    // timeout-lose WaitCompletionAsync-Pfad wartete ewig auf das null-Changes-Signal eines
    // toten Servers. Fail muss alle Warter mit der Disconnect-Ursache aufwecken.
    [Fact]
    public async Task Fail_CompletesPendingWaitersWithTheDisconnectCause()
    {
        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        var waiter = listener.WaitCompletionAsync();

        listener.Fail(new InvalidOperationException("server connection lost"));

        var act = async () => await waiter.ConfigureAwait(false);
        (await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true)).WithMessage("*connection lost*");
    }

    [Fact]
    public void Fail_AfterComplete_IsIgnored()
    {
        var listener = new TestNodeUpdatesResponseListener(Guid.NewGuid(), _ => Task.CompletedTask);
        listener.Complete();

        var act = () => listener.Fail(new InvalidOperationException("late disconnect"));

        act.Should().NotThrow("settling a settled listener must be a no-op");
    }
}
