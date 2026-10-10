namespace SmartDog.Application.DTOs;

public record DecisionAlternativeRatingDto(
    Guid DecisionCriterionId,
    string CriterionName,
    decimal Rating);
