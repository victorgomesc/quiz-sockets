namespace Quiz.Api.Models.Ranking;

public sealed class RankingEntryResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
    public int TotalScore { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
