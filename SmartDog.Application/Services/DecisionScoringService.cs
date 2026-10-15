using SmartDog.Application.DTOs;
using SmartDog.Domain.Engines;

namespace SmartDog.Application.Services;

public class DecisionScoringService : IDecisionScoringService
{
    private readonly IDecisionRepository _decisionRepository;
    private readonly IDecisionCriterionRepository _criterionRepository;
    private readonly IDecisionAlternativeRepository _alternativeRepository;
    private readonly IDecisionScoringEngine _scoringEngine;
    private readonly ICurrentUser _currentUser;

    public DecisionScoringService(
        IDecisionRepository decisionRepository,
        IDecisionCriterionRepository criterionRepository,
        IDecisionAlternativeRepository alternativeRepository,
        IDecisionScoringEngine scoringEngine,
        ICurrentUser currentUser)
    {
        _decisionRepository = decisionRepository;
        _criterionRepository = criterionRepository;
        _alternativeRepository = alternativeRepository;
        _scoringEngine = scoringEngine;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DecisionResultDto>?> GetResultsAsync(
        Guid decisionId)
    {
        if (!await _decisionRepository.ExistsAsync(decisionId, _currentUser.RequireUserId()))
        {
            return null;
        }

        var criteria = await _criterionRepository.GetByDecisionIdAsync(decisionId);
        var alternatives = await _alternativeRepository
            .GetByDecisionIdWithRatingsAsync(decisionId);
        var results = _scoringEngine.Score(criteria, alternatives);

        return results
            .Select((result, index) => new DecisionResultDto(
                index + 1,
                result.AlternativeId,
                result.AlternativeName,
                result.Score))
            .ToList();
    }
}
