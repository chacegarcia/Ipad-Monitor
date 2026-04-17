using System.Buffers.Binary;

namespace PadLink.Hosting.Protocol;

/// <summary>
/// [ 'P','L','K','1' | uint32 big-endian length | payload ]
/// </summary>
public static class BinaryFraming
{
    public static ReadOnlySpan<byte> Magic => "PLK1"u8;

    public const int HeaderSize = 8;

    public static int WriteFrame(Span<byte> destination, ReadOnlySpan<byte> payload)
    {
        if (destination.Length < HeaderSize + payload.Length)
            throw new ArgumentException("Destination buffer too small.");

        Magic.CopyTo(destination);
        BinaryPrimitives.WriteUInt32BigEndian(destination[4..8], (uint)payload.Length);
        payload.CopyTo(destination[HeaderSize..]);
        return HeaderSize + payload.Length;
    }

    public static bool TryReadHeader(ReadOnlySpan<byte> header, out uint length)
    {
        length = 0;
        if (header.Length < HeaderSize)
            return false;

        if (!header[..4].SequenceEqual(Magic))
            return false;

        length = BinaryPrimitives.ReadUInt32BigEndian(header[4..8]);
        return true;
    }
}
