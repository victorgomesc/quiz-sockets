using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quiz.Server.Networking;
using Quiz.Server.Rooms;
using Quiz.Server.Reporting;


var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// DI
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<TcpGameServer>();
builder.Services.AddSingleton<UdpDiscoveryService>();
builder.Services.AddHttpClient<MatchReportClient>();

builder.Services.AddSingleton<MatchReportClient>();


var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Quiz.Server");
var tcp = host.Services.GetRequiredService<TcpGameServer>();
var udp = host.Services.GetRequiredService<UdpDiscoveryService>();

// Configs (pode jogar para appsettings depois)
var tcpPort = 5050;
var udpPort = 5051;
var bindIp = IPAddress.Any;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

logger.LogInformation("Starting services...");

await udp.StartAsync(udpPort, tcpPort, cts.Token);
await tcp.StartAsync(bindIp, tcpPort, cts.Token);

await host.RunAsync(cts.Token);
