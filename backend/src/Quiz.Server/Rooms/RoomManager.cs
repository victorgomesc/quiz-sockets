using System.Collections.Concurrent;
using Quiz.Server.Networking;
using Quiz.Server.Reporting;
using Quiz.Shared.Dtos;

namespace Quiz.Server.Rooms;

public sealed class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly MatchReportClient _reportClient;

    public RoomManager(MatchReportClient reportClient)
    {
        _reportClient = reportClient;
    }

    public GameRoom CreateRoom(string ownerSessionId, Guid ownerUserId)
    {
        var room = new GameRoom(ownerSessionId, ownerUserId, _reportClient);
        _rooms[room.Code] = room;
        return room;
    }


    public bool JoinRoom(string roomCode, ClientSession session)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
            return false;

        return room.AddPlayer(session);
    }

    public void LeaveRoom(string roomCode, ClientSession session)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
            return;

        room.RemovePlayer(session);

        if (room.IsEmpty)
            _rooms.TryRemove(roomCode, out _);
    }

    public bool TryStartMatch(string roomCode, ClientSession requester)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
            return false;

        if (requester.UserId is null)
            return false;

        return room.StartMatch(requester.UserId.Value);
    }

    public bool SubmitAnswer(string roomCode, ClientSession session, AnswerDto answer)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
            return false;

        return room.SubmitAnswer(session, answer);
    }
}
