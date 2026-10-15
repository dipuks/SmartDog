using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionCriterionService : IDecisionCriterionService
{
    private readonly IDecisionCriterionRepository _criterionRepository;
    private readonly IDecisionRepository _decisionRepository;
    private readonly ICurrentUser _currentUser;

    public DecisionCriterionService(
        IDecisionCriterionRepository criterionRepository,
        IDecisionRepository decisionRepository,
        ICurrentUser currentUser)
    {
        _criterionRepository = criterionRepository;
        _decisionRepository = decisionRepository;
        _currentUser = currentUser;
    }

    /// <summary>Every operation first checks that the decision belongs to the signed-in user.</summary>
    private Task<bool> OwnsDecisionAsync(Guid decisionId) =>
        _decisionRepository.ExistsAsync(decisionId, _currentUser.RequireUserId());

    public async Task<IReadOnlyList<DecisionCriterionDto>?> GetByDecisionIdAsync(
        Guid decisionId)
    {
        if (!await OwnsDecisionAsync(decisionId))
        {
            return null;
        }

        var criteria = await _criterionRepository.GetByDecisionIdAsync(decisionId);
        return criteria.Select(MapToDto).ToList();
    }

    public async Task<DecisionCriterionDto?> GetByIdAsync(
        Guid decisionId,
        Guid criterionId)
    {
        if (!await OwnsDecisionAsync(decisionId))
        {
            return null;
        }

        var criterion = await _criterionRepository.GetByIdAndDecisionIdAsync(
            criterionId,
            decisionId);

        return criterion is null ? null : MapToDto(criterion);
    }

    public async Task<DecisionCriterionDto?> CreateAsync(
        Guid decisionId,
        CreateDecisionCriterionRequest request)
    {
        if (!await OwnsDecisionAsync(decisionId))
        {
            return null;
        }

        var criterion = new DecisionCriterion
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            Name = request.Name,
            Weight = request.Weight
        };

        await _criterionRepository.AddAsync(criterion);
        await _criterionRepository.SaveChangesAsync();
        return MapToDto(criterion);
    }

    public async Task<DecisionCriterionDto?> UpdateAsync(
        Guid decisionId,
        Guid criterionId,
        UpdateDecisionCriterionRequest request)
    {
        if (!await OwnsDecisionAsync(decisionId))
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

        criterion.Name = request.Name;
        criterion.Weight = request.Weight;
        _criterionRepository.Update(criterion);
        await _criterionRepository.SaveChangesAsync();
        return MapToDto(criterion);
    }

    public async Task<bool> DeleteAsync(Guid decisionId, Guid criterionId)
    {
        if (!await OwnsDecisionAsync(decisionId))
        {
            return false;
        }

        var criterion = await _criterionRepository.GetByIdAndDecisionIdAsync(
            criterionId,
            decisionId);

        if (criterion is null)
        {
            return false;
        }

        await _criterionRepository.DeleteAsync(criterion);
        await _criterionRepository.SaveChangesAsync();
        return true;
    }

    private static DecisionCriterionDto MapToDto(DecisionCriterion criterion)
    {
        return new DecisionCriterionDto(
            criterion.Id,
            criterion.DecisionId,
            criterion.Name,
            criterion.Weight);
    }
}
