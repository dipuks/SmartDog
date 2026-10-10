using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionAlternativeRatingService : IDecisionAlternativeRatingService
{
    private readonly IDecisionAlternativeRepository _alternativeRepository;
    private readonly IDecisionCriterionRepository _criterionRepository;

    public DecisionAlternativeRatingService(
        IDecisionAlternativeRepository alternativeRepository,
        IDecisionCriterionRepository criterionRepository)
    {
        _alternativeRepository = alternativeRepository;
        _criterionRepository = criterionRepository;
    }

    public async Task<IReadOnlyList<DecisionAlternativeRatingDto>?> GetAsync(
        Guid decisionId,
        Guid alternativeId)
    {
        if (await _alternativeRepository.GetByIdAndDecisionIdAsync(
                alternativeId,
                decisionId) is null)
        {
            return null;
        }

        var ratings = await _alternativeRepository.GetRatingsByAlternativeIdAsync(
            decisionId,
            alternativeId);

        return ratings
            .Select(rating => new DecisionAlternativeRatingDto(
                rating.DecisionCriterionId,
                rating.Criterion!.Name,
                rating.Rating))
            .ToList();
    }

    public async Task<DecisionAlternativeRatingDto?> SetAsync(
        Guid decisionId,
        Guid alternativeId,
        Guid criterionId,
        UpdateDecisionAlternativeRatingRequest request)
    {
        if (await _alternativeRepository.GetByIdAndDecisionIdAsync(
                alternativeId,
                decisionId) is null)
        {
            return null;
        }

        var criterion = await _criterionRepository.GetByIdAndDecisionIdAsync(
            criterionId,
            decisionId);

        if (criterion is null)
        {
            return null;
        }

        var rating = await _alternativeRepository.GetRatingAsync(
            decisionId,
            alternativeId,
            criterionId);

        if (rating is null)
        {
            rating = new DecisionAlternativeCriterionRating
            {
                DecisionId = decisionId,
                DecisionAlternativeId = alternativeId,
                DecisionCriterionId = criterionId,
                Rating = request.Rating
            };

            await _alternativeRepository.AddRatingAsync(rating);
        }
        else
        {
            rating.Rating = request.Rating;
        }

        await _alternativeRepository.SaveChangesAsync();

        return new DecisionAlternativeRatingDto(
            criterion.Id,
            criterion.Name,
            rating.Rating);
    }
}
