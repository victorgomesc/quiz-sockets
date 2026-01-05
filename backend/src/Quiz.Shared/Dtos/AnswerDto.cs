namespace Quiz.Shared.Dtos;

public sealed class AnswerDto
{
    public string QuestionId { get; set; } = default!;
    public int SelectedOptionIndex { get; set; }
}
