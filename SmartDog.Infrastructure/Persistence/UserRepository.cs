using Microsoft.EntityFrameworkCore;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;
using Microsoft.Data.SqlClient;

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

    public async Task<bool> CreateAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException
            {
                Number: 2601 or 2627
            })
        {
            _dbContext.Entry(user).State = EntityState.Detached;
            return false;
        }
    }
}