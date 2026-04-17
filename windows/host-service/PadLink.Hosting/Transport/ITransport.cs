namespace PadLink.Hosting.Transport;

/// <summary>
/// Duplex byte stream after a connection is established (client or accepted server socket).
/// WHY: isolates socket/USB details from session logic.
/// </summary>
public interface ITransportSession : IAsyncDisposable
{
    string Name { get; }

    Stream Duplex { get; }
}

/// <summary>
/// Outbound/client transport (e.g., iPad-side in future shared code; on Windows used for tests).
/// </summary>
public interface IOutboundTransport
{
    string Name { get; }

    Task<ITransportSession> ConnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Inbound listener for Windows host (Wi‑Fi TCP first).
/// </summary>
public interface IInboundTransport
{
    string Name { get; }

    Task<ITransportSession> AcceptSingleClientAsync(CancellationToken cancellationToken = default);
}
