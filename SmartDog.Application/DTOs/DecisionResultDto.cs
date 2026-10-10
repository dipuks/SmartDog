namespace SmartDog.Application.DTOs;

public record DecisionResultDto(
    int Rank,
    Guid AlternativeId,
    string AlternativeName,
    decimal Score);
