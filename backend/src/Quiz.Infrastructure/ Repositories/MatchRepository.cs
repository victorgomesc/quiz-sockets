using Microsoft.EntityFrameworkCore;
using Quiz.Application.Abstractions;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Persistence;

namespace Quiz.Infrastructure.Repositories;

public sealed class MatchRepository : IMatchRepository
{
    private readonly QuizDbContext _db;
    public MatchRepository(QuizDbContext db) => _db = db;

    public async Task AddAsync(Match match)
    {
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();
    }

    public Task<List<Match>> ListByUserAsync(Guid userId, int skip, int take)
    {
        return _db.Matches.AsNoTracking()
            .Where(m => m.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(m => m.EndedAtUtc)
            .Skip(skip).Take(take)
            .ToListAsync();
    }
}
