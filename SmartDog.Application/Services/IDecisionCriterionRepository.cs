using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public interface IDecisionCriterionRepository
{
    Task<IReadOnlyList<DecisionCriterion>> GetByDecisionIdAsync(
        Guid decisionId);

    Task<DecisionCriterion?> GetByIdAsync(Guid criterionId);

    Task<DecisionCriterion?> GetByIdAndDecisionIdAsync(
        Guid criterionId,
        Guid decisionId);

    Task AddAsync(DecisionCriterion criterion);

    void Update(DecisionCriterion criterion);

    Task DeleteAsync(DecisionCriterion criterion);

    Task SaveChangesAsync();
}
