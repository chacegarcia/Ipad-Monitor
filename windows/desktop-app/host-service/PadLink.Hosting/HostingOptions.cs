namespace PadLink.Hosting;

public sealed class PadLinkHostingOptions
{
    public const int DefaultPort = 39777;

    /// <summary>TCP port for Wi‑Fi transport vertical slice.</summary>
    public int TcpListenPort { get; set; } = DefaultPort;

    public uint FakeWidth { get; set; } = 512;

    public uint FakeHeight { get; set; } = 288;
}
