namespace Quiz.Shared.Dtos;

public sealed class ScoreUpdateDto
{
    public Guid UserId { get; set; }
    public int Delta { get; set; }
    public int TotalScore { get; set; }
}
