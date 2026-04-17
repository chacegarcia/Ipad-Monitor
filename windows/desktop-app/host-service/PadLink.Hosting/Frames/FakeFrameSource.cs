using System.Diagnostics;
using PadLink.Protocol;

namespace PadLink.Hosting.Frames;

/// <summary>
/// Generates a deterministic RGBA test pattern (not H.264).
/// WHY: proves capture/encode pipeline wiring before DXGI + Media Foundation land.
/// NEXT: replace with DXGI duplication + hardware encoder feeding <see cref="VideoCodec.H264"/>.
/// </summary>
public sealed class FakeFrameSource
{
    private readonly uint _width;
    private readonly uint _height;
    private ulong _frameIndex;

    public FakeFrameSource(uint width, uint height)
    {
        _width = width;
        _height = height;
    }

    public uint Width => _width;

    public uint Height => _height;

    /// <summary>RGBA8888, row-major, sRGB-ish test gradient.</summary>
    public byte[] GenerateRgba(out FrameHeader header)
    {
        var sw = Stopwatch.StartNew();

        var pixels = new byte[_width * _height * 4];
        var idx = _frameIndex;
        var idxL = (long)idx;
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var o = (int)((y * _width + x) * 4);
            pixels[o + 0] = (byte)((x + idxL) % 256);
            pixels[o + 1] = (byte)((y + idxL / 3) % 256);
            pixels[o + 2] = (byte)((x ^ y) % 256);
            pixels[o + 3] = 255;
        }

        sw.Stop();
        _frameIndex++;

        header = new FrameHeader
        {
            FrameIndex = idx,
            CaptureTimestampUnixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            WidthPx = _width,
            HeightPx = _height,
            Codec = VideoCodec.RawRgbaTest,
            HostCaptureUs = (ulong)sw.Elapsed.TotalMicroseconds,
            HostEncodeEndUs = (ulong)sw.Elapsed.TotalMicroseconds
        };

        return pixels;
    }
}
