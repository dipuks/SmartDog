using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public interface IDecisionAlternativeRepository
{
    Task<IReadOnlyList<DecisionAlternative>> GetByDecisionIdAsync(
        Guid decisionId);

    Task<IReadOnlyList<DecisionAlternative>> GetByDecisionIdWithRatingsAsync(
        Guid decisionId);

    Task<DecisionAlternative?> GetByIdAsync(Guid id);

    Task<DecisionAlternative?> GetByIdAndDecisionIdAsync(
        Guid id,
        Guid decisionId);

    Task<IReadOnlyList<DecisionAlternativeCriterionRating>>
        GetRatingsByAlternativeIdAsync(Guid decisionId, Guid alternativeId);

    Task<DecisionAlternativeCriterionRating?> GetRatingAsync(
        Guid decisionId,
        Guid alternativeId,
        Guid criterionId);

    Task AddRatingAsync(DecisionAlternativeCriterionRating rating);

    Task AddAsync(DecisionAlternative alternative);

    void Update(DecisionAlternative alternative);

    void Delete(DecisionAlternative alternative);

    Task SaveChangesAsync();
}