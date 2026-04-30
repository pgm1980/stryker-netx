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
    public bool HasConnectedClients => _writers.Count > 0;

    private readonly HttpListener _listener;
    private readonly List<StreamWriter> _writers;
    private bool _disposed;

    public SseServer()
    {
        Port = FreeTcpPort();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _writers = [];
    }

    public int ConnectedClients => _writers.Count;

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
            foreach (var writer in _writers)
            {
                writer.Dispose();
            }
            _writers.Clear();
        }
        _disposed = true;
    }

    private async Task ListenForConnectionsAsync()
    {
        while (_listener.IsListening)
        {
            var context = await _listener.GetContextAsync().ConfigureAwait(false);
            var response = context.Response;
            response.ContentType = "text/event-stream";
            // The file:// protocols needs this, since we can't add a file location as an allowed origin.
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var writer = new StreamWriter(response.OutputStream);
            _writers.Add(writer);
            ClientConnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SendEvent<T>(SseEvent<T> @event)
    {
        var serialized = @event.Serialize();
        var lostClients = new List<StreamWriter>();
        foreach (var writer in _writers)
        {
            try
            {
                writer.Write($"{serialized}{Environment.NewLine}{Environment.NewLine}");
                writer.Flush();
            }
            catch (HttpListenerException)
            {
                // The client disconnected
                lostClients.Add(writer);
            }
        }
        foreach (var lostClient in lostClients)
        {
            _writers.Remove(lostClient);
            lostClient.Dispose();
        }
    }

    public void CloseSseEndpoint()
    {
        Task.WaitAll([.. _writers.Select(writer => writer.BaseStream.FlushAsync())]);

        _listener.Close();
    }
}
