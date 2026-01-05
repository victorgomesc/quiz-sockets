using Quiz.Domain.Entities;

namespace Quiz.Application.Abstractions;

public interface IRankingRepository
{
    Task<UserStats> GetOrCreateAsync(Guid userId);
    Task SaveAsync(UserStats stats);

    Task<List<(User User, UserStats Stats)>> TopAsync(int take);
}
