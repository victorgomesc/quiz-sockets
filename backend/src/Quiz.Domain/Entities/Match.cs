namespace Quiz.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string RoomCode { get; private set; } = default!;
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; private set; }
    public string Status { get; private set; } = "Ended"; // simplificado

    public List<MatchParticipant> Participants { get; private set; } = new();

    private Match() { }

    public Match(string roomCode, DateTime startedAtUtc, DateTime endedAtUtc)
    {
        RoomCode = roomCode;
        StartedAtUtc = startedAtUtc;
        EndedAtUtc = endedAtUtc;
        Status = "Ended";
    }
}
