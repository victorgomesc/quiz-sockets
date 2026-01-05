namespace Quiz.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private User() { }

    public User(string username, string email, string passwordHash)
    {
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    public void UpdateProfile(string username, string email)
    {
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
