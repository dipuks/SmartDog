using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartDog.Application.Configuration;
using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartDog.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
     IUserRepository userRepository,
     IPasswordHasher<User> passwordHasher,
     IOptions<JwtOptions> jwtOptions,
     ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(
    LoginRequest request)
    {
        var user =
            await _userRepository.GetByUsernameAsync(
                request.Username.Trim());

        if (user is null)
        {
            _logger.LogWarning(
                "Failed login attempt for user {Username}",
                request.Username);

            return null;
        }

        var passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (passwordResult ==
            PasswordVerificationResult.Failed)
        {
            _logger.LogWarning(
                "Failed login attempt for user {Username}",
                request.Username);

            return null;
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
            throw new InvalidOperationException("JWT key is not configured.");

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
            throw new InvalidOperationException("JWT issuer is not configured.");

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
            throw new InvalidOperationException("JWT audience is not configured.");

        var expiresAtUtc =
            DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new[]
        {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.UniqueName,
            user.Username),

        new Claim(
            ClaimTypes.Name,
            user.Username),

        new Claim(
            ClaimTypes.Role,
            user.Role)
    };

        var signingKey =
    new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        _logger.LogInformation(
            "User {Username} authenticated successfully",
            user.Username);

        return new LoginResponse(
            accessToken,
            expiresAtUtc);
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
    {
        var username = request.Username.Trim();
        if (await _userRepository.GetByUsernameAsync(username) is not null)
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = "User"
        };
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        if (!await _userRepository.CreateAsync(user))
        {
            return null;
        }

        return await LoginAsync(new LoginRequest(username, request.Password));
    }
}