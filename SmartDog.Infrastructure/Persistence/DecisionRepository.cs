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

    public async Task<IReadOnlyList<Decision>> GetAllAsync()
    {
        return await _dbContext.Decisions
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Decision?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Decisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
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

    public void Delete(Decision decision)
    {
        _dbContext.Decisions.Remove(decision);
    }

    public IQueryable<Decision> Query()
    {
        return _dbContext.Decisions
            .AsNoTracking();
    }
}