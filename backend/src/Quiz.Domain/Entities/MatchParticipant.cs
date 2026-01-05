namespace Quiz.Domain.Entities;

public sealed class MatchParticipant
{
    public Guid MatchId { get; private set; }
    public Guid UserId { get; private set; }

    public int Score { get; private set; }
    public int CorrectAnswers { get; private set; }
    public int TotalAnswers { get; private set; }

    private MatchParticipant() { }

    public MatchParticipant(Guid matchId, Guid userId, int score, int correctAnswers, int totalAnswers)
    {
        MatchId = matchId;
        UserId = userId;
        Score = score;
        CorrectAnswers = correctAnswers;
        TotalAnswers = totalAnswers;
    }
}
