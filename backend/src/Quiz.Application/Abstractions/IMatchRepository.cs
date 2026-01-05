using Quiz.Domain.Entities;

namespace Quiz.Application.Abstractions;

public interface IMatchRepository
{
    Task AddAsync(Match match);
    Task<List<Match>> ListByUserAsync(Guid userId, int skip, int take);
}
