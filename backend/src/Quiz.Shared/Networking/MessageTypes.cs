namespace Quiz.Shared.Networking;

public static class MessageTypes
{
    public const string HELLO = "HELLO";
    public const string ERROR = "ERROR";
    public const string PING = "PING";
    public const string PONG = "PONG";

    // Room flow
    public const string CREATE_ROOM = "CREATE_ROOM";
    public const string ROOM_CREATED = "ROOM_CREATED";

    public const string JOIN_ROOM = "JOIN_ROOM";
    public const string JOINED_ROOM = "JOINED_ROOM";

    public const string LEAVE_ROOM = "LEAVE_ROOM";
    public const string LEFT_ROOM = "LEFT_ROOM";

    public const string START_MATCH = "START_MATCH";
    public const string MATCH_STARTED = "MATCH_STARTED";

    public const string QUESTION = "QUESTION";
    public const string ANSWER = "ANSWER";

    public const string SCOREBOARD = "SCOREBOARD";
    public const string MATCH_ENDED = "MATCH_ENDED";
    public const string PLAYER_JOINED = "PLAYER_JOINED";
    public const string PLAYER_LEFT = "PLAYER_LEFT";
    public const string PLAYERS_SNAPSHOT = "PLAYERS_SNAPSHOT";

    public const string ANSWER_RECEIVED = "ANSWER_RECEIVED";
    public const string SCORE_UPDATE = "SCORE_UPDATE";
    public const string ANSWER_RESULT = "ANSWER_RESULT";

}
