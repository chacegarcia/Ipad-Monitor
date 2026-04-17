using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PadLink.Hosting;
using PadLink.Hosting.Diagnostics;

namespace PadLink.DesktopApp;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private PadLinkHostCoordinator? _coordinator;
    private DiagnosticsSnapshot? _diagnostics;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => RefreshDiagnostics();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _diagnostics = App.Services.GetRequiredService<DiagnosticsSnapshot>();
        _coordinator = App.Services.GetRequiredService<PadLinkHostCoordinator>();
        _coordinator.Start();
        _timer.Start();
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _timer.Stop();
        if (_coordinator is not null)
            await _coordinator.StopAsync();
    }

    private void RefreshDiagnostics()
    {
        if (_diagnostics is null)
            return;

        var (transport, resolution, fps, enc, dec, rtt, err) = _diagnostics.Read();
        TransportText.Text = transport;
        ResolutionText.Text = resolution;
        FpsText.Text = fps.ToString("F1");
        EncodeText.Text = enc.ToString("F2");
        DecodeText.Text = dec.ToString("F2");
        RttText.Text = rtt.ToString("F2");
        ErrorText.Text = string.IsNullOrWhiteSpace(err) ? "—" : err;
    }
}
