namespace Quiz.Api.Models.Matches;

public sealed class MatchHistoryResponse
{
    public Guid MatchId { get; set; }
    public string RoomCode { get; set; } = default!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string Status { get; set; } = default!;
}
