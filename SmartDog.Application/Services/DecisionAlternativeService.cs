using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionAlternativeService
    : IDecisionAlternativeService
{
    private readonly IDecisionAlternativeRepository _repository;
    private readonly IDecisionRepository _decisionRepository;

    public DecisionAlternativeService(
        IDecisionAlternativeRepository repository,
        IDecisionRepository decisionRepository)
    {
        _repository = repository;
        _decisionRepository = decisionRepository;
    }

    public async Task<IReadOnlyList<DecisionAlternativeDto>?>
        GetByDecisionIdAsync(Guid decisionId)
    {
        if (await _decisionRepository.GetByIdAsync(decisionId) is null)
        {
            return null;
        }

        var alternatives =
            await _repository.GetByDecisionIdAsync(decisionId);

        return alternatives
            .Select(MapToDto)
            .ToList();
    }

    public async Task<DecisionAlternativeDto?>
        GetByIdAsync(
            Guid decisionId,
            Guid alternativeId)
    {
        var alternative =
            await _repository.GetByIdAndDecisionIdAsync(
                alternativeId,
                decisionId);

        return alternative is null
            ? null
            : MapToDto(alternative);
    }

    public async Task<DecisionAlternativeDto?>
        CreateAsync(
            Guid decisionId,
            CreateDecisionAlternativeRequest request)
    {
        if (await _decisionRepository.GetByIdAsync(decisionId) is null)
        {
            return null;
        }

        var alternative = new DecisionAlternative
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            Name = request.Name,
            Description = request.Description,
            Score = request.Score
        };

        await _repository.AddAsync(alternative);
        await _repository.SaveChangesAsync();

        return MapToDto(alternative);
    }

    public async Task<DecisionAlternativeDto?>
        UpdateAsync(
            Guid decisionId,
            Guid alternativeId,
            UpdateDecisionAlternativeRequest request)
    {
        var alternative =
            await _repository.GetByIdAndDecisionIdAsync(
                alternativeId,
                decisionId);

        if (alternative is null)
            return null;

        alternative.Name = request.Name;
        alternative.Description = request.Description;
        alternative.Score = request.Score;

        _repository.Update(alternative);

        await _repository.SaveChangesAsync();

        return MapToDto(alternative);
    }

    public async Task<bool> DeleteAsync(
        Guid decisionId,
        Guid alternativeId)
    {
        var alternative =
            await _repository.GetByIdAndDecisionIdAsync(
                alternativeId,
                decisionId);

        if (alternative is null)
            return false;

        _repository.Delete(alternative);

        await _repository.SaveChangesAsync();

        return true;
    }

    private static DecisionAlternativeDto MapToDto(
        DecisionAlternative alternative)
    {
        return new DecisionAlternativeDto(
            alternative.Id,
            alternative.DecisionId,
            alternative.Name,
            alternative.Description,
            alternative.Score);
    }
}