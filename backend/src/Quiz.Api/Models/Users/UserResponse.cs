namespace Quiz.Api.Models.Users;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
