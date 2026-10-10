using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionCriterionService : IDecisionCriterionService
{
    private readonly IDecisionCriterionRepository _criterionRepository;
    private readonly IDecisionRepository _decisionRepository;

    public DecisionCriterionService(
        IDecisionCriterionRepository criterionRepository,
        IDecisionRepository decisionRepository)
    {
        _criterionRepository = criterionRepository;
        _decisionRepository = decisionRepository;
    }

    public async Task<IReadOnlyList<DecisionCriterionDto>?> GetByDecisionIdAsync(
        Guid decisionId)
    {
        if (await _decisionRepository.GetByIdAsync(decisionId) is null)
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
        var criterion = await _criterionRepository.GetByIdAndDecisionIdAsync(
            criterionId,
            decisionId);

        return criterion is null ? null : MapToDto(criterion);
    }

    public async Task<DecisionCriterionDto?> CreateAsync(
        Guid decisionId,
        CreateDecisionCriterionRequest request)
    {
        if (await _decisionRepository.GetByIdAsync(decisionId) is null)
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
