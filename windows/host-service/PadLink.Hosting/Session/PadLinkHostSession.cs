using System.Buffers;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using PadLink.Hosting.Diagnostics;
using PadLink.Hosting.Frames;
using PadLink.Hosting.Protocol;
using PadLink.Hosting.Transport;
using PadLink.Protocol;

namespace PadLink.Hosting.Session;

/// <summary>
/// Owns one TCP client session: handshake + fake frame pump.
/// </summary>
public sealed class PadLinkHostSession
{
    private const uint ProtocolVersion = 1;

    private readonly ILogger<PadLinkHostSession> _logger;
    private readonly DiagnosticsSnapshot _diagnostics;
    private readonly FakeFrameSource _frames;
    private readonly ITransportSession _transport;
    private readonly byte[] _frameBuffer = new byte[1024 * 1024 * 16]; // 16MB cap; RGBA test only

    public PadLinkHostSession(
        ILogger<PadLinkHostSession> logger,
        DiagnosticsSnapshot diagnostics,
        FakeFrameSource frames,
        ITransportSession transport)
    {
        _logger = logger;
        _diagnostics = diagnostics;
        _frames = frames;
        _transport = transport;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _diagnostics.UpdateTransport(_transport.Name);
        _diagnostics.UpdateMode(_frames.Width, _frames.Height);

        // NOTE: Do not dispose Duplex here; ITransportSession owns the stream lifecycle.
        var stream = _transport.Duplex;
        var buffer = new ArrayBufferWriter<byte>();

        if (!await TryReadHandshakeAsync(stream, cancellationToken).ConfigureAwait(false))
            return;

        await SendSessionAckAsync(stream, buffer, cancellationToken).ConfigureAwait(false);

        var frameCount = 0L;
        var lastFpsTick = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            var rgba = _frames.GenerateRgba(out var header);
            var bundle = new FrameBundle { Header = header, Payload = ByteString.CopyFrom(rgba) };
            var envelope = new WireEnvelope { FrameBundle = bundle };

            var written = WriteFramedWireEnvelope(buffer, envelope, _frameBuffer);
            await stream.WriteAsync(_frameBuffer.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            frameCount++;
            var now = DateTime.UtcNow;
            if ((now - lastFpsTick).TotalSeconds >= 1.0)
            {
                var fps = frameCount / (now - lastFpsTick).TotalSeconds;
                lastFpsTick = now;
                frameCount = 0;
                _diagnostics.UpdatePerformance(fps, header.HostEncodeEndUs / 1000.0, decodeMs: 0, rttMs: 0);
                _logger.LogInformation("PadLink streaming FPS ≈ {Fps:F1}", fps);
            }

            // TODO(PadLink): pace to negotiated refresh; currently yields CPU — ugly but fine for prototype.
            await Task.Delay(16, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> TryReadHandshakeAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[BinaryFraming.HeaderSize];
        await ReadExactAsync(stream, header, ct).ConfigureAwait(false);

        if (!BinaryFraming.TryReadHeader(header, out var len) || len > _frameBuffer.Length)
        {
            _logger.LogWarning("Invalid handshake length {Len}.", len);
            return false;
        }

        await ReadExactAsync(stream, _frameBuffer.AsMemory(0, (int)len), ct).ConfigureAwait(false);
        var envelope = WireEnvelope.Parser.ParseFrom(_frameBuffer.AsSpan(0, (int)len));
        if (envelope.PayloadCase != WireEnvelope.PayloadOneofCase.Hello)
        {
            _logger.LogWarning("Expected SessionHello.");
            return false;
        }

        _logger.LogInformation("SessionHello from {Client} proto v{Version}",
            envelope.Hello.ClientName, envelope.Hello.ProtocolVersion);
        return true;
    }

    private async Task SendSessionAckAsync(Stream stream, ArrayBufferWriter<byte> buffer, CancellationToken ct)
    {
        var ack = new SessionAck
        {
            Ok = true,
            SessionId = Guid.NewGuid().ToString("N"),
            ProtocolVersion = ProtocolVersion,
            SelectedCodec = VideoCodec.RawRgbaTest,
            NegotiatedMode = new DisplayMode
            {
                WidthPx = _frames.Width,
                HeightPx = _frames.Height,
                RefreshMillihertz = 60_000
            }
        };

        var envelope = new WireEnvelope { SessionAck = ack };
        var written = WriteFramedWireEnvelope(buffer, envelope, _frameBuffer);
        await stream.WriteAsync(_frameBuffer.AsMemory(0, written), ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Serializes protobuf then PLK1-framing into <paramref name="frameBuffer"/>; returns total bytes to send.</summary>
    private static int WriteFramedWireEnvelope(ArrayBufferWriter<byte> buffer, WireEnvelope envelope, byte[] frameBuffer)
    {
        buffer.Clear();
        WriteEnvelope(buffer, envelope);
        var payload = buffer.WrittenSpan;
        return BinaryFraming.WriteFrame(frameBuffer.AsSpan(0, BinaryFraming.HeaderSize + payload.Length), payload);
    }

    private static void WriteEnvelope(ArrayBufferWriter<byte> buffer, WireEnvelope envelope)
    {
        var size = envelope.CalculateSize();
        var span = buffer.GetSpan(size);
        envelope.WriteTo(span);
        buffer.Advance(size);
    }

    private static async Task ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer[total..], ct).ConfigureAwait(false);
            if (n == 0)
                throw new IOException("Remote closed.");
            total += n;
        }
    }
}
