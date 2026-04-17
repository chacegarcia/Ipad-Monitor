using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PadLink.Hosting;
using PadLink.Hosting.Diagnostics;
using PadLink.HostService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PadLinkHostingOptions>(_ => { });
builder.Services.AddSingleton<DiagnosticsSnapshot>();
builder.Services.AddSingleton<PadLinkHostCoordinator>();
builder.Services.AddHostedService<PadLinkServiceWorker>();

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();
