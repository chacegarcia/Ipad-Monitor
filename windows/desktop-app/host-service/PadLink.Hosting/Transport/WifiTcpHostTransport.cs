using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace PadLink.Hosting.Transport;

/// <summary>
/// Listens on TCP; accepts **one** concurrent client (prototype policy).
/// </summary>
public sealed class WifiTcpHostTransport : IInboundTransport, IAsyncDisposable
{
    private readonly int _port;
    private readonly ILogger<WifiTcpHostTransport> _logger;
    private TcpListener? _listener;

    public WifiTcpHostTransport(int port, ILogger<WifiTcpHostTransport> logger)
    {
        _port = port;
        _logger = logger;
    }

    public string Name => "Wi-Fi (TCP)";

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _logger.LogInformation("PadLink TCP host listening on {Endpoint}", _listener.LocalEndpoint);
    }

    public async Task<ITransportSession> AcceptSingleClientAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is null)
            throw new InvalidOperationException("Call Start() before Accept.");

        _logger.LogInformation("Waiting for a single PadLink client…");
        var socket = await _listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Client connected from {Remote}", socket.RemoteEndPoint);
        return new TcpSession(socket);
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is not null)
        {
            _listener.Stop();
            _listener = null;
        }

        await Task.CompletedTask;
    }

    private sealed class TcpSession : ITransportSession
    {
        private readonly Socket _socket;

        public TcpSession(Socket socket)
        {
            _socket = socket;
            Duplex = new NetworkStream(socket, ownsSocket: true);
        }

        public string Name => "Wi-Fi (TCP)";

        public Stream Duplex { get; }

        public async ValueTask DisposeAsync()
        {
            await Duplex.DisposeAsync().ConfigureAwait(false);
        }
    }
}
