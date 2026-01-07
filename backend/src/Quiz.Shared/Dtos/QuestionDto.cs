namespace Quiz.Shared.Dtos;

public sealed class QuestionDto
{
    public string QuestionId { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string[] Options { get; set; } = [];

    //  IMPORTANTE:
    // Esse campo NÃO deve ser enviado ao cliente no futuro.
    // Por enquanto está aqui para facilitar o mock.
    public int CorrectOptionIndex { get; set; }
}
