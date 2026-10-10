using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionAlternativeService
{
    Task<IReadOnlyList<DecisionAlternativeDto>?>
        GetByDecisionIdAsync(Guid decisionId);

    Task<DecisionAlternativeDto?>
        GetByIdAsync(Guid decisionId, Guid alternativeId);

    Task<DecisionAlternativeDto?>
        CreateAsync(
            Guid decisionId,
            CreateDecisionAlternativeRequest request);

    Task<DecisionAlternativeDto?>
        UpdateAsync(
            Guid decisionId,
            Guid alternativeId,
            UpdateDecisionAlternativeRequest request);

    Task<bool> DeleteAsync(
        Guid decisionId,
        Guid alternativeId);
}