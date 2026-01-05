namespace Quiz.Api.Models.Matches;

public sealed class MatchReportRequest
{
    public string RoomCode { get; set; } = default!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }

    public List<PlayerResultDto> Players { get; set; } = new();
}

public sealed class PlayerResultDto
{
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
}
