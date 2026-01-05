using Microsoft.EntityFrameworkCore;
using Quiz.Application.Abstractions;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Persistence;

namespace Quiz.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly QuizDbContext _db;

    public UserRepository(QuizDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public Task<User?> GetByUsernameAsync(string username)
        => _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<List<User>> ListAsync(int skip, int take)
        => _db.Users.AsNoTracking()
            .OrderBy(u => u.CreatedAtUtc)
            .Skip(skip).Take(take)
            .ToListAsync();

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}
