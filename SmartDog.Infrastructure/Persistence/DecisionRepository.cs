using Microsoft.EntityFrameworkCore;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence;

public class DecisionRepository : IDecisionRepository
{
    private readonly SmartDogDbContext _dbContext;

    public DecisionRepository(SmartDogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Decision>> GetAllAsync(Guid ownerId)
    {
        return await Query(ownerId).ToListAsync();
    }

    public async Task<Decision?> GetByIdAsync(Guid id, Guid ownerId)
    {
        return await Query(ownerId)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public Task<bool> ExistsAsync(Guid id, Guid ownerId)
    {
        return _dbContext.Decisions.AnyAsync(d => d.Id == id && d.OwnerId == ownerId);
    }

    public async Task AddAsync(Decision decision)
    {
        await _dbContext.Decisions.AddAsync(decision);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public void Update(Decision decision)
    {
        _dbContext.Decisions.Update(decision);
    }

    public async Task DeleteAsync(Decision decision)
    {
        var ratings = await _dbContext.DecisionAlternativeCriterionRatings
            .Where(rating => rating.DecisionId == decision.Id)
            .ToListAsync();

        _dbContext.DecisionAlternativeCriterionRatings.RemoveRange(ratings);
        _dbContext.Decisions.Remove(decision);
    }

    public IQueryable<Decision> Query(Guid ownerId)
    {
        return _dbContext.Decisions
            .AsNoTracking()
            .Where(d => d.OwnerId == ownerId);
    }
}