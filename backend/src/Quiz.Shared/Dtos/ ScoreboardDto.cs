namespace Quiz.Shared.Dtos;

public sealed class ScoreboardDto
{
    public string UserId { get; set; } = default!;

    public int Score { get; set; }
}
