namespace Quiz.Api.Models.Users;

public sealed class UpdateUserRequest
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
}
