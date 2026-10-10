namespace SmartDog.Application.DTOs;

public record DecisionDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime CreatedAt,
    string Status,
    byte[] RowVersion);
