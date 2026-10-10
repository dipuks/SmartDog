using Microsoft.EntityFrameworkCore;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly SmartDogDbContext _dbContext;

    public UserRepository(
        SmartDogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByUsernameAsync(
        string username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Username == username);
    }

    public async Task AddAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}