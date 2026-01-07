namespace Quiz.Shared.Dtos;

public sealed class AnswerResultDto
{
    public string QuestionId { get; set; } = default!;
    public bool IsCorrect { get; set; }
    public int Delta { get; set; }
    public int TotalScore { get; set; }
}
