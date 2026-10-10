namespace SmartDog.Domain.Models;

public record DecisionScore(
    Guid AlternativeId,
    string AlternativeName,
    decimal Score);