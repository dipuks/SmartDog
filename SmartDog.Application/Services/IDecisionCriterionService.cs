using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionCriterionService
{
    Task<IReadOnlyList<DecisionCriterionDto>?> GetByDecisionIdAsync(
        Guid decisionId);

    Task<DecisionCriterionDto?> GetByIdAsync(Guid decisionId, Guid criterionId);

    Task<DecisionCriterionDto?> CreateAsync(
        Guid decisionId,
        CreateDecisionCriterionRequest request);

    Task<DecisionCriterionDto?> UpdateAsync(
        Guid decisionId,
        Guid criterionId,
        UpdateDecisionCriterionRequest request);

    Task<bool> DeleteAsync(Guid decisionId, Guid criterionId);
}
