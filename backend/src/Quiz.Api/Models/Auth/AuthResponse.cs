namespace Quiz.Api.Models.Auth;

public sealed class AuthResponse
{
    public string Token { get; set; } = default!;
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
}
