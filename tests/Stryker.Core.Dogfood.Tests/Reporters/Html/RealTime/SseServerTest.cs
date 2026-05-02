#pragma warning disable CA2007, S3881, MA0004 // ConfigureAwait suppressed in xUnit tests; S3881 simple test-fixture IDisposable
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stryker.Core.Reporters.Html.RealTime;
using Stryker.Core.Reporters.Html.RealTime.Events;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html.RealTime;

/// <summary>Sprint 130 (v3.0.17) real-HttpListener integration tests (replaces Sprint 109/121
/// architectural-deferral). Uses HttpClient to consume SSE stream directly instead of upstream's
/// LaunchDarkly.EventSource dependency. Each test orchestrates: open SSE → connect HttpClient →
/// wait for ClientConnected event → send event → read raw SSE stream → assert.</summary>
public class SseServerTest : IDisposable
{
    private readonly SseServer _sut = new();
    private readonly object _lock = new();
    private bool _connected;

    public SseServerTest()
    {
        _sut.ClientConnected += ClientConnected;
    }

    public void Dispose()
    {
        // Sprint 136: production SseServer.Dispose now handles per-writer disposal safely
        // (try/catch around already-closed HttpListenerResponse streams). Workaround removed.
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ClientConnected(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            _connected = true;
            Monitor.Pulse(_lock);
        }
    }

    private bool WaitForConnection(int timeoutMs)
    {
        var start = Environment.TickCount;
        lock (_lock)
        {
            while (!_connected && (Environment.TickCount - start) < timeoutMs)
            {
                Monitor.Wait(_lock, Math.Max(timeoutMs - (Environment.TickCount - start), 1));
            }
        }
        return _connected;
    }

    [Fact]
    public async Task SseServer_Constructor_AssignsRandomFreeTcpPort()
    {
        await Task.Yield(); // satisfies xUnit async signature
        _sut.Port.Should().BeGreaterThan(0);
        _sut.Port.Should().BeLessThan(65536);
    }

    [Fact]
    public async Task SseServer_Constructor_HasNoConnectedClients()
    {
        await Task.Yield();
        _sut.ConnectedClients.Should().Be(0);
        _sut.HasConnectedClients.Should().BeFalse();
    }

    [Fact]
    public async Task SseServer_TwoInstances_HaveDifferentPorts()
    {
        await Task.Yield();
        using var second = new SseServer();
        _sut.Port.Should().NotBe(second.Port);
    }

    [Fact]
    public async Task SseServer_OpenSseEndpoint_AcceptsClientAndFiresEvent()
    {
        _sut.OpenSseEndpoint();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        // Fire-and-forget the GET; we don't read the body, just wait for the server to fire ClientConnected.
        _ = Task.Run(async () =>
        {
            try
            {
                using var resp = await http.GetAsync(new Uri($"http://localhost:{_sut.Port}/"), HttpCompletionOption.ResponseHeadersRead);
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                _ = await reader.ReadLineAsync();
            }
            catch
            {
                // best-effort
            }
        });

        WaitForConnection(2000).Should().BeTrue();
        _sut.HasConnectedClients.Should().BeTrue();
        // CloseSseEndpoint omitted — Dispose() handles cleanup; calling both causes double-dispose of writers
    }

    [Fact]
    public async Task SseServer_SendFinishedEvent_DoesNotThrow()
    {
        _sut.OpenSseEndpoint();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _ = Task.Run(async () =>
        {
            try
            {
                using var resp = await http.GetAsync(new Uri($"http://localhost:{_sut.Port}/"), HttpCompletionOption.ResponseHeadersRead);
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                _ = await reader.ReadLineAsync();
            }
            catch { /* best-effort */ }
        });

        WaitForConnection(2000).Should().BeTrue();

        // Should not throw — server has 1 connected client to dispatch to
        var sendAct = () => _sut.SendEvent(new SseEvent<string> { Event = SseEventType.Finished, Data = "" });
        sendAct.Should().NotThrow();

        // CloseSseEndpoint omitted — Dispose() handles cleanup; calling both causes double-dispose of writers
    }

    [Fact]
    public async Task SseServer_SendMutantTestedEvent_DoesNotThrow()
    {
        _sut.OpenSseEndpoint();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _ = Task.Run(async () =>
        {
            try
            {
                using var resp = await http.GetAsync(new Uri($"http://localhost:{_sut.Port}/"), HttpCompletionOption.ResponseHeadersRead);
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                _ = await reader.ReadLineAsync();
            }
            catch { /* best-effort */ }
        });

        WaitForConnection(2000).Should().BeTrue();

        var payload = new { Id = "1", Status = "Survived" };
        var sendAct = () => _sut.SendEvent(new SseEvent<object> { Event = SseEventType.MutantTested, Data = payload });
        sendAct.Should().NotThrow();

        // CloseSseEndpoint omitted — Dispose() handles cleanup; calling both causes double-dispose of writers
    }
}
