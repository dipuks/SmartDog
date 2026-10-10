using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionScoringService
{
    Task<IReadOnlyList<DecisionResultDto>?> GetResultsAsync(Guid decisionId);
}
