using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionAlternativeService
    : IDecisionAlternativeService
{
    private readonly IDecisionAlternativeRepository _repository;
    private readonly IDecisionRepository _decisionRepository;
    private readonly ICurrentUser _currentUser;

    public DecisionAlternativeService(
        IDecisionAlternativeRepository repository,
        IDecisionRepository decisionRepository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _decisionRepository = decisionRepository;
        _currentUser = currentUser;
    }

    /// <summary>Every operation first checks that the decision belongs to the signed-in user.</summary>
    private Task<bool> OwnsDecisionAsync(Guid decisionId) =>
        _decisionRepository.ExistsAsync(decisionId, _currentUser.RequireUserId());

    public async Task<IReadOnlyList<DecisionAlternativeDto>?>
        GetByDecisionIdAsync(Guid decisionId)
    {
        if (!await OwnsDecisionAsync(decisionId))
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
        if (!await OwnsDecisionAsync(decisionId))
        {
            return null;
        }

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
        if (!await OwnsDecisionAsync(decisionId))
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
        if (!await OwnsDecisionAsync(decisionId))
            return null;

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
        if (!await OwnsDecisionAsync(decisionId))
            return false;

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