using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionAlternativeRatingService
{
    Task<IReadOnlyList<DecisionAlternativeRatingDto>?> GetAsync(
        Guid decisionId,
        Guid alternativeId);

    Task<DecisionAlternativeRatingDto?> SetAsync(
        Guid decisionId,
        Guid alternativeId,
        Guid criterionId,
        UpdateDecisionAlternativeRatingRequest request);
}
