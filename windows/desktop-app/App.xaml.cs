using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PadLink.Hosting;
using PadLink.Hosting.Diagnostics;

namespace PadLink.DesktopApp;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureLogging(logging =>
            {
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((_, services) =>
            {
                services.Configure<PadLinkHostingOptions>(o =>
                {
                    o.TcpListenPort = PadLinkHostingOptions.DefaultPort;
                    o.FakeWidth = 512;
                    o.FakeHeight = 288;
                });
                services.AddSingleton<DiagnosticsSnapshot>();
                services.AddSingleton<PadLinkHostCoordinator>();
            })
            .Build();

        Services = _host.Services;
        _host.Start();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync();

        base.OnExit(e);
    }
}
