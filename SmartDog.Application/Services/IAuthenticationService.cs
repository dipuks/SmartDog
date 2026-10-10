using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(
        LoginRequest request);
}