using Microsoft.EntityFrameworkCore;
using Quiz.Application.Abstractions;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Persistence;

namespace Quiz.Infrastructure.Repositories;

public sealed class RankingRepository : IRankingRepository
{
    private readonly QuizDbContext _db;
    public RankingRepository(QuizDbContext db) => _db = db;

    public async Task<UserStats> GetOrCreateAsync(Guid userId)
    {
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats is not null) return stats;

        stats = new UserStats(userId);
        _db.UserStats.Add(stats);
        await _db.SaveChangesAsync();
        return stats;
    }

    public async Task SaveAsync(UserStats stats)
    {
        _db.UserStats.Update(stats);
        await _db.SaveChangesAsync();
    }

    public async Task<List<(User User, UserStats Stats)>> TopAsync(int take)
    {
        take = Math.Clamp(take, 1, 100);

        var query =
            from s in _db.UserStats.AsNoTracking()
            join u in _db.Users.AsNoTracking() on s.UserId equals u.Id
            orderby s.TotalScore descending, s.Wins descending, s.UpdatedAtUtc descending
            select new { u, s };

        var list = await query.Take(take).ToListAsync();
        return list.Select(x => (x.u, x.s)).ToList();
    }
}
