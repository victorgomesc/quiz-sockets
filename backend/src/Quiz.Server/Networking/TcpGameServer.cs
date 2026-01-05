using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Quiz.Server.Rooms;

namespace Quiz.Server.Networking;

public sealed class TcpGameServer : IAsyncDisposable
{
    private readonly ILogger<TcpGameServer> _logger;
    private readonly RoomManager _roomManager;

    private TcpListener? _listener;
    private readonly List<ClientSession> _sessions = new();
    private readonly object _lock = new();

    public TcpGameServer(ILogger<TcpGameServer> logger, RoomManager roomManager)
    {
        _logger = logger;
        _roomManager = roomManager;
    }

    public async Task StartAsync(IPAddress bindIp, int port, CancellationToken ct)
    {
        _listener = new TcpListener(bindIp, port);
        _listener.Start();
        _logger.LogInformation("TCP GameServer listening on {Ip}:{Port}", bindIp, port);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(ct);

                client.NoDelay = true;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                var session = new ClientSession(client, _logger, _roomManager);

                lock (_lock) _sessions.Add(session);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await session.RunAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Session crashed");
                    }
                    finally
                    {
                        lock (_lock) _sessions.Remove(session);
                        await session.DisposeAsync();
                    }
                }, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { _listener?.Stop(); } catch { /* ignore */ }

        List<ClientSession> copy;
        lock (_lock) copy = _sessions.ToList();

        foreach (var s in copy)
        {
            try { await s.DisposeAsync(); } catch { /* ignore */ }
        }
    }
}
