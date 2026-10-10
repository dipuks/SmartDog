using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(
        string username);

    Task AddAsync(User user);

    Task SaveChangesAsync();
}