using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionAlternativeRatingService : IDecisionAlternativeRatingService
{
    private readonly IDecisionAlternativeRepository _alternativeRepository;
    private readonly IDecisionCriterionRepository _criterionRepository;
    private readonly IDecisionRepository _decisionRepository;
    private readonly ICurrentUser _currentUser;

    public DecisionAlternativeRatingService(
        IDecisionAlternativeRepository alternativeRepository,
        IDecisionCriterionRepository criterionRepository,
        IDecisionRepository decisionRepository,
        ICurrentUser currentUser)
    {
        _alternativeRepository = alternativeRepository;
        _criterionRepository = criterionRepository;
        _decisionRepository = decisionRepository;
        _currentUser = currentUser;
    }

    /// <summary>Every operation first checks that the decision belongs to the signed-in user.</summary>
    private Task<bool> OwnsDecisionAsync(Guid decisionId) =>
        _decisionRepository.ExistsAsync(decisionId, _currentUser.RequireUserId());

    public async Task<IReadOnlyList<DecisionAlternativeRatingDto>?> GetAsync(
        Guid decisionId,
        Guid alternativeId)
    {
        if (!await OwnsDecisionAsync(decisionId) ||
            await _alternativeRepository.GetByIdAndDecisionIdAsync(
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
        if (!await OwnsDecisionAsync(decisionId) ||
            await _alternativeRepository.GetByIdAndDecisionIdAsync(
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
