using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Quiz.Server.Rooms;
using Quiz.Shared.Dtos;
using Quiz.Shared.Networking;

namespace Quiz.Server.Networking;

public sealed class ClientSession : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly ILogger _logger;
    private readonly RoomManager _roomManager;

    private readonly Dictionary<string, Func<MessageEnvelope, CancellationToken, Task>> _routes;

    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public Guid? UserId { get; private set; }

    public string? CurrentRoomCode { get; private set; }

    public ClientSession(TcpClient client, ILogger logger, RoomManager roomManager)
    {
        _client = client;
        _stream = client.GetStream();
        _logger = logger;
        _roomManager = roomManager;

        _routes = new(StringComparer.OrdinalIgnoreCase)
        {
            [MessageTypes.HELLO] = HandleHelloAsync,
            [MessageTypes.PING] = HandlePingAsync,
            [MessageTypes.CREATE_ROOM] = HandleCreateRoomAsync,
            [MessageTypes.JOIN_ROOM] = HandleJoinRoomAsync,
            [MessageTypes.LEAVE_ROOM] = HandleLeaveRoomAsync,
            [MessageTypes.ANSWER] = HandleAnswerAsync,
            [MessageTypes.START_MATCH] = HandleStartMatchAsync
        };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Client connected: {SessionId} from {RemoteEndPoint}",
            SessionId, _client.Client.RemoteEndPoint);

        // Opcional: banner/hello do servidor
        await SendAsync(MessageEnvelope.Create(
            MessageTypes.HELLO,
            new HelloDto { Message = "Welcome to Quiz Server", ServerTimeUtc = DateTime.UtcNow },
            requestId: null
        ), ct);

        await foreach (var msg in JsonLineProtocol.ReadAllAsync(_stream, ct))
        {
            try
            {
                if (_routes.TryGetValue(msg.Type, out var handler))
                {
                    await handler(msg, ct);
                }
                else
                {
                    await SendErrorAsync("UNKNOWN_TYPE", $"Unknown message type: {msg.Type}", msg.RequestId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling {Type} for session {SessionId}", msg.Type, SessionId);
                await SendErrorAsync("HANDLER_ERROR", ex.Message, msg.RequestId, ct);
            }
        }

        // desconexão: limpar estado
        await SafeLeaveRoomOnDisconnect(ct);

        _logger.LogInformation("Client disconnected: {SessionId}", SessionId);
    }

    private async Task HandleHelloAsync(MessageEnvelope msg, CancellationToken ct)
    {
        var dto = msg.PayloadAs<HelloDto>();

        if (dto?.UserId is not null && Guid.TryParse(dto.UserId, out var parsed))
        {
            UserId = parsed;
        }

        await SendAsync(MessageEnvelope.Create(
            MessageTypes.HELLO,
            new HelloDto
            {
                Message = "HELLO_ACK",
                UserId = UserId?.ToString(),
                ServerTimeUtc = DateTime.UtcNow
            },
            msg.RequestId
        ), ct);
    }

    private Task HandlePingAsync(MessageEnvelope msg, CancellationToken ct)
        => SendAsync(MessageEnvelope.Create(MessageTypes.PONG, new { ts = DateTime.UtcNow }, msg.RequestId), ct);

    private async Task HandleCreateRoomAsync(MessageEnvelope msg, CancellationToken ct)
    {
        var dto = msg.PayloadAs<CreateRoomDto>();
        if (dto is null)
        {
            await SendErrorAsync("INVALID_PAYLOAD", "CreateRoom payload is null/invalid", msg.RequestId, ct);
            return;
        }

        if (UserId is null)
        {
            await SendErrorAsync(
                "UNAUTHENTICATED",
                "User must send HELLO with valid userId before creating room",
                msg.RequestId,
                ct
            );
            return;
        }

        var room = _roomManager.CreateRoom(
            ownerSessionId: SessionId,
            ownerUserId: UserId.Value
        );


        CurrentRoomCode = room.Code;

        _roomManager.JoinRoom(room.Code, this);

        await SendAsync(MessageEnvelope.Create(
            MessageTypes.ROOM_CREATED,
            new { roomCode = room.Code },
            msg.RequestId
        ), ct);
    }

    private async Task HandleJoinRoomAsync(MessageEnvelope msg, CancellationToken ct)
    {
        var dto = msg.PayloadAs<JoinRoomDto>();
        if (dto is null || string.IsNullOrWhiteSpace(dto.RoomCode))
        {
            await SendErrorAsync("INVALID_PAYLOAD", "JoinRoom requires roomCode", msg.RequestId, ct);
            return;
        }

        var ok = _roomManager.JoinRoom(dto.RoomCode, this);
        if (!ok)
        {
            await SendErrorAsync("ROOM_NOT_FOUND", $"Room {dto.RoomCode} not found", msg.RequestId, ct);
            return;
        }

        CurrentRoomCode = dto.RoomCode;

        await SendAsync(MessageEnvelope.Create(
            MessageTypes.JOINED_ROOM,
            new { roomCode = dto.RoomCode, sessionId = SessionId, userId = UserId },
            msg.RequestId
        ), ct);
    }

    private async Task HandleLeaveRoomAsync(MessageEnvelope msg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(CurrentRoomCode))
        {
            await SendErrorAsync("NOT_IN_ROOM", "You are not in a room", msg.RequestId, ct);
            return;
        }

        _roomManager.LeaveRoom(CurrentRoomCode, this);
        var left = CurrentRoomCode;
        CurrentRoomCode = null;

        await SendAsync(MessageEnvelope.Create(
            MessageTypes.LEFT_ROOM,
            new { roomCode = left },
            msg.RequestId
        ), ct);
    }

    private async Task HandleStartMatchAsync(MessageEnvelope msg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(CurrentRoomCode))
        {
            await SendErrorAsync("NOT_IN_ROOM", "You are not in a room", msg.RequestId, ct);
            return;
        }

        var ok = _roomManager.TryStartMatch(CurrentRoomCode, this);
        if (!ok)
        {
            await SendErrorAsync(
                "FORBIDDEN_OR_INVALID_STATE",
                "Only the room owner can start the match",
                msg.RequestId,
                ct
            );
            return;
        }

        await SendAsync(
            MessageEnvelope.Create(
                MessageTypes.MATCH_STARTED,
                new { roomCode = CurrentRoomCode },
                msg.RequestId
            ),
            ct
        );
    }

    private async Task HandleAnswerAsync(MessageEnvelope msg, CancellationToken ct)
    {
        var dto = msg.PayloadAs<AnswerDto>();
        if (dto is null)
        {
            await SendErrorAsync("INVALID_PAYLOAD", "Answer payload is null/invalid", msg.RequestId, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentRoomCode))
        {
            await SendErrorAsync("NOT_IN_ROOM", "You are not in a room", msg.RequestId, ct);
            return;
        }

        // Encaminha para a sala (GameLoop/GameRoom)
        var accepted = _roomManager.SubmitAnswer(CurrentRoomCode, this, dto);
        if (!accepted)
        {
            await SendErrorAsync("ANSWER_REJECTED", "Answer rejected by room state", msg.RequestId, ct);
            return;
        }

        // ACK opcional
        await SendAsync(MessageEnvelope.Create(
            MessageTypes.ANSWER,
            new { ok = true, dto.QuestionId, dto.SelectedOptionIndex },
            msg.RequestId
        ), ct);
    }

    public Task SendAsync(MessageEnvelope envelope, CancellationToken ct)
        => JsonLineProtocol.WriteAsync(_stream, envelope, ct);

    private Task SendErrorAsync(string code, string message, string? requestId, CancellationToken ct)
        => SendAsync(MessageEnvelope.Create(MessageTypes.ERROR, new { code, message }, requestId), ct);

    private async Task SafeLeaveRoomOnDisconnect(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(CurrentRoomCode))
        {
            try { _roomManager.LeaveRoom(CurrentRoomCode!, this); } catch { /* ignore */ }
            CurrentRoomCode = null;
        }

        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        try { _stream.Close(); } catch { /* ignore */ }
        try { _client.Close(); } catch { /* ignore */ }
        return ValueTask.CompletedTask;
    }
}
