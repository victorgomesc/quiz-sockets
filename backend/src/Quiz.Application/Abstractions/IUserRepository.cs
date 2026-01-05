using Quiz.Domain.Entities;

namespace Quiz.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);

    Task<List<User>> ListAsync(int skip, int take);

    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
