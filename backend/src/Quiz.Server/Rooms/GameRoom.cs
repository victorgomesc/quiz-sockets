using System.Collections.Concurrent;
using Quiz.Server.Networking;
using Quiz.Server.Reporting;
using Quiz.Shared.Dtos;
using Quiz.Shared.Networking;

namespace Quiz.Server.Rooms;

public sealed class GameRoom
{
    private readonly ConcurrentDictionary<string, ClientSession> _players = new();
    private readonly object _stateLock = new();

    public string Code { get; }
    public Guid OwnerUserId { get; }


    private GameLoop? _gameLoop;
    private readonly MatchReportClient _reportClient;

    public bool IsEmpty => _players.IsEmpty;

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }


    public GameRoom(string ownerSessionId, Guid ownerUserId, MatchReportClient reportClient)
    {
        Code = GenerateRoomCode();
        OwnerUserId = ownerUserId;
        _reportClient = reportClient;
    }


    public bool AddPlayer(ClientSession session)
    {
        return _players.TryAdd(session.SessionId, session);
    }

    public void RemovePlayer(ClientSession session)
    {
        _players.TryRemove(session.SessionId, out _);
    }

    
    public bool StartMatch(Guid requesterUserId)
    {
        lock (_stateLock)
        {
            if (requesterUserId != OwnerUserId)
                return false;

            if (_gameLoop != null)
                return false;

            StartedAtUtc = DateTime.UtcNow;
            _gameLoop = new GameLoop(this, _reportClient);
            _ = _gameLoop.RunAsync();
            return true;
        }
    }

    public bool SubmitAnswer(ClientSession session, AnswerDto dto)
    {
        return _gameLoop?.ReceiveAnswer(session, dto) ?? false;
    }

    public async Task BroadcastAsync(MessageEnvelope envelope)
    {
        foreach (var player in _players.Values)
        {
            try
            {
                await player.SendAsync(envelope, CancellationToken.None);
            }
            catch
            {
                // Ignora falha individual
            }
        }
    }

    public void EndMatch()
    {
        EndedAtUtc = DateTime.UtcNow;
    }


    private static string GenerateRoomCode()
        => Random.Shared.Next(100000, 999999).ToString();
}
