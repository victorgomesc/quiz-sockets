namespace Quiz.Shared.Dtos;

public sealed class PlayerStateDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; } // opcional (por enquanto pode ficar null)
    public bool IsOnline { get; set; }
    public bool HasAnswered { get; set; }
    public int Score { get; set; }
}
