using SmartDog.Application.DTOs;
using SmartDog.Domain.Engines;

namespace SmartDog.Application.Services;

public class DecisionScoringService : IDecisionScoringService
{
    private readonly IDecisionRepository _decisionRepository;
    private readonly IDecisionCriterionRepository _criterionRepository;
    private readonly IDecisionAlternativeRepository _alternativeRepository;
    private readonly IDecisionScoringEngine _scoringEngine;

    public DecisionScoringService(
        IDecisionRepository decisionRepository,
        IDecisionCriterionRepository criterionRepository,
        IDecisionAlternativeRepository alternativeRepository,
        IDecisionScoringEngine scoringEngine)
    {
        _decisionRepository = decisionRepository;
        _criterionRepository = criterionRepository;
        _alternativeRepository = alternativeRepository;
        _scoringEngine = scoringEngine;
    }

    public async Task<IReadOnlyList<DecisionResultDto>?> GetResultsAsync(
        Guid decisionId)
    {
        if (await _decisionRepository.GetByIdAsync(decisionId) is null)
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
