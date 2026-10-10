using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public interface IDecisionRepository
{
    IQueryable<Decision> Query();

    Task<IReadOnlyList<Decision>> GetAllAsync();

    Task<Decision?> GetByIdAsync(Guid id);

    Task AddAsync(Decision decision);

    void Update(Decision decision);

    void Delete(Decision decision);

    Task SaveChangesAsync();
}