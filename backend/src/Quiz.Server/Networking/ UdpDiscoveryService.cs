using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Quiz.Server.Networking;

public sealed class UdpDiscoveryService : IAsyncDisposable
{
    private readonly ILogger<UdpDiscoveryService> _logger;
    private UdpClient? _udp;

    public const string DiscoveryRequest = "QUIZ_DISCOVERY_REQUEST";
    public const string DiscoveryResponsePrefix = "QUIZ_DISCOVERY_RESPONSE";

    public UdpDiscoveryService(ILogger<UdpDiscoveryService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(int udpPort, int tcpPort, CancellationToken ct)
    {
        _udp = new UdpClient(udpPort)
        {
            EnableBroadcast = true
        };

        _logger.LogInformation("UDP discovery listening on port {Port}", udpPort);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = await _udp.ReceiveAsync(ct);
                    var msg = Encoding.UTF8.GetString(result.Buffer).Trim();

                    if (!string.Equals(msg, DiscoveryRequest, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var remote = result.RemoteEndPoint;
                    var localIp = GetBestLocalIpFor(remote.Address) ?? IPAddress.Loopback;

                    var response = $"{DiscoveryResponsePrefix}|{localIp}|{tcpPort}";
                    var bytes = Encoding.UTF8.GetBytes(response);

                    // UdpClient NÃO suporta CancellationToken
                    await _udp.SendAsync(
                        bytes,
                        bytes.Length,
                        remote.Address.ToString(),
                        remote.Port
                    );

                    _logger.LogDebug(
                        "Discovery responded to {Remote} with {Response}",
                        remote.ToString(),
                        response
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UDP discovery crashed");
            }
        }, ct);

        return Task.CompletedTask;
    }

    private static IPAddress? GetBestLocalIpFor(IPAddress remote)
    {
        // Simples: retorna um IPv4 não-loopback se existir
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(a =>
                a.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(a));
        }
        catch
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        try { _udp?.Close(); } catch { /* ignore */ }
        _udp?.Dispose();
        return ValueTask.CompletedTask;
    }
}
