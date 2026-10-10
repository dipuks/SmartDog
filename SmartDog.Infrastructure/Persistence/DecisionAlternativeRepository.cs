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

    public async Task<IReadOnlyList<DecisionAlternative>>
        GetByDecisionIdWithRatingsAsync(Guid decisionId)
    {
        return await _dbContext.DecisionAlternatives
            .AsNoTracking()
            .Include(alternative => alternative.CriterionRatings)
            .Where(alternative => alternative.DecisionId == decisionId)
            .OrderBy(alternative => alternative.Name)
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

    public async Task<IReadOnlyList<DecisionAlternativeCriterionRating>>
        GetRatingsByAlternativeIdAsync(
            Guid decisionId,
            Guid alternativeId)
    {
        return await _dbContext.DecisionAlternativeCriterionRatings
            .AsNoTracking()
            .Include(rating => rating.Criterion)
            .Where(rating =>
                rating.DecisionId == decisionId &&
                rating.DecisionAlternativeId == alternativeId)
            .OrderBy(rating => rating.Criterion!.Name)
            .ToListAsync();
    }

    public async Task<DecisionAlternativeCriterionRating?> GetRatingAsync(
        Guid decisionId,
        Guid alternativeId,
        Guid criterionId)
    {
        return await _dbContext.DecisionAlternativeCriterionRatings
            .FirstOrDefaultAsync(rating =>
                rating.DecisionId == decisionId &&
                rating.DecisionAlternativeId == alternativeId &&
                rating.DecisionCriterionId == criterionId);
    }

    public async Task AddRatingAsync(
        DecisionAlternativeCriterionRating rating)
    {
        await _dbContext.DecisionAlternativeCriterionRatings.AddAsync(rating);
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