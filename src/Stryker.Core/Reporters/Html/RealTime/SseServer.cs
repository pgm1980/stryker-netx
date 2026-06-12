using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Stryker.Core.Reporters.Html.RealTime.Events;

namespace Stryker.Core.Reporters.Html.RealTime;

public class SseServer : ISseServer, IDisposable
{
    public int Port { get; set; }
    public bool HasConnectedClients
    {
        get
        {
            lock (_writersLock)
            {
                return _writers.Count > 0;
            }
        }
    }

    private readonly HttpListener _listener;
    private readonly List<StreamWriter> _writers;
    // Sprint 183 (issue #300, J-06): the listener task adds writers while mutant threads
    // iterate them in SendEvent — unsynchronized access corrupted the list or threw
    // "Collection was modified". Every _writers access goes through this lock.
    private readonly System.Threading.Lock _writersLock = new();
    private bool _disposed;

    public SseServer()
    {
        Port = FreeTcpPort();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _writers = [];
    }

    public int ConnectedClients
    {
        get
        {
            lock (_writersLock)
            {
                return _writers.Count;
            }
        }
    }

    public event EventHandler<EventArgs>? ClientConnected;

    private static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void OpenSseEndpoint()
    {
        _listener.Start();
        _ = Task.Run(ListenForConnectionsAsync);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            if (_listener.IsListening)
            {
                _listener.Close();
            }
            ((IDisposable)_listener).Dispose();
            lock (_writersLock)
            {
                foreach (var writer in _writers)
                {
                    // Sprint 136 fix: best-effort disposal — if the underlying HttpListener response
                    // stream was already closed (client disconnected mid-stream), the StreamWriter's
                    // Dispose() throws ObjectDisposedException or HttpListenerException. We're tearing
                    // down anyway; failure to flush a writer whose stream is already gone is recoverable.
                    try
                    {
                        writer.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // already cleaned up by client disconnect
                    }
                    catch (HttpListenerException)
                    {
                        // underlying socket already closed
                    }
                }
                _writers.Clear();
            }
        }
        _disposed = true;
    }

    private async Task ListenForConnectionsAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                // Sprint 183 (issue #300, J-06): the listener gets closed during shutdown while
                // this loop awaits the next connection — that race used to die as an unobserved
                // task fault and silently ended realtime reporting. It is the normal loop end.
                return;
            }

            var response = context.Response;
            response.ContentType = "text/event-stream";
            // The file:// protocols needs this, since we can't add a file location as an allowed origin.
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var writer = new StreamWriter(response.OutputStream);
            lock (_writersLock)
            {
                _writers.Add(writer);
            }

            ClientConnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SendEvent<T>(SseEvent<T> @event)
    {
        var serialized = @event.Serialize();
        StreamWriter[] snapshot;
        lock (_writersLock)
        {
            snapshot = [.. _writers];
        }

        var lostClients = new List<StreamWriter>();
        foreach (var writer in snapshot)
        {
            try
            {
                writer.Write($"{serialized}{Environment.NewLine}{Environment.NewLine}");
                writer.Flush();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or IOException)
            {
                // The client disconnected
                lostClients.Add(writer);
            }
        }

        if (lostClients.Count == 0)
        {
            return;
        }

        lock (_writersLock)
        {
            foreach (var lostClient in lostClients)
            {
                _writers.Remove(lostClient);
                lostClient.Dispose();
            }
        }
    }

    public void CloseSseEndpoint()
    {
        // Sprint 183 (issue #300, J-06): the previous Task.WaitAll over all writer flushes
        // threw an AggregateException for any client that disconnected meanwhile — and that
        // exception broke the BroadcastReporter chain, silently skipping the reporters that
        // run AFTER the realtime one (Json, Baseline). Flush best-effort per writer instead,
        // mirroring the Dispose path.
        StreamWriter[] snapshot;
        lock (_writersLock)
        {
            snapshot = [.. _writers];
        }

        foreach (var writer in snapshot)
        {
            try
            {
                writer.Flush();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or IOException)
            {
                // the client is gone — nothing left to flush
            }
        }

        _listener.Close();
    }
}
