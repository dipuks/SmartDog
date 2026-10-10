namespace SmartDog.Application.DTOs;

public record DecisionCriterionDto(
    Guid Id,
    Guid DecisionId,
    string Name,
    decimal Weight);
