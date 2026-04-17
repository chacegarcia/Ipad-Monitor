using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PadLink.Hosting.Diagnostics;
using PadLink.Hosting.Frames;
using PadLink.Hosting.Session;
using PadLink.Hosting.Transport;

namespace PadLink.Hosting;

/// <summary>
/// Starts the TCP listener and pumps a single session (prototype: one iPad only).
/// </summary>
public sealed class PadLinkHostCoordinator : IAsyncDisposable
{
    private readonly ILogger<PadLinkHostCoordinator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DiagnosticsSnapshot _diagnostics;
    private readonly PadLinkHostingOptions _options;
    private CancellationTokenSource? _runCts;
    private Task? _loop;

    public PadLinkHostCoordinator(
        ILogger<PadLinkHostCoordinator> logger,
        ILoggerFactory loggerFactory,
        DiagnosticsSnapshot diagnostics,
        IOptions<PadLinkHostingOptions> options)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _diagnostics = diagnostics;
        _options = options.Value;
    }

    public bool IsRunning => _loop is { IsCompleted: false };

    public void Start()
    {
        if (_loop is { IsCompleted: false })
            return;

        _runCts = new CancellationTokenSource();
        var token = _runCts.Token;
        _loop = Task.Run(() => RunAsync(token), token);
    }

    public async Task StopAsync()
    {
        if (_runCts is null)
            return;

        try
        {
            _runCts.Cancel();
            if (_loop is not null)
                await _loop.ConfigureAwait(false);
        }
        finally
        {
            _runCts.Dispose();
            _runCts = null;
            _loop = null;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var tcpLogger = _loggerFactory.CreateLogger<WifiTcpHostTransport>();
        var sessionLogger = _loggerFactory.CreateLogger<PadLinkHostSession>();
        await using var transport = new WifiTcpHostTransport(_options.TcpListenPort, tcpLogger);
        transport.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var session = await transport.AcceptSingleClientAsync(cancellationToken).ConfigureAwait(false);
                var frames = new FakeFrameSource(_options.FakeWidth, _options.FakeHeight);
                var hostSession = new PadLinkHostSession(sessionLogger, _diagnostics, frames, session);
                await hostSession.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _diagnostics.SetError(ex.Message);
                _logger.LogError(ex, "PadLink session failed.");
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
