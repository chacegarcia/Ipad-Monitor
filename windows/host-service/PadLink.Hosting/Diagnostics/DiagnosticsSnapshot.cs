namespace PadLink.Hosting.Diagnostics;

/// <summary>
/// Observable state for the WPF diagnostics panel. Thread-safe via lock for UI polling.
/// </summary>
public sealed class DiagnosticsSnapshot
{
    private readonly object _gate = new();

    public string TransportName { get; private set; } = "None";

    public string ResolutionText { get; private set; } = "—";

    public double Fps { get; private set; }

    public double EncodeLatencyMs { get; private set; }

    public double DecodeLatencyMs { get; private set; }

    public double RoundTripInputMs { get; private set; }

    public string LastError { get; private set; } = "";

    public void UpdateTransport(string name)
    {
        lock (_gate) TransportName = name;
    }

    public void UpdateMode(uint width, uint height)
    {
        lock (_gate) ResolutionText = $"{width} × {height}";
    }

    public void UpdatePerformance(double fps, double encodeMs, double decodeMs, double rttMs)
    {
        lock (_gate)
        {
            Fps = fps;
            EncodeLatencyMs = encodeMs;
            DecodeLatencyMs = decodeMs;
            RoundTripInputMs = rttMs;
        }
    }

    public void SetError(string? message)
    {
        lock (_gate) LastError = message ?? "";
    }

    public (string Transport, string Resolution, double Fps, double Enc, double Dec, double Rtt, string Err) Read()
    {
        lock (_gate)
        {
            return (TransportName, ResolutionText, Fps, EncodeLatencyMs, DecodeLatencyMs, RoundTripInputMs, LastError);
        }
    }
}
