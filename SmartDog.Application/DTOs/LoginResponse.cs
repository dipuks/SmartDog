namespace SmartDog.Application.DTOs;

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc);