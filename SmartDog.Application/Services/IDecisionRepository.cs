using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

/// <summary>
/// Decisions are always read through their owner: a decision of another user, or one without an owner, is never
/// returned, so callers cannot tell it apart from one that does not exist.
/// </summary>
public interface IDecisionRepository
{
    IQueryable<Decision> Query(Guid ownerId);

    Task<IReadOnlyList<Decision>> GetAllAsync(Guid ownerId);

    Task<Decision?> GetByIdAsync(Guid id, Guid ownerId);

    /// <summary>Whether the decision exists and belongs to <paramref name="ownerId"/>.</summary>
    Task<bool> ExistsAsync(Guid id, Guid ownerId);

    Task AddAsync(Decision decision);

    void Update(Decision decision);

    Task DeleteAsync(Decision decision);

    Task SaveChangesAsync();
}