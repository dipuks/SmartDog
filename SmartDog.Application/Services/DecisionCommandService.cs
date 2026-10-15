using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionCommandService : IDecisionCommandService
{
    private readonly IDecisionRepository _repository;
    private readonly ILogger<DecisionCommandService> _logger;

    private readonly IMemoryCache _cache;
    private readonly ICurrentUser _currentUser;

    public DecisionCommandService(
    IDecisionRepository repository,
    ILogger<DecisionCommandService> logger,
    IMemoryCache cache,
    ICurrentUser currentUser)
    {
        _repository = repository;
        _logger = logger;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async Task<DecisionDto> CreateAsync(
        CreateDecisionRequest request)
    {
        _logger.LogInformation(
            "Creating decision with title {DecisionTitle}",
            request.Title);

        var decision = new Decision
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            Status = "Created",
            // Always the signed-in user; requests carry no owner, so one cannot be forged.
            OwnerId = _currentUser.RequireUserId()
        };

        await _repository.AddAsync(decision);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Decision created successfully with ID {DecisionId}",
            decision.Id);

        return new DecisionDto(
            decision.Id,
            decision.Title,
            decision.Description,
            decision.CreatedAt,
            decision.Status,
            decision.RowVersion);
    }

    public async Task<DecisionDto?> UpdateAsync(
        Guid id,
        UpdateDecisionRequest request)
    {
        _logger.LogInformation(
            "Updating decision {DecisionId}",
            id);

        var ownerId = _currentUser.RequireUserId();
        var decision = await _repository.GetByIdAsync(id, ownerId);

        if (decision is null)
        {
            _logger.LogWarning(
                "Decision {DecisionId} was not found",
                id);

            return null;
        }

        if (!DecisionStatuses.CanTransition(
            decision.Status,
            request.Status))
        {
            throw new DecisionStatusTransitionException(
                decision.Status,
                request.Status);
        }

        decision.Title = request.Title;
        decision.Description = request.Description;
        decision.Status = request.Status;
        decision.RowVersion = request.RowVersion.ToArray();

        _repository.Update(decision);

        await _repository.SaveChangesAsync();
        _cache.Remove(DecisionCacheKeys.Decision(ownerId, id));

        _logger.LogInformation(
            "Decision {DecisionId} updated successfully",
            id);

        return new DecisionDto(
            decision.Id,
            decision.Title,
            decision.Description,
            decision.CreatedAt,
            decision.Status,
            decision.RowVersion);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation(
            "Deleting decision {DecisionId}",
            id);

        var ownerId = _currentUser.RequireUserId();
        var decision = await _repository.GetByIdAsync(id, ownerId);

        if (decision is null)
        {
            _logger.LogWarning(
                "Cannot delete decision {DecisionId}: not found",
                id);

            return false;
        }

        await _repository.DeleteAsync(decision);

        await _repository.SaveChangesAsync();
        _cache.Remove(DecisionCacheKeys.Decision(ownerId, id));

        _logger.LogInformation(
            "Decision {DecisionId} deleted successfully",
            id);

        return true;
    }
}