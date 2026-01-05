namespace Quiz.Shared.Dtos;

public sealed class QuestionPublicDto
{
    public string QuestionId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string[] Options { get; set; } = [];
}
