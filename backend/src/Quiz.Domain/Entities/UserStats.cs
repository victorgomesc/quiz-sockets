namespace Quiz.Domain.Entities;

public sealed class UserStats
{
    public Guid UserId { get; private set; }
    public int TotalScore { get; private set; }
    public int MatchesPlayed { get; private set; }
    public int Wins { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    private UserStats() { }

    public UserStats(Guid userId)
    {
        UserId = userId;
    }

    public void ApplyMatch(int addedScore, bool isWinner)
    {
        TotalScore += addedScore;
        MatchesPlayed += 1;
        if (isWinner) Wins += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
