using Microsoft.EntityFrameworkCore;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence;

public class DecisionCriterionRepository : IDecisionCriterionRepository
{
    private readonly SmartDogDbContext _dbContext;

    public DecisionCriterionRepository(SmartDogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DecisionCriterion>> GetByDecisionIdAsync(
        Guid decisionId)
    {
        return await _dbContext.DecisionCriteria
            .AsNoTracking()
            .Where(criterion => criterion.DecisionId == decisionId)
            .OrderBy(criterion => criterion.Name)
            .ToListAsync();
    }

    public async Task<DecisionCriterion?> GetByIdAsync(Guid criterionId)
    {
        return await _dbContext.DecisionCriteria
            .AsNoTracking()
            .FirstOrDefaultAsync(criterion => criterion.Id == criterionId);
    }

    public async Task<DecisionCriterion?> GetByIdAndDecisionIdAsync(
        Guid criterionId,
        Guid decisionId)
    {
        return await _dbContext.DecisionCriteria
            .FirstOrDefaultAsync(criterion =>
                criterion.Id == criterionId &&
                criterion.DecisionId == decisionId);
    }

    public async Task AddAsync(DecisionCriterion criterion)
    {
        await _dbContext.DecisionCriteria.AddAsync(criterion);
    }

    public void Update(DecisionCriterion criterion)
    {
        _dbContext.DecisionCriteria.Update(criterion);
    }

    public async Task DeleteAsync(DecisionCriterion criterion)
    {
        var ratings = await _dbContext.DecisionAlternativeCriterionRatings
            .Where(rating =>
                rating.DecisionId == criterion.DecisionId &&
                rating.DecisionCriterionId == criterion.Id)
            .ToListAsync();

        _dbContext.DecisionAlternativeCriterionRatings.RemoveRange(ratings);
        _dbContext.DecisionCriteria.Remove(criterion);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
