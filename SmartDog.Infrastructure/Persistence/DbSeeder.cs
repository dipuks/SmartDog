using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        SmartDogDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        string username,
        string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = "Admin"
        };

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                password);

        await dbContext.Users.AddAsync(user);

        await dbContext.SaveChangesAsync();
    }
}