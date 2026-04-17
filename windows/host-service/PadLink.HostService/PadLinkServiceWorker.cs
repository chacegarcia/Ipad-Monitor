using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PadLink.Hosting;

namespace PadLink.HostService;

/// <summary>
/// Headless host entry — same coordinator as the desktop app without WPF.
/// TODO(PadLink): run as Windows Service + ACL hardening.
/// </summary>
public sealed class PadLinkServiceWorker : BackgroundService
{
    private readonly ILogger<PadLinkServiceWorker> _logger;
    private readonly PadLinkHostCoordinator _coordinator;

    public PadLinkServiceWorker(ILogger<PadLinkServiceWorker> logger, PadLinkHostCoordinator coordinator)
    {
        _logger = logger;
        _coordinator = coordinator;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PadLink host service starting (TCP fake frames).");
        _coordinator.Start();
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _coordinator.StopAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
