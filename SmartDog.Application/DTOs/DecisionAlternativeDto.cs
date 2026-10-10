namespace SmartDog.Application.DTOs;

public record DecisionAlternativeDto(
    Guid Id,
    Guid DecisionId,
    string Name,
    string? Description,
    decimal Score);