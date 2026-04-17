using Microsoft.Extensions.Logging;

namespace PadLink.Hosting.Transport;

/// <summary>
/// USB-C byte pipe is **not** implemented. This stub exists so callers compile against the abstraction.
/// WHY incomplete: no stable OS-level “generic iPad app link” over USB on arbitrary PCs.
/// BLOCKER: pick a spike (see docs/USB_TRANSPORT.md).
/// NEXT: prototype a real duplex channel, then replace stub.
/// </summary>
public sealed class UsbTransportStub : IOutboundTransport
{
    private readonly ILogger<UsbTransportStub> _logger;

    public UsbTransportStub(ILogger<UsbTransportStub> logger) => _logger = logger;

    public string Name => "USB (stub)";

    public Task<ITransportSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogError("UsbTransport is not implemented.");
        throw new NotSupportedException(
            "USB transport is not implemented. Use Wi-Fi (TCP) for the prototype. See docs/USB_TRANSPORT.md.");
    }
}
