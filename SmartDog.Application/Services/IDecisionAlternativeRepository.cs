using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public interface IDecisionAlternativeRepository
{
    Task<IReadOnlyList<DecisionAlternative>> GetByDecisionIdAsync(
        Guid decisionId);

    Task<DecisionAlternative?> GetByIdAsync(Guid id);

    Task<DecisionAlternative?> GetByIdAndDecisionIdAsync(
        Guid id,
        Guid decisionId);

    Task AddAsync(DecisionAlternative alternative);

    void Update(DecisionAlternative alternative);

    void Delete(DecisionAlternative alternative);

    Task SaveChangesAsync();
}