using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SmartDog.Application.Configuration;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesStandardUserAndReturnsSignInToken()
    {
        var repository = new Mock<IUserRepository>();
        User? createdUser = null;
        repository
            .Setup(item => item.GetByUsernameAsync("new_user"))
            .Returns(() => Task.FromResult(createdUser));
        repository
            .Setup(item => item.CreateAsync(It.IsAny<User>()))
            .Callback<User>(user => createdUser = user)
            .ReturnsAsync(true);
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        passwordHasher
            .Setup(item => item.HashPassword(It.IsAny<User>(), "a-long-password"))
            .Returns("hashed-password");
        passwordHasher
            .Setup(item => item.VerifyHashedPassword(
                It.IsAny<User>(),
                "hashed-password",
                "a-long-password"))
            .Returns(PasswordVerificationResult.Success);
        var service = CreateService(repository, passwordHasher);

        var result = await service.RegisterAsync(
            new RegisterRequest(" new_user ", "a-long-password"));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.NotNull(createdUser);
        Assert.Equal("new_user", createdUser.Username);
        Assert.Equal("User", createdUser.Role);
        Assert.Equal("hashed-password", createdUser.PasswordHash);
        passwordHasher.Verify(
            item => item.HashPassword(It.IsAny<User>(), "a-long-password"),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNullWhenUsernameAlreadyExists()
    {
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(item => item.GetByUsernameAsync("existing"))
            .ReturnsAsync(new User { Username = "existing" });
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var service = CreateService(repository, passwordHasher);

        var result = await service.RegisterAsync(
            new RegisterRequest("existing", "a-long-password"));

        Assert.Null(result);
        repository.Verify(
            item => item.CreateAsync(It.IsAny<User>()),
            Times.Never);
    }

    private static AuthenticationService CreateService(
        Mock<IUserRepository> repository,
        Mock<IPasswordHasher<User>> passwordHasher)
    {
        return new AuthenticationService(
            repository.Object,
            passwordHasher.Object,
            Options.Create(new JwtOptions
            {
                Key = "a-long-jwt-signing-key-with-32-bytes-minimum",
                Issuer = "SmartDog.Api",
                Audience = "SmartDog.Client",
                ExpirationMinutes = 60
            }),
            NullLogger<AuthenticationService>.Instance);
    }
}
