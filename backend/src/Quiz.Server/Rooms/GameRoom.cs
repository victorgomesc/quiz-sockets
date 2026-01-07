using System.Collections.Concurrent;
using Quiz.Server.Networking;
using Quiz.Server.Reporting;
using Quiz.Shared.Dtos;
using Quiz.Shared.Networking;

namespace Quiz.Server.Rooms;

public sealed class GameRoom
{
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new(); // SessionId -> session
    private readonly ConcurrentDictionary<Guid, PlayerStateDto> _players = new();  // UserId -> state
    private readonly object _stateLock = new();

    public string Code { get; }
    public Guid OwnerUserId { get; }

    private GameLoop? _gameLoop;
    private readonly MatchReportClient _reportClient;

    public bool IsEmpty => _sessions.IsEmpty;

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
        if (session.UserId is null) return false;

        // adiciona a conexão
        if (!_sessions.TryAdd(session.SessionId, session))
            return false;

        // upsert estado do jogador
        var userId = session.UserId.Value;

        _players.AddOrUpdate(
            userId,
            _ => new PlayerStateDto
            {
                UserId = userId,
                IsOnline = true,
                HasAnswered = false,
                Score = 0
            },
            (_, existing) =>
            {
                existing.IsOnline = true;
                existing.HasAnswered = false;
                return existing;
            }
        );

        // 1) avisa todo mundo que alguém entrou
        _ = BroadcastAsync(MessageEnvelope.Create(
            MessageTypes.PLAYER_JOINED,
            _players[userId]
        ));

        // 2) envia snapshot completo para quem entrou (garante UI consistente)
        _ = session.SendAsync(MessageEnvelope.Create(
            MessageTypes.PLAYERS_SNAPSHOT,
            new PlayersSnapshotDto { RoomCode = Code, Players = GetSnapshot() }
        ), CancellationToken.None);

        // 3) opcional: também broadcast do snapshot (mantém todos sincronizados)
        _ = BroadcastSnapshotAsync();

        return true;
    }

    public void RemovePlayer(ClientSession session)
    {
        _sessions.TryRemove(session.SessionId, out _);

        if (session.UserId is null) return;

        var userId = session.UserId.Value;

        if (_players.TryGetValue(userId, out var state))
        {
            state.IsOnline = false;
            state.HasAnswered = false;
        }

        _ = BroadcastAsync(MessageEnvelope.Create(
            MessageTypes.PLAYER_LEFT,
            new { userId }
        ));

        _ = BroadcastSnapshotAsync();
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
        => _gameLoop?.ReceiveAnswer(session, dto) ?? false;

    public async Task BroadcastAsync(MessageEnvelope envelope)
    {
        foreach (var s in _sessions.Values)
        {
            try { await s.SendAsync(envelope, CancellationToken.None); }
            catch { /* ignora falha individual */ }
        }
    }

    public void EndMatch()
    {
        EndedAtUtc = DateTime.UtcNow;
    }

    // ====== MÉTODOS DE ESTADO PARA UX ======

    public List<PlayerStateDto> GetSnapshot()
        => _players.Values
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.UserId)
            .Select(p => new PlayerStateDto
            {
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                IsOnline = p.IsOnline,
                HasAnswered = p.HasAnswered,
                Score = p.Score
            })
            .ToList();

    public Task BroadcastSnapshotAsync()
        => BroadcastAsync(MessageEnvelope.Create(
            MessageTypes.PLAYERS_SNAPSHOT,
            new PlayersSnapshotDto { RoomCode = Code, Players = GetSnapshot() }
        ));

    public void ResetAnswersForNewQuestion()
    {
        foreach (var p in _players.Values)
        {
            if (p.IsOnline)
                p.HasAnswered = false;
        }
    }

    public void MarkAnswered(Guid userId)
    {
        if (_players.TryGetValue(userId, out var p))
            p.HasAnswered = true;
    }

    public int AddScore(Guid userId, int delta)
    {
        _players.AddOrUpdate(
            userId,
            _ => new PlayerStateDto
            {
                UserId = userId,
                IsOnline = true,
                HasAnswered = true,
                Score = Math.Max(0, delta)
            },
            (_, existing) =>
            {
                existing.Score += delta;
                if (existing.Score < 0) existing.Score = 0;
                return existing;
            }
        );

        return _players[userId].Score;
    }

    private static string GenerateRoomCode()
        => Random.Shared.Next(100000, 999999).ToString();
}
