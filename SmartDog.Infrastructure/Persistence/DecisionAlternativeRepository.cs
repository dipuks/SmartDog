using Microsoft.EntityFrameworkCore;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence;

public class DecisionAlternativeRepository
    : IDecisionAlternativeRepository
{
    private readonly SmartDogDbContext _dbContext;

    public DecisionAlternativeRepository(
        SmartDogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DecisionAlternative>>
        GetByDecisionIdAsync(Guid decisionId)
    {
        return await _dbContext.DecisionAlternatives
            .AsNoTracking()
            .Where(a => a.DecisionId == decisionId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<DecisionAlternative?> GetByIdAsync(Guid id)
    {
        return await _dbContext.DecisionAlternatives
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<DecisionAlternative?>
        GetByIdAndDecisionIdAsync(
            Guid id,
            Guid decisionId)
    {
        return await _dbContext.DecisionAlternatives
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.DecisionId == decisionId);
    }

    public async Task AddAsync(
        DecisionAlternative alternative)
    {
        await _dbContext.DecisionAlternatives.AddAsync(alternative);
    }

    public void Update(
        DecisionAlternative alternative)
    {
        _dbContext.DecisionAlternatives.Update(alternative);
    }

    public void Delete(
        DecisionAlternative alternative)
    {
        _dbContext.DecisionAlternatives.Remove(alternative);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}