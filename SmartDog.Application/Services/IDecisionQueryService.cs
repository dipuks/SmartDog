using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionQueryService
{
    Task<PagedResult<DecisionDto>> GetAllAsync(
    DecisionQueryRequest request);

    Task<DecisionDto?> GetByIdAsync(Guid id);
}